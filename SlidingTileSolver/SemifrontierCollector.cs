using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class SemifrontierCollector
{
    private SegmentedFile Semifrontier;
    private List<uint[]> Buffers = new List<uint[]>();
    private int[] Counts;

    public SemifrontierCollector(SegmentedFile semifrontier, PuzzleInfo info)
    {
        Semifrontier = semifrontier;
        Counts = new int[info.SegmentsCount];
        for (int i = 0; i < info.SegmentsCount; i++)
        {
            Buffers.Add(new uint[PuzzleInfo.SEMIFRONTIER_BUFFER_SIZE]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Collect(long[] buffer, int len)
    {
        for (int i = 0; i < len; i++)
        {
            Add(buffer[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void Add(long value)
    {
        int segment = (int)(value >> PuzzleInfo.SEGMENT_SIZE_POW);
        Buffers[segment][Counts[segment]++] = (uint)(value & PuzzleInfo.SEGMENT_MASK);
        if (Counts[segment] == Buffers[segment].Length)
        {
            Flush(segment);
        }
    }

    public void Close()
    {
        for (int i = 0; i < Buffers.Count; i++)
        {
            if (Counts[i] > 0) Flush(i);
        }
    }

    private void Flush(int segment)
    {
        Semifrontier.WriteSegment(segment, Buffers[segment], 0, Counts[segment]);
        Counts[segment] = 0;
    }


}
