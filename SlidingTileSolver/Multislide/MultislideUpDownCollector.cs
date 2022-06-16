using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public unsafe class MultislideUpDownCollector : IMultislideUpDownCollector
{
    private readonly PuzzleInfo Info;
    public readonly MultislideSemifrontierCollector SemifrontierCollector;
    private int BufferLength;
    private long*[] RowBuffers;
    private int[] Counts;
    private int MaxCount;

    private int[] RowMap;

    private static TimeSpan TimeCollect = TimeSpan.Zero;
    private static TimeSpan TimeClose = TimeSpan.Zero;
    private Stopwatch Timer = new Stopwatch();

    public MultislideUpDownCollector(PuzzleInfo info, MultislideSemifrontierCollector semifrontierCollector)
    {
        Info = info;
        SemifrontierCollector = semifrontierCollector;
        RowBuffers = new long*[info.Height];
        Counts = new int[info.Height];
        BufferLength = 2 * GpuSolver.GPUSIZE / info.Height;
        MaxCount = BufferLength / (info.Height - 1);
        for (int i = 0; i < info.Height; i++)
        {
            RowBuffers[i] = info.Arena.Alloclong(BufferLength);
        }

        RowMap = new int[16];
        for (int i = 0; i < 16; i++)
        {
            if (i >= info.Size) RowMap[i] = -1;
            else RowMap[i] = i / Info.Width;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Collect(int segment, uint[] vals, int len)
    {
        Timer.Restart();
        long baseIndex = ((long)segment << PuzzleInfo.SEGMENT_SIZE_POW);
        for (int i = 0; i < len; i++)
        {
            long val = baseIndex | vals[i];
            int row = RowMap[vals[i] & 15];
            RowBuffers[row][Counts[row]] = val;
            Counts[row]++;
            if (Counts[row] >= MaxCount)
            {
                Flush(row);
            }
        }
        TimeCollect += Timer.Elapsed;
    }

    private void Flush(int row)
    {
        int count = Counts[row];
        if (count == 0) return;
        GpuSolver.CalcGPU_Multimove(count, RowBuffers[row], row);
        SemifrontierCollector.Collect(RowBuffers[row], count * (Info.Height - 1));

        Counts[row] = 0;
    }

    public void Close()
    {
        Timer.Restart();
        for (int i = 0; i < Info.Height; i++)
        {
            Flush(i);
        }
        SemifrontierCollector.Close();
        TimeClose += Timer.Elapsed;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"MultislideUpDownCollector: collect={TimeCollect} close={TimeClose}");
    }
}
