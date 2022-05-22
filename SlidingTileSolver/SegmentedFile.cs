using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct FilePart
{
    public long Offset;
    public int Length;
}

public class SegmentedFile : IDisposable
{
    class Segment
    {
        public readonly List<FilePart> Parts = new List<FilePart>();
    }

    private static TimeSpan WriteTime;
    private static TimeSpan ReadTime;
    private static long BytesWritten;
    private static long BytesRead;

    private readonly FileStream[] Streams;
    private readonly Segment[] Segments;

    public SegmentedFile(string fileName, int segmentsCount)
    {
        Streams = new FileStream[] { Util.OpenFile(fileName) };
        Segments = new Segment[segmentsCount];
        for (int i = 0; i < Segments.Length; i++)
        {
            Segments[i] = new Segment();
        }
    }

    public SegmentedFile(int segmentsCount, params string[] fileNames)
    {
        Streams = fileNames.Select(f => Util.OpenFile(f)).ToArray();
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
        foreach (var stream in Streams) stream.Position = 0;
    }

    public unsafe void WriteSegment(int segment, uint[] buffer, int offset, int length)
    {
        if (length == 0) return;

        var stream = Streams[segment % Streams.Length];

        FilePart part;
        part.Length = length;
        lock (stream)
        {
            var timer = Stopwatch.StartNew();
            part.Offset = stream.Position;
            fixed (uint* bufPtr = buffer)
            {
                var span = new ReadOnlySpan<byte>((byte*)(bufPtr + offset), length * 4);
                stream.Write(span);
            }
            BytesWritten += length * 4;
            WriteTime += timer.Elapsed;
        }
        Segments[segment].Parts.Add(part);
    }

    public unsafe void WriteSegment(int segment, uint* buffer, int offset, int length)
    {
        if (length == 0) return;

        var stream = Streams[segment % Streams.Length];

        FilePart part;
        part.Length = length;
        lock (stream)
        {
            var timer = Stopwatch.StartNew();
            part.Offset = stream.Position;
            var span = new ReadOnlySpan<byte>((byte*)(buffer + offset), length * 4);
            stream.Write(span);
            BytesWritten += length * 4;
            WriteTime += timer.Elapsed;
        }
        Segments[segment].Parts.Add(part);
    }

    public int SegmentParts(int segment)
    {
        return Segments[segment].Parts.Count;
    }

    public unsafe int ReadSegment(int segment, int part, uint[] buffer)
    {
        var stream = Streams[segment % Streams.Length];
        var segmentPart = Segments[segment].Parts[part];
        lock (stream)
        {
            var timer = Stopwatch.StartNew();
            stream.Seek(segmentPart.Offset, SeekOrigin.Begin);
            fixed (uint* bufPtr = buffer)
            {
                var span = new Span<byte>((byte*)(bufPtr), segmentPart.Length * 4);
                int read = stream.Read(span);
                if (read != segmentPart.Length * 4) throw new Exception($"Error: read={read} exp={span.Length}");
            }
            BytesRead += segmentPart.Length * 4;
            ReadTime += timer.Elapsed;
        }
        return segmentPart.Length;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"SegmentedFileUint: WriteTime={WriteTime}, Bytes={BytesWritten:N0}, ReadTime={ReadTime}, Bytes={BytesRead:N0}");
    }

    public void Dispose()
    {
        foreach (var stream in Streams) stream.Dispose();
    }
}
