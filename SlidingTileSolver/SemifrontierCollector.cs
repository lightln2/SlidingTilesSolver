﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public unsafe class SemifrontierCollector
{
    private readonly int Segments;
    private readonly SegmentedFile Semifrontier;
    private uint* Buffers;
    private readonly int[] Counts;

    private static TimeSpan TimeCollect = TimeSpan.Zero;
    private static TimeSpan TimeFlush = TimeSpan.Zero;

    public SemifrontierCollector(SegmentedFile semifrontier, PuzzleInfo info)
    {
        Segments = info.SegmentsCount;
        Semifrontier = semifrontier;
        Buffers = info.Arena.AllocUint(PuzzleInfo.SEMIFRONTIER_BUFFER_SIZE * Segments);
        Counts = new int[Segments];
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Collect(long[] buffer, int len)
    {
        var timer = Stopwatch.StartNew();
        lock (Counts)
        {
            fixed (long* bufferPtr = buffer)
            {
                for (int i = 0; i < len; i++)
                {
                    Add(bufferPtr + i, Buffers);
                }
            }
            TimeCollect += timer.Elapsed;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void Add(long* value, uint* dstBuffer)
    {
        int* ival = (int*)value;
        int segment = ival[1];
        long offset = (long)segment << PuzzleInfo.SEMIFRONTIER_BUFFER_POW;
        Buffers[offset + (Counts[segment]++)] = (uint)ival[0];
        if (Counts[segment] == PuzzleInfo.SEMIFRONTIER_BUFFER_SIZE)
        {
            Flush(segment);
        }
    }

    public void Close()
    {
        lock(Counts)
        {
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < Segments; i++)
            {
                if (Counts[i] > 0) Flush(i);
            }
            TimeFlush += timer.Elapsed;
        }
    }

    private void Flush(int segment)
    {
        Semifrontier.WriteSegment(segment, Buffers, segment << PuzzleInfo.SEMIFRONTIER_BUFFER_POW, Counts[segment]);
        Counts[segment] = 0;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"SemifrontierCollector: collect={TimeCollect}; close={TimeFlush}");
    }

}
