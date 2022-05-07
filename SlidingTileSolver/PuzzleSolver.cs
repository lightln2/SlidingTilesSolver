using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PuzzleSolver
{
    public static void Solve(int width, int height, int initialIndex)
    {
        var totalTime = Stopwatch.StartNew();

        var info = new PuzzleInfo(width, height, initialIndex);
        GpuSolver.Initialize(width, height);
        Console.WriteLine(info);
        /*
        using var frontier = new SegmentedFile("d:/PUZ/frontier.1", info.SegmentsCount * 16);
        using var newFrontier = new SegmentedFile("d:/PUZ/frontier.2", info.SegmentsCount * 16);
        using var semiFrontier = new SegmentedFile("c:/PUZ/semifrontier", info.SegmentsCount * 2);

        uint[] buffer = new uint[4 * 1024 * 1024];

        // 1. Fill initial state

        buffer[0] = (uint)((info.InitialIndex << 4) | info.GetState(initialIndex));
        frontier.WriteSegment(0, buffer, 0, 1);


        */

        var upBuffer = new long[GpuSolver.GPUSIZE];
        int upPos = 0;
        var dnBuffer = new long[GpuSolver.GPUSIZE];
        int dnPos = 0;

        var list = new List<long>();
        list.Add((info.InitialIndex << 4) | info.GetState(initialIndex));

        var states = new byte[info.Total];

        TimeSpan S0 = TimeSpan.Zero;
        TimeSpan S1 = TimeSpan.Zero;
        TimeSpan S2 = TimeSpan.Zero;
        TimeSpan S3 = TimeSpan.Zero;
        TimeSpan S4 = TimeSpan.Zero;
        TimeSpan S5 = TimeSpan.Zero;
        TimeSpan S6 = TimeSpan.Zero;

        Console.WriteLine($"Step: {0}; states: {1}");

        for (int step = 1; step < 1000; step++)
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < states.Length; i++) states[i] = 0;
            upPos = 0;
            dnPos = 0;

            S0 += sw.Elapsed;

            // 1. states
            foreach (long val in list)
            {
                long index = val >> 4;

                if ((val & PuzzleInfo.STATE_UP) != 0) upBuffer[upPos++] = index;
                if ((val & PuzzleInfo.STATE_DN) != 0) dnBuffer[dnPos++] = index;
                if ((val & PuzzleInfo.STATE_LT) != 0) states[index - 1] |= PuzzleInfo.STATE_RT;
                if ((val & PuzzleInfo.STATE_RT) != 0) states[index + 1] |= PuzzleInfo.STATE_LT;
            }

            S1 += sw.Elapsed;

            if (upPos > 0)
            {
                GpuSolver.CalcGPU(upPos, true, upBuffer);
            }
            if (dnPos > 0)
            {
                GpuSolver.CalcGPU(dnPos, false, dnBuffer);
            }

            S2 += sw.Elapsed;

            for (int i = 0; i < upPos; i++)
            {
                states[upBuffer[i]] |= PuzzleInfo.STATE_DN;
            }
            for (int i = 0; i < dnPos; i++)
            {
                states[dnBuffer[i]] |= PuzzleInfo.STATE_UP;
            }

            S3 += sw.Elapsed;

            for (long i = 0; i < states.Length; i++)
            {
                if (states[i] == 0) continue;
                if (!info.CanGoUp(i)) states[i] |= PuzzleInfo.STATE_UP;
                if (!info.CanGoDown(i)) states[i] |= PuzzleInfo.STATE_DN;
                if (!info.CanGoLeft(i)) states[i] |= PuzzleInfo.STATE_LT;
                if (!info.CanGoRight(i)) states[i] |= PuzzleInfo.STATE_RT;
            }

            S4 += sw.Elapsed;

            foreach (long val in list)
            {
                long index = val >> 4;
                states[index] = 0;
            }

            S5 += sw.Elapsed;

            list.Clear();
            long count = 0;
            for (long i = 0; i < states.Length; i++)
            {
                byte s = states[i];
                if (s == 0) continue;
                count++;
                if (s == 15) continue;
                list.Add((i << 4) | (byte)(~s & 0xF));
            }

            S6 += sw.Elapsed;

            if (count == 0) break;
            Console.WriteLine($"Step: {step}; states: {count} time: {sw.Elapsed}");
        }
        Console.WriteLine($"Total time: {totalTime.Elapsed}");
        GpuSolver.PrintStats();
        Console.WriteLine($"S0={S0}");
        Console.WriteLine($"S1={S1}");
        Console.WriteLine($"S2={S2}");
        Console.WriteLine($"S3={S3}");
        Console.WriteLine($"S4={S4}");
        Console.WriteLine($"S5={S5}");
        Console.WriteLine($"S6={S6}");
    }
}
