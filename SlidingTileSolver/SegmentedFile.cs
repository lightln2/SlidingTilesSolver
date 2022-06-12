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

public unsafe class SegmentedFile : IDisposable
{
    public static bool DELETE_ON_CLEAR = true;

    class Segment
    {
        public readonly List<FilePart> Parts = new List<FilePart>();
    }

    private static TimeSpan WriteTime;
    private static TimeSpan ReadTime;
    private static long BytesWritten;
    private static long BytesRead;

    private string[] FileNames;
    private FileStream[] Streams;
    private readonly Segment[] Segments;

    public SegmentedFile(string fileName, int segmentsCount) : this(segmentsCount, new string[] { fileName }) { }

    public SegmentedFile(int segmentsCount, params string[] fileNames)
    {
        FileNames = fileNames;
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
        if (DELETE_ON_CLEAR)
        {
            foreach (var stream in Streams) stream.Dispose();
            foreach (var fileName in FileNames) File.Delete(fileName);
            Streams = FileNames.Select(f => Util.OpenFile(f)).ToArray();
        }
        else
        {
            foreach (var stream in Streams) stream.Position = 0;
        }
    }

    public unsafe void WriteSegment(int segment, uint[] buffer, long offset, int length)
    {
        fixed (uint* ptr = buffer)
        {
            WriteSegment(segment, ptr, offset, length);
        }
    }

    public unsafe void WriteSegment(int segment, uint* buffer, long offset, int length)
    {
        WriteSegment(segment, (byte*)buffer, offset * 4, length * 4);
    }

    public void WriteSegment(int segment, byte[] buffer, long offset, int length)
    {
        if (length == 0) return;
        fixed (byte* ptr = buffer)
        {
            WriteSegment(segment, ptr, offset, length);
        }
    }

    public void WriteSegment(int segment, byte* buffer, long offset, int length)
    {
        if (length == 0) return;

        var stream = Streams[segment % Streams.Length];

        FilePart part;
        part.Length = length;
        lock (stream)
        {
            var timer = Stopwatch.StartNew();
            part.Offset = stream.Position;
            var span = new ReadOnlySpan<byte>(buffer + offset, length);
            stream.Write(span);
            BytesWritten += length;
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
        fixed (uint* ptr = buffer)
        {
            int bytesRead = ReadSegment(segment, part, (byte*)ptr);
            if (bytesRead % 4 != 0) throw new Exception($"Bytes read = {bytesRead} but should be multiple of 4");
            return bytesRead / 4;
        }
    }

    public unsafe int ReadSegment(int segment, int part, byte[] buffer)
    {
        fixed (byte* ptr = buffer)
        {
            return ReadSegment(segment, part, ptr);
        }
    }

    public unsafe int ReadSegment(int segment, int part, byte* buffer)
    {
        var stream = Streams[segment % Streams.Length];
        var segmentPart = Segments[segment].Parts[part];
        lock (stream)
        {
            var timer = Stopwatch.StartNew();
            stream.Seek(segmentPart.Offset, SeekOrigin.Begin);
            var span = new Span<byte>(buffer, segmentPart.Length);
            int read = stream.Read(span);
            if (read != segmentPart.Length) throw new Exception($"Error: read={read} exp={span.Length}");
            BytesRead += segmentPart.Length;
            ReadTime += timer.Elapsed;
        }
        return segmentPart.Length;
    }

    public long TotalSize()
    {
        long size = 0;
        foreach(var s in Segments)
        {
            foreach (var p in s.Parts)
            {
                size += p.Length;
            }
        }
        return size;
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
