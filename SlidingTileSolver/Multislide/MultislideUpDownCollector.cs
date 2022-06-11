using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public unsafe class MultislideUpDownCollector
{
    private readonly PuzzleInfo Info;
    public readonly SemifrontierCollector SemifrontierCollector;
    private const int BufferLength = GpuSolver.GPUSIZE;
    private readonly long* UpBuffer;
    private int UpCount;
    private readonly long* DnBuffer;
    private int DnCount;

    private static TimeSpan TimeCollect = TimeSpan.Zero;
    private static TimeSpan TimeClose = TimeSpan.Zero;
    private Stopwatch Timer = new Stopwatch();

    public MultislideUpDownCollector(PuzzleInfo info, SemifrontierCollector semifrontierCollector)
    {
        Info = info;
        SemifrontierCollector = semifrontierCollector;
        UpBuffer = info.Arena.Alloclong(BufferLength);
        DnBuffer = info.Arena.Alloclong(BufferLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Collect(int segment, uint[] vals, byte[] states, int len)
    {
        Timer.Restart();
        long baseIndex = ((long)segment << PuzzleInfo.SEGMENT_SIZE_POW);
        fixed(byte* statesPtr = states)
        {
            for (int i = 0; i < len; i++)
            {
                long val = baseIndex | vals[i];
                byte state = states[i];
                UpBuffer[UpCount] = val;
                UpCount += (state & PuzzleInfo.STATE_UP);
                if (UpCount == BufferLength) FlushUp();
                DnBuffer[DnCount] = val;
                DnCount += ((state & PuzzleInfo.STATE_DN) >> 1);
                if (DnCount == BufferLength) FlushDn();
            }
        }
        TimeCollect += Timer.Elapsed;
    }

    private void FlushUp()
    {
        GpuSolver.CalcGPU_Up(UpCount, UpBuffer);
        SemifrontierCollector.Collect(UpBuffer, UpCount);

        for (int i = 0; i < Info.Height - 2; i++)
        {
            GpuSolver.CalcGPU_Up(UpCount, UpBuffer);

            int src = 0;
            int dst = 0;
            while (dst < UpCount)
            {
                if (UpBuffer[dst] == -1)
                {
                    dst++;
                    continue;
                }
                UpBuffer[src] = UpBuffer[dst];
                src++;
                dst++;
            }
            UpCount = src;
            SemifrontierCollector.Collect(UpBuffer, UpCount);
        }

        UpCount = 0;
    }

    private void FlushDn()
    {
        GpuSolver.CalcGPU_Down(DnCount, DnBuffer);
        SemifrontierCollector.Collect(DnBuffer, DnCount);

        for (int i = 0; i < Info.Height - 2; i++)
        {
            GpuSolver.CalcGPU_Down(DnCount, DnBuffer);

            int src = 0;
            int dst = 0;
            while (dst < DnCount)
            {
                if (DnBuffer[dst] == -1)
                {
                    dst++;
                    continue;
                }
                DnBuffer[src] = DnBuffer[dst];
                src++;
                dst++;
            }
            DnCount = src;
            SemifrontierCollector.Collect(DnBuffer, DnCount);
        }

        DnCount = 0;
    }

    public void Close()
    {
        Timer.Restart();
        FlushUp();
        FlushDn();
        SemifrontierCollector.Close();
        TimeClose += Timer.Elapsed;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"UpDownCollector: collect={TimeCollect} close={TimeClose}");
    }
}
