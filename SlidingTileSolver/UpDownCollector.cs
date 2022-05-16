﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class UpDownCollector
{
    public readonly SemifrontierCollector SemifrontierCollectorUp;
    public readonly SemifrontierCollector SemifrontierCollectorDown;
    private readonly long[] UpBuffer = new long[GpuSolver.GPUSIZE];
    private int UpCount;
    private readonly long[] DnBuffer = new long[GpuSolver.GPUSIZE];
    private int DnCount;

    private static TimeSpan TimeCollect = TimeSpan.Zero;
    private static TimeSpan TimeClose = TimeSpan.Zero;
    private Stopwatch Timer = new Stopwatch();

    public UpDownCollector(SemifrontierCollector semifrontierCollectorUp, SemifrontierCollector semifrontierCollectorDown)
    {
        SemifrontierCollectorUp = semifrontierCollectorUp;
        SemifrontierCollectorDown = semifrontierCollectorDown;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Collect(int segment, long[] buffer, int len)
    {
        Timer.Restart();
        long baseIndex = ((long)segment << PuzzleInfo.SEGMENT_SIZE_POW) << 4;
        for (int i = 0; i < len; i++)
        {
            long val = baseIndex | buffer[i];

            if ((val & PuzzleInfo.STATE_UP) != 0)
            {
                AddUp(val >> 4);
            }
            if ((val & PuzzleInfo.STATE_DN) != 0)
            {
                AddDn(val >> 4);
            }
        }
        TimeCollect += Timer.Elapsed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void AddUp(long value)
    {
        UpBuffer[UpCount++] = value;
        if (UpCount == UpBuffer.Length)
        {
            FlushUp();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void AddDn(long value)
    {
        DnBuffer[DnCount++] = value;
        if (DnCount == DnBuffer.Length)
        {
            FlushDn();
        }
    }

    private void FlushUp()
    {
        GpuSolver.CalcGPU_Up(UpCount, UpBuffer);
        SemifrontierCollectorUp.Collect(UpBuffer, UpCount);
        UpCount = 0;
    }

    private void FlushDn()
    {
        GpuSolver.CalcGPU_Down(DnCount, DnBuffer);
        SemifrontierCollectorDown.Collect(DnBuffer, DnCount);
        DnCount = 0;
    }

    public void Close()
    {
        Timer.Restart();
        FlushUp();
        FlushDn();
        SemifrontierCollectorUp.Close();
        SemifrontierCollectorDown.Close();
        TimeClose += Timer.Elapsed;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"UpDownCollector: collect={TimeCollect} close={TimeClose}");
    }
}
