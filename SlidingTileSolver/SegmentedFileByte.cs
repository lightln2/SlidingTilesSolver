using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SegmentedFileByte : IDisposable
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

    public SegmentedFileByte(string fileName, int segmentsCount)
    {
        Streams = new FileStream[] { Util.OpenFile(fileName) };
        Segments = new Segment[segmentsCount];
        for (int i = 0; i < Segments.Length; i++)
        {
            Segments[i] = new Segment();
        }
    }

    public SegmentedFileByte(int segmentsCount, params string[] fileNames)
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

    public void WriteSegment(int segment, byte[] buffer, int offset, int length)
    {
        if (length == 0) return;

        var stream = Streams[segment % Streams.Length];

        FilePart part;
        part.Length = length;
        lock (stream)
        {
            var timer = Stopwatch.StartNew();
            part.Offset = stream.Position;
            stream.Write(buffer, offset, length);
            BytesWritten += length;
            WriteTime += timer.Elapsed;
        }
        Segments[segment].Parts.Add(part);
    }

    public int SegmentParts(int segment)
    {
        return Segments[segment].Parts.Count;
    }

    public unsafe int ReadSegment(int segment, int part, byte[] buffer)
    {
        var segmentPart = Segments[segment].Parts[part];

        var stream = Streams[segment % Streams.Length];

        lock (stream)
        {
            var timer = Stopwatch.StartNew();
            stream.Seek(segmentPart.Offset, SeekOrigin.Begin);
            int read = stream.Read(buffer, 0, segmentPart.Length);
            if (read != segmentPart.Length) throw new Exception($"Error: read={read} exp={segmentPart.Length}");
            BytesRead += segmentPart.Length;
            ReadTime += timer.Elapsed;
        }
        return segmentPart.Length;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"SegmentedFileBytes: WriteTime={WriteTime}, Bytes={BytesWritten:N0}, ReadTime={ReadTime}, Bytes={BytesRead:N0}");
    }

    public void Dispose()
    {
        foreach (var stream in Streams) stream.Dispose();
    }
}
