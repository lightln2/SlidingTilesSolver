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
    private int BufferLength;
    private long*[] UpBuffers;
    private long*[] DnBuffers;
    private int[] Counts;

    private int[] RowMap;

    private static TimeSpan TimeCollect = TimeSpan.Zero;
    private static TimeSpan TimeClose = TimeSpan.Zero;
    private Stopwatch Timer = new Stopwatch();

    public MultislideUpDownCollector(PuzzleInfo info, SemifrontierCollector semifrontierCollector)
    {
        Info = info;
        SemifrontierCollector = semifrontierCollector;
        UpBuffers = new long*[info.Height];
        DnBuffers = new long*[info.Height];
        Counts = new int[info.Height];
        BufferLength = GpuSolver.GPUSIZE / info.Height;
        for (int i = 0; i < info.Height; i++)
        {
            UpBuffers[i] = info.Arena.Alloclong(BufferLength);
            DnBuffers[i] = info.Arena.Alloclong(BufferLength);
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
            UpBuffers[row][Counts[row]] = val;
            DnBuffers[row][Counts[row]] = val;
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
        GpuSolver.CalcGPU_MultislideUp(Counts[row], UpBuffers[row], row, () => {
            SemifrontierCollector.Collect(UpBuffers[row], Counts[row]);
        });
        GpuSolver.CalcGPU_MultislideDown(Counts[row], DnBuffers[row], Info.Height - row - 1, () => {
            SemifrontierCollector.Collect(DnBuffers[row], Counts[row]);
        });
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
        Console.WriteLine($"UpDownCollector: collect={TimeCollect} close={TimeClose}");
    }
}
