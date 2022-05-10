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

        var frontier = new Frontier("d:/PUZ/frontier.1", info);
        var newFrontier = new Frontier("d:/PUZ/frontier.2", info);
        long[] buffer = new long[PuzzleInfo.FRONTIER_BUFFER_SIZE];

        // Fill initial state
        buffer[0] = (info.InitialIndex << 4) | info.GetState(initialIndex);
        frontier.Write(0, buffer, 1);

        using var semiFrontier = new SegmentedFileLong("c:/PUZ/semifrontier", info.SegmentsCount * 2);
        var semifrontierCollector = new SemifrontierCollector(semiFrontier, info);

        var states = new FrontierStates(info);
        var upDownCollector = new UpDownCollector(semifrontierCollector);

        TimeSpan S1 = TimeSpan.Zero;
        TimeSpan S2 = TimeSpan.Zero;
        TimeSpan S3 = TimeSpan.Zero;
        TimeSpan S4 = TimeSpan.Zero;
        TimeSpan S5 = TimeSpan.Zero;

        Console.WriteLine($"Step: {0}; states: {1}");
        results.Add(1);

        for (int step = 1; step < 1000; step++)
        {
            var sw = Stopwatch.StartNew();

            semiFrontier.Clear();

            // Fill semi-frontier
            for (int s = 0; s < info.SegmentsCount; s++)
            {
                for (int p = 0; p < frontier.SegmentParts(s); p++)
                {
                    int len = frontier.Read(s, p, buffer);
                    upDownCollector.Collect(buffer, len);
                }
            }

            upDownCollector.Close();

            S1 += sw.Elapsed;

            // Fill new frontier

            long count = 0;

            for (int s = 0; s < info.SegmentsCount; s++)
            {
                states.SetSegment(s);
                // up
                for (int p = 0; p < semiFrontier.SegmentParts(2 * s); p++)
                {
                    int len = semiFrontier.ReadSegment(2 * s, p, buffer);
                    states.AddUp(buffer, len);
                }

                // down
                for (int p = 0; p < semiFrontier.SegmentParts(2 * s + 1); p++)
                {
                    int len = semiFrontier.ReadSegment(2 * s + 1, p, buffer);
                    states.AddDown(buffer, len);
                }

                for (int p = 0; p < frontier.SegmentParts(s); p++)
                {
                    int len = frontier.Read(s, p, buffer);
                    states.AddLeftRight(buffer, len);
                }

                var frontierCollector = new FrontierCollector(newFrontier, s);
                count += states.Collect(frontierCollector);
            }

            S2 += sw.Elapsed;

            var tmp = frontier;
            frontier = newFrontier;
            newFrontier = tmp;
            newFrontier.Clear();
            semiFrontier.Clear();

            S5 += sw.Elapsed;

            if (count == 0) break;
            results.Add(count);
            Console.WriteLine($"Step: {step}; states: {count:N0} time: {sw.Elapsed}");
        }
        Console.WriteLine($"Total time: {totalTime.Elapsed}");
        GpuSolver.PrintStats();
        Console.WriteLine($"S1={S1}");
        Console.WriteLine($"S2={S2}");
        Console.WriteLine($"S3={S3}");
        Console.WriteLine($"S4={S4}");
        Console.WriteLine($"S5={S5}");
        frontier.Dispose();
        newFrontier.Dispose();
        return results.ToArray();
    }
}
