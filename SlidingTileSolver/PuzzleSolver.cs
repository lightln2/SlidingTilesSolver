using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class PuzzleSolver
{
    public static unsafe long[] Solve(int width, int height, int initialIndex)
    {
        var info = new PuzzleInfo(width, height, initialIndex);
        return Solve(info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe long[] Solve(PuzzleInfo info)
    {
        var totalTime = Stopwatch.StartNew();

        GpuSolver.Initialize(info.Width, info.Height);
        Console.WriteLine(info);
        var results = new List<long>();

        var frontier = new Frontier(info, "c:/PUZ/frontier.1-p1", "d:/PUZ/frontier.1-p2");
        var newFrontier = new Frontier(info, "d:/PUZ/frontier.2-p1", "c:/PUZ/frontier.2-p2");

        using var semiFrontierUp = new SegmentedFile(info.SegmentsCount, "c:/PUZ/semifrontier.up-p1", "d:/PUZ/semifrontier.up-p2");
        using var semiFrontierDown = new SegmentedFile(info.SegmentsCount, "d:/PUZ/semifrontier.dn-p1", "c:/PUZ/semifrontier.dn-p2");

        List<uint[]> valsBuffersList = new List<uint[]>()
        {
            new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE],
            new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE],
            new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE],
            new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE],
        };
        List<byte[]> statesBuffersList = new List<byte[]>()
        {
            new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE],
            new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE],
            new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE],
            new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE],
        };
        List<byte[]> tempBuffersList = new List<byte[]>()
        {
            new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4],
            new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4],
            new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4],
            new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4],
        };

        // Fill initial state
        valsBuffersList[0][0] = (uint)info.InitialIndex;
        statesBuffersList[0][0] = info.GetState(info.InitialIndex);
        frontier.Write(0, tempBuffersList[0], valsBuffersList[0], statesBuffersList[0], 1);

        TimeSpan TimerFillSemifrontier = TimeSpan.Zero;
        TimeSpan TimerAddUpDown = TimeSpan.Zero;
        TimeSpan TimerAddLeftRight = TimeSpan.Zero;
        TimeSpan TimerCollect = TimeSpan.Zero;
        var timer = new Stopwatch();
        var sw = new Stopwatch();

        Console.WriteLine($"Step: {0}; states: {1}");
        results.Add(1);
        long countSoFar = 1;

        var statesList = new List<FrontierStates>() 
        { 
            new FrontierStates(info),
            new FrontierStates(info),
            new FrontierStates(info),
            new FrontierStates(info),
        };

        info.Arena.Reset();

        var semifrontierCollectorUp = new SemifrontierCollector(semiFrontierUp, info);
        var semifrontierCollectorDown = new SemifrontierCollector(semiFrontierDown, info);

        var upDownCollectors = new UpDownCollector[]
        {
            new UpDownCollector(info, semifrontierCollectorUp, semifrontierCollectorDown),
            new UpDownCollector(info, semifrontierCollectorUp, semifrontierCollectorDown),
            new UpDownCollector(info, semifrontierCollectorUp, semifrontierCollectorDown),
            new UpDownCollector(info, semifrontierCollectorUp, semifrontierCollectorDown),
        };

        for (int step = 1; step <= info.MaxSteps; step++)
        {
            sw.Restart();
            timer.Restart();

            // Fill semi-frontier
            {
                var threads = new List<Thread>();
                int segmentIndex = 0;
                for (int i = 0; i < upDownCollectors.Length; i++)
                {
                    var thread = new Thread(new ParameterizedThreadStart((object c) => {
                        int index = (int)c;
                        var collector = upDownCollectors[index];
                        while(true)
                        {
                            int s = -1;
                            lock (info)
                            {
                                s = segmentIndex++;
                            }
                            if (s >= info.SegmentsCount) break;
                            for (int p = 0; p < frontier.SegmentParts(s); p++)
                            {
                                int len = frontier.Read(s, p, tempBuffersList[index], valsBuffersList[index], statesBuffersList[index]);
                                collector.Collect(s, valsBuffersList[index], statesBuffersList[index], len);
                            }
                        }
                        collector.Close();
                    }));
                    threads.Add(thread);
                    thread.Start(i);
                }
                foreach (var thread in threads) thread.Join();
            }
            /*
            for (int s = 0; s < info.SegmentsCount; s++)
            {
                for (int p = 0; p < frontier.SegmentParts(s); p++)
                {
                    int len = frontier.Read(s, p, valsBuffer, statesBuffer);
                    upDownCollector.Collect(s, valsBuffer, statesBuffer, len);
                }
            }
            upDownCollector.Close();
            */

            TimerFillSemifrontier += timer.Elapsed;

            // Fill new frontier
            long count = 0;
            {
                var threads = new List<Thread>();
                int segmentIndex = 0;
                for (int i = 0; i < statesList.Count; i++)
                {
                    var thread = new Thread(new ParameterizedThreadStart((object c) =>
                    {
                        int index = (int)c;
                        var state = statesList[index];
                        state.Reset();
                        var timer = Stopwatch.StartNew();
                        while (true)
                        {
                            int s = -1;
                            lock (info)
                            {
                                s = segmentIndex++;
                            }
                            if (s >= info.SegmentsCount) break;

                            timer.Restart();
                            // up
                            for (int p = 0; p < semiFrontierUp.SegmentParts(s); p++)
                            {
                                int len = semiFrontierUp.ReadSegment(s, p, valsBuffersList[index]);
                                state.AddUp(valsBuffersList[index], len);
                            }
                            // down
                            for (int p = 0; p < semiFrontierDown.SegmentParts(s); p++)
                            {
                                int len = semiFrontierDown.ReadSegment(s, p, valsBuffersList[index]);
                                state.AddDown(valsBuffersList[index], len);
                            }

                            TimerAddUpDown += timer.Elapsed;
                            timer.Restart();

                            for (int p = 0; p < frontier.SegmentParts(s); p++)
                            {
                                int len = frontier.Read(s, p, tempBuffersList[index], valsBuffersList[index], statesBuffersList[index]);
                                state.AddLeftRight(valsBuffersList[index], statesBuffersList[index], len);
                            }

                            TimerAddLeftRight += timer.Elapsed;
                            timer.Restart();

                            var frontierCollector = new FrontierCollector(newFrontier, s, tempBuffersList[index], valsBuffersList[index], statesBuffersList[index]);
                            var localCount = state.Collect(frontierCollector);
                            lock (info)
                            {
                                count += localCount;
                                TimerCollect += timer.Elapsed;
                            }
                        }
                    }));
                    threads.Add(thread);
                    thread.Start(i);
                }
                foreach (var thread in threads) thread.Join();
            }
            /*
            {
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
            }
            */

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
        Frontier.PrintStats();
        FrontierStates.PrintStats();
        PackStates.PrintStats();
        PackInts.PrintStats();
        PackBytes.PrintStats();
        frontier.Dispose();
        newFrontier.Dispose();

        info.Close();

        return results.ToArray();
    }
}
