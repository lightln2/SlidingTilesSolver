using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class PuzzleSolver
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe long[] Solve(int width, int height, int initialIndex)
    {
        var totalTime = Stopwatch.StartNew();

        var info = new PuzzleInfo(width, height, initialIndex);
        GpuSolver.Initialize(width, height);
        Console.WriteLine(info);
        var results = new List<long>();

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

        var states = new FrontierStates(info);

        TimeSpan S1 = TimeSpan.Zero;
        TimeSpan S2 = TimeSpan.Zero;
        TimeSpan S3 = TimeSpan.Zero;
        TimeSpan S4 = TimeSpan.Zero;
        TimeSpan S6 = TimeSpan.Zero;

        Console.WriteLine($"Step: {0}; states: {1}");
        results.Add(1);

        for (int step = 1; step < 1000; step++)
        {
            var sw = Stopwatch.StartNew();

            // 1. states
            foreach (long val in list)
            {
                if ((val & PuzzleInfo.STATE_UP) != 0) upBuffer[upPos++] = val >> 4;
                if ((val & PuzzleInfo.STATE_DN) != 0) dnBuffer[dnPos++] = val >> 4;
            }

            S1 += sw.Elapsed;

            // 2. left / right
            states.AddLeftRight(list);

            S2 += sw.Elapsed;

            GpuSolver.CalcGPU(upPos, true, upBuffer);
            GpuSolver.CalcGPU(dnPos, false, dnBuffer);
            upPos = 0;
            dnPos = 0;

            S3 += sw.Elapsed;

            states.AddUp(upBuffer, upPos);
            states.AddDown(dnBuffer, dnPos);

            S4 += sw.Elapsed;

            list.Clear();
            long count = states.Collect(list);

            S6 += sw.Elapsed;

            if (count == 0) break;
            results.Add(count);
            Console.WriteLine($"Step: {step}; states: {count} non-terminal: {list.Count} time: {sw.Elapsed}");
        }
        Console.WriteLine($"Total time: {totalTime.Elapsed}");
        GpuSolver.PrintStats();
        Console.WriteLine($"S1={S1}");
        Console.WriteLine($"S2={S2}");
        Console.WriteLine($"S3={S3}");
        Console.WriteLine($"S4={S4}");
        Console.WriteLine($"S6={S6}");
        return results.ToArray();
    }
}
