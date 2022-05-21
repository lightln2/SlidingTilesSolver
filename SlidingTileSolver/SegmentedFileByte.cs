﻿using System;
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

    public readonly string FileName;
    private readonly FileStream Stream;
    private readonly Segment[] Segments;

    private readonly object Sync = new object();
    private readonly Stopwatch Timer = new Stopwatch();

    public SegmentedFileByte(string fileName, int segmentsCount)
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

    public void WriteSegment(int segment, byte[] buffer, int offset, int length)
    {
        if (length == 0) return;

        FilePart part;
        part.Length = length;
        lock (Sync)
        {
            Timer.Restart();
            part.Offset = Stream.Position;
            Stream.Write(buffer, offset, length);
            BytesWritten += length;
            WriteTime += Timer.Elapsed;
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
        lock (Sync)
        {
            Timer.Restart();
            Stream.Seek(segmentPart.Offset, SeekOrigin.Begin);
            int read = Stream.Read(buffer, 0, segmentPart.Length);
            if (read != segmentPart.Length) throw new Exception($"Error: read={read} exp={segmentPart.Length}");
            BytesRead += segmentPart.Length;
            ReadTime += Timer.Elapsed;
        }
        return segmentPart.Length;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"SegmentedFileBytes: WriteTime={WriteTime}, Bytes={BytesWritten:N0}, ReadTime={ReadTime}, Bytes={BytesRead:N0}");
    }

    public void Dispose()
    {
        Stream.Dispose();
    }
}