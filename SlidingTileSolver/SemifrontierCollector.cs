using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class SemifrontierCollector
{
    private SegmentedFileLong Semifrontier;
    private List<long[]> UpBuffers = new List<long[]>();
    private int[] UpCounts;
    private List<long[]> DnBuffers = new List<long[]>();
    private int[] DnCounts;

    public SemifrontierCollector(SegmentedFileLong semifrontier, PuzzleInfo info)
    {
        Semifrontier = semifrontier;
        UpCounts = new int[info.SegmentsCount];
        DnCounts = new int[info.SegmentsCount];
        for (int i = 0; i < info.SegmentsCount; i++)
        {
            UpBuffers.Add(new long[PuzzleInfo.SEMIFRONTIER_BUFFER_SIZE]);
            DnBuffers.Add(new long[PuzzleInfo.SEMIFRONTIER_BUFFER_SIZE]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void CollectUp(long[] buffer, int len)
    {
        for (int i = 0; i < len; i++)
        {
            AddUp(buffer[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void CollectDn(long[] buffer, int len)
    {
        for (int i = 0; i < len; i++)
        {
            AddDn(buffer[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void AddUp(long value)
    {
        int segment = (int)(value >> PuzzleInfo.SEGMENT_SIZE_POW);
        UpBuffers[segment][UpCounts[segment]++] = value;
        if (UpCounts[segment] == UpBuffers[segment].Length)
        {
            FlushUp(segment);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void AddDn(long value)
    {
        int segment = (int)(value >> PuzzleInfo.SEGMENT_SIZE_POW);
        DnBuffers[segment][DnCounts[segment]++] = value;
        if (DnCounts[segment] == DnBuffers[segment].Length)
        {
            FlushDn(segment);
        }
    }

    public void Close()
    {
        for (int i = 0; i < UpBuffers.Count; i++)
        {
            if (UpCounts[i] > 0) FlushUp(i);
            if (DnCounts[i] > 0) FlushDn(i);
        }
    }

    private void FlushUp(int segment)
    {
        Semifrontier.WriteSegment(segment * 2, UpBuffers[segment], 0, UpCounts[segment]);
        UpCounts[segment] = 0;
    }


    private void FlushDn(int segment)
    {
        Semifrontier.WriteSegment(segment * 2 + 1, DnBuffers[segment], 0, DnCounts[segment]);
        DnCounts[segment] = 0;
    }
}
