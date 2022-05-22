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

        uint[] valsBuffer = new uint[PuzzleInfo.SEMIFRONTIER_BUFFER_SIZE];
        byte[] statesBuffer = new byte[PuzzleInfo.SEMIFRONTIER_BUFFER_SIZE];
        // Fill initial state
        valsBuffer[0] = (uint)(initialIndex);
        statesBuffer[0] = (byte)(info.GetState(initialIndex));
        frontier.Write(0, valsBuffer, statesBuffer, 1);

        using var semiFrontierUp = new SegmentedFile("c:/PUZ/semifrontier.up", info.SegmentsCount);
        using var semiFrontierDown = new SegmentedFile("c:/PUZ/semifrontier.dn", info.SegmentsCount);

        TimeSpan TimerFillSemifrontier = TimeSpan.Zero;
        TimeSpan TimerAddUpDown = TimeSpan.Zero;
        TimeSpan TimerAddLeftRight = TimeSpan.Zero;
        TimeSpan TimerCollect = TimeSpan.Zero;
        var timer = new Stopwatch();
        var sw = new Stopwatch();

        Console.WriteLine($"Step: {0}; states: {1}");
        results.Add(1);
        long countSoFar = 1;

        var states = new FrontierStates(info);
        info.Arena.Reset();
        var semifrontierCollectorUp = new SemifrontierCollector(semiFrontierUp, info);
        var semifrontierCollectorDown = new SemifrontierCollector(semiFrontierDown, info);
        var upDownCollector = new UpDownCollector(semifrontierCollectorUp, semifrontierCollectorDown);

        for (int step = 1; step <= PuzzleInfo.MaxSteps; step++)
        {
            sw.Restart();
            timer.Restart();

            {
                // Fill semi-frontier
                for (int s = 0; s < info.SegmentsCount; s++)
                {
                    for (int p = 0; p < frontier.SegmentParts(s); p++)
                    {
                        int len = frontier.Read(s, p, valsBuffer, statesBuffer);
                        upDownCollector.Collect(s, valsBuffer, statesBuffer, len);
                    }
                }

                upDownCollector.Close();

                TimerFillSemifrontier += timer.Elapsed;

            }
            // Fill new frontier

            long count = 0;
            states.Reset();

            for (int s = 0; s < info.SegmentsCount; s++)
            {
                timer.Restart();
                // up
                for (int p = 0; p < semiFrontierUp.SegmentParts(s); p++)
                {
                    int len = semiFrontierUp.ReadSegment(s, p, valsBuffer);
                    states.AddUp(valsBuffer, len);
                }
                // down
                for (int p = 0; p < semiFrontierDown.SegmentParts(s); p++)
                {
                    int len = semiFrontierDown.ReadSegment(s, p, valsBuffer);
                    states.AddDown(valsBuffer, len);
                }

                TimerAddUpDown += timer.Elapsed;
                timer.Restart();

                for (int p = 0; p < frontier.SegmentParts(s); p++)
                {
                    int len = frontier.Read(s, p, valsBuffer, statesBuffer);
                    states.AddLeftRight(valsBuffer, statesBuffer, len);
                }

                TimerAddLeftRight += timer.Elapsed;
                timer.Restart();

                var frontierCollector = new FrontierCollector(newFrontier, s, valsBuffer, statesBuffer);
                count += states.Collect(frontierCollector);

                TimerCollect += timer.Elapsed;
            }

            var tmp = frontier;
            frontier = newFrontier;
            newFrontier = tmp;
            newFrontier.Clear();
            semiFrontierUp.Clear();
            semiFrontierDown.Clear();

            if (count == 0) break;
            results.Add(count);
            countSoFar += count;
            Console.WriteLine($"Step: {step}; states: {count:N0} time: {sw.Elapsed} ({(countSoFar * 100.0 / info.RealStates):N5}% in {totalTime.Elapsed})");
        }
        Console.WriteLine($"Steps: {results.Count - 1}, Total: {countSoFar:N0}, eq={countSoFar == info.RealStates}");
        Console.WriteLine($"{string.Join(" ", results)}");
        Console.WriteLine($"Total time: {totalTime.Elapsed}");
        Console.WriteLine();
        Console.WriteLine($"Timer.FillSemifrontier={TimerFillSemifrontier}");
        GpuSolver.PrintStats();
        UpDownCollector.PrintStats();
        SemifrontierCollector.PrintStats();
        SegmentedFile.PrintStats();
        Console.WriteLine();
        Console.WriteLine($"Timer.AddUpDown={TimerAddUpDown}");
        Console.WriteLine($"Timer.AddLeftRight={TimerAddLeftRight}");
        Console.WriteLine($"Timer.FrontierCollector.Collect={TimerCollect}");
        SegmentedFileByte.PrintStats();
        Frontier.PrintStats();
        FrontierStates.PrintStats();
        PackStates.PrintStats();
        PackInts.PrintStats();
        PackBytes.PrintStats();
        frontier.Dispose();
        newFrontier.Dispose();
        return results.ToArray();
    }
}
