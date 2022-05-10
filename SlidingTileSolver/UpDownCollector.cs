using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class UpDownCollector
{
    public readonly SemifrontierCollector SemifrontierCollector;
    private readonly long[] UpBuffer = new long[GpuSolver.GPUSIZE];
    private int UpCount;
    private readonly long[] DnBuffer = new long[GpuSolver.GPUSIZE];
    private int DnCount;

    public UpDownCollector(SemifrontierCollector semifrontierCollector)
    {
        SemifrontierCollector = semifrontierCollector;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Collect(long[] buffer, int len)
    {
        for (int i = 0; i < len; i++)
        {
            long val = buffer[i];
            if ((val & PuzzleInfo.STATE_UP) != 0)
            {
                AddUp(val >> 4);
            }
            if ((val & PuzzleInfo.STATE_DN) != 0)
            {
                AddDn(val >> 4);
            }
        }
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
        GpuSolver.CalcGPU(UpCount, true, UpBuffer);
        SemifrontierCollector.CollectUp(UpBuffer, UpCount);
        UpCount = 0;
    }

    private void FlushDn()
    {
        GpuSolver.CalcGPU(DnCount, false, DnBuffer);
        SemifrontierCollector.CollectDn(DnBuffer, DnCount);
        DnCount = 0;
    }

    public void Close()
    {
        FlushUp();
        FlushDn();
        SemifrontierCollector.Close();
    }
}
