using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SegmentedFileLong : IDisposable
{
    class Segment
    {
        public readonly List<FilePart> Parts = new List<FilePart>();
    }

    private static TimeSpan WriteTime;
    private static TimeSpan ReadTime;
    private static long BytesWritten;
    private static long BytesRead;

    public readonly string FileName;
    private readonly FileStream Stream;
    private readonly Segment[] Segments;

    private readonly object Sync = new object();
    private readonly Stopwatch Timer = new Stopwatch();

    public SegmentedFileLong(string fileName, int segmentsCount)
    {
        FileName = fileName;
        Stream = Util.OpenFile(fileName);
        Segments = new Segment[segmentsCount];
        for (int i = 0; i < Segments.Length; i++)
        {
            Segments[i] = new Segment();
        }
    }

    public int SegmentsCount => Segments.Length;

    public void Clear()
    {
        for (int i = 0; i < Segments.Length; i++)
        {
            Segments[i].Parts.Clear();
        }
        Stream.Position = 0;
    }

    public unsafe void WriteSegment(int segment, long[] buffer, int offset, int length)
    {
        if (length == 0) return;

        FilePart part;
        part.Length = length;
        lock (Sync)
        {
            Timer.Restart();
            part.Offset = Stream.Position;
            fixed (long* bufPtr = buffer)
            {
                var span = new ReadOnlySpan<byte>((byte*)(bufPtr + offset), length * 8);
                Stream.Write(span);
            }
            BytesWritten += length * 8;
            WriteTime += Timer.Elapsed;
        }
        Segments[segment].Parts.Add(part);
    }

    public int SegmentParts(int segment)
    {
        return Segments[segment].Parts.Count;
    }

    public unsafe int ReadSegment(int segment, int part, long[] buffer)
    {
        var segmentPart = Segments[segment].Parts[part];
        lock (Sync)
        {
            Timer.Restart();
            Stream.Seek(segmentPart.Offset, SeekOrigin.Begin);
            fixed (long* bufPtr = buffer)
            {
                var span = new Span<byte>((byte*)(bufPtr), segmentPart.Length * 8);
                int read = Stream.Read(span);
                if (read != segmentPart.Length * 8) throw new Exception($"Error: read={read} exp={span.Length}");
            }
            BytesRead += segmentPart.Length * 8;
            ReadTime += Timer.Elapsed;
        }
        return segmentPart.Length;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"Frontier: WriteTime={WriteTime}, Bytes={BytesWritten:N0}, ReadTime={ReadTime}, Bytes={BytesRead:N0}");
    }

    public void Dispose()
    {
        Stream.Dispose();
    }
}
