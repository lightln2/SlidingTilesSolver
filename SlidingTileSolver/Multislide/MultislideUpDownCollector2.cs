using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public unsafe class MultislideUpDownCollector2 : IMultislideUpDownCollector
{
    private readonly PuzzleInfo Info;
    public readonly MultislideSemifrontierCollector SemifrontierCollector;
    private int BufferLength;
    private long*[] Buffers;
    private int[] Counts;

    private int[] RowMap;

    private static TimeSpan TimeCollect = TimeSpan.Zero;
    private static TimeSpan TimeClose = TimeSpan.Zero;
    private Stopwatch Timer = new Stopwatch();

    public MultislideUpDownCollector2(PuzzleInfo info, MultislideSemifrontierCollector semifrontierCollector)
    {
        Info = info;
        SemifrontierCollector = semifrontierCollector;
        Buffers = new long*[2];
        Counts = new int[2];
        BufferLength = GpuSolver.GPUSIZE;
        Buffers[0] = info.Arena.Alloclong(BufferLength);
        Buffers[1] = info.Arena.Alloclong(BufferLength);

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
            Buffers[row][Counts[row]] = val;
            Counts[row]++;
            if (Counts[row] == BufferLength)
            {
                Flush(row);
            }
        }
        TimeCollect += Timer.Elapsed;
    }

    private void Flush(int row)
    {
        if (Counts[row] == 0) return;
        if (row == 0)
        {
            GpuSolver.CalcGPU_Down(Counts[row], Buffers[row]);
        }
        else if (row == 1)
        {
            GpuSolver.CalcGPU_Up(Counts[row], Buffers[row]);
        }
        else throw new Exception($"row={row}, should be 0 or 1");
        SemifrontierCollector.Collect(Buffers[row], Counts[row]);
        Counts[row] = 0;
    }

    public void Close()
    {
        Timer.Restart();
        Flush(0);
        Flush(1);
        SemifrontierCollector.Close();
        TimeClose += Timer.Elapsed;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"MultislideUpDownCollector2: collect={TimeCollect} close={TimeClose}");
    }
}
