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
        if (info.Multislide) throw new Exception("PuzzleInfo.Multislide should not be set");

        var totalTime = Stopwatch.StartNew();

        GpuSolver.Initialize(info.Width, info.Height);
        Console.WriteLine(info);
        var results = new List<long>();

        
        using var frontier = new Frontier(info, "E:/PUZ/frontier.1.1", "F:/PUZ/frontier.1.2", "G:/PUZ/frontier.1.3", "H:/PUZ/frontier.1.4", "H:/PUZ/frontier.1.5");
        using var newFrontier = new Frontier(info, "G:/PUZ/frontier.2.1", "G:/PUZ/frontier.2.2", "H:/PUZ/frontier.2.3", "E:/PUZ/frontier.2.4", "F:/PUZ/frontier.2.5");
        using var semiFrontierUp = new SegmentedFile(info.SegmentsCount, "H:/PUZ/semifrontier.up.1", "H:/PUZ/semifrontier.up.2", "E:/PUZ/semifrontier.up.3", "F:/PUZ/semifrontier.up.4", "G:/PUZ/semifrontier.up.5");
        using var semiFrontierDown = new SegmentedFile(info.SegmentsCount, "F:/PUZ/semifrontier.dn.1", "E:/PUZ/semifrontier.dn.2", "H:/PUZ/semifrontier.dn.3", "G:/PUZ/semifrontier.dn.4", "H:/PUZ/semifrontier.dn.5");
        
        /*
        using var frontier = new Frontier(info, "c:/PUZ/frontier.1");
        using var newFrontier = new Frontier(info, "d:/PUZ/frontier.2");
        using var semiFrontierUp = new SegmentedFile(info.SegmentsCount, "c:/PUZ/semifrontier.up");
        using var semiFrontierDown = new SegmentedFile(info.SegmentsCount, "d:/PUZ/semifrontier.dn");
        */

        List<uint[]> valsBuffersList = new List<uint[]>();
        List<uint[]> valsBuffersList2 = new List<uint[]>();
        List<uint[]> valsBuffersList3 = new List<uint[]>();
        List<byte[]> statesBuffersList = new List<byte[]>();
        List<byte[]> tempBuffersList = new List<byte[]>();
        List<byte[]> tempBuffersList2 = new List<byte[]>();
        List<byte[]> tempBuffersList3 = new List<byte[]>();
        List<FrontierCollector> frontierCollectorsList = new List<FrontierCollector>();

        for (int i = 0; i < PuzzleInfo.THREADS; i++)
        {
            valsBuffersList.Add(new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE]);
            valsBuffersList2.Add(new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE]);
            valsBuffersList3.Add(new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE]);
            statesBuffersList.Add(new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE]);
            tempBuffersList.Add(new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4]);
            tempBuffersList2.Add(new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4]);
            tempBuffersList3.Add(new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4]);
            frontierCollectorsList.Add(new FrontierCollector(newFrontier, tempBuffersList[i], valsBuffersList[i], statesBuffersList[i]));
        }

        // Fill initial state
        valsBuffersList[0][0] = (uint)info.InitialIndex;
        statesBuffersList[0][0] = info.GetState(info.InitialIndex);
        frontier.Write(0, tempBuffersList[0], valsBuffersList[0], statesBuffersList[0], 1);

        TimeSpan TimerFillSemifrontier = TimeSpan.Zero;
        TimeSpan TimerFillFrontier = TimeSpan.Zero;

        var timer = new Stopwatch();
        var sw = new Stopwatch();

        Console.WriteLine($"Step: {0}; states: {1}");
        results.Add(1);
        long countSoFar = 1;

        var statesList = new List<FrontierStates>();
        for (int i = 0; i < PuzzleInfo.THREADS; i++)
        {
            statesList.Add(new FrontierStates(info));
        }

        info.Arena.Reset();

        var semifrontierCollectorUp = new SemifrontierCollector(semiFrontierUp, info);
        var semifrontierCollectorDown = new SemifrontierCollector(semiFrontierDown, info);

        var upDownCollectors = new UpDownCollector[PuzzleInfo.THREADS];
        for (int i = 0; i < PuzzleInfo.THREADS; i++)
        {
            var coll = new UpDownCollector(info, semifrontierCollectorUp, semifrontierCollectorDown);
            upDownCollectors[i] = coll;
        }

        for (int step = 1; step <= info.MaxSteps; step++)
        {
            sw.Restart();
            timer.Restart();

            // Fill semi-frontier
            {
                var tasks = new List<Task>();
                int segmentIndex = 0;
                for (int i = 0; i < upDownCollectors.Length; i++)
                {
                    
                    var task = Task.Factory.StartNew((object c) => {
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
                    }, i);
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
            }

            TimerFillSemifrontier += timer.Elapsed;
            timer.Restart();

            // Fill new frontier
            long count = 0;
            {
                var tasks = new List<Task>();
                int segmentIndex = 0;
                for (int i = 0; i < statesList.Count; i++)
                {
                    var task = Task.Factory.StartNew((object c) =>
                    {
                        int index = (int)c;
                        var state = statesList[index];
                        var frontierCollector = frontierCollectorsList[index];
                        state.Reset();
                        while (true)
                        {
                            int s = -1;
                            lock (info)
                            {
                                s = segmentIndex++;
                            }
                            if (s >= info.SegmentsCount) break;

                            // up
                            var t1 = Task.Factory.StartNew(() => {
                                for (int p = 0; p < semiFrontierUp.SegmentParts(s); p++)
                                {
                                    if (PuzzleInfo.SEMIFRONTIER_DIFF_ENCODING)
                                    {
                                        int size = semiFrontierUp.ReadSegment(s, p, tempBuffersList2[index]);
                                        int len = PackStates.UnpackVals(tempBuffersList2[index], size, valsBuffersList2[index]);
                                        lock (state)
                                        {
                                            state.AddUp(valsBuffersList2[index], len);
                                        }
                                    }
                                    else
                                    {
                                        int len = semiFrontierUp.ReadSegment(s, p, valsBuffersList2[index]);
                                        lock (state)
                                        {
                                            state.AddUp(valsBuffersList2[index], len);
                                        }
                                    }
                                }
                            });
                            // down
                            var t2 = Task.Factory.StartNew(() => {
                                for (int p = 0; p < semiFrontierDown.SegmentParts(s); p++)
                                {
                                    if (PuzzleInfo.SEMIFRONTIER_DIFF_ENCODING)
                                    {
                                        int size = semiFrontierDown.ReadSegment(s, p, tempBuffersList3[index]);
                                        int len = PackStates.UnpackVals(tempBuffersList3[index], size, valsBuffersList3[index]);
                                        lock (state)
                                        {
                                            state.AddDown(valsBuffersList3[index], len);
                                        }
                                    }
                                    else
                                    {
                                        int len = semiFrontierDown.ReadSegment(s, p, valsBuffersList3[index]);
                                        lock (state)
                                        {
                                            state.AddDown(valsBuffersList3[index], len);
                                        }
                                    }
                                }
                            });
                            // left/right
                            var t3 = Task.Factory.StartNew(() => {
                                for (int p = 0; p < frontier.SegmentParts(s); p++)
                                {
                                    int len = frontier.Read(s, p, tempBuffersList[index], valsBuffersList[index], statesBuffersList[index]);
                                    lock (state)
                                    {
                                        state.AddLeftRight(valsBuffersList[index], statesBuffersList[index], len);
                                    }
                                }
                            });
                            Task.WaitAll(new Task[] { t1, t2, t3 });

                            frontierCollector.Segment = s;
                            var localCount = state.Collect(frontierCollector);
                            lock (info)
                            {
                                count += localCount;
                            }
                        }
                    }, i);
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
            }

            long semifrontierSize = semiFrontierUp.TotalSize() + semiFrontierDown.TotalSize();
            long frontierSize = frontier.TotalSize();
            long newFrontierSize = newFrontier.TotalSize();
            long currentSize = semifrontierSize + frontierSize + newFrontierSize;

            TimerFillFrontier += timer.Elapsed;

            frontier.Swap(newFrontier);
            newFrontier.Clear();
            semiFrontierUp.Clear();
            semiFrontierDown.Clear();

            if (count == 0) break;
            results.Add(count);
            countSoFar += count;
            double percent = countSoFar * 100.0 / info.RealStates;
            Console.WriteLine(
                $"Step: {step}; states: {count:N0} time: {sw.Elapsed} ({percent:N5}% in {totalTime.Elapsed}) " + 
                $"FilesGB={Util.GB(currentSize)} ({Util.GB(frontierSize)}, {Util.GB(semifrontierSize)}, {Util.GB(newFrontierSize)})");
        }
        Console.WriteLine($"Steps: {results.Count - 1}, Total: {countSoFar:N0}, eq={countSoFar == info.RealStates}");
        Console.WriteLine($"{string.Join(" ", results)}");
        Console.WriteLine($"Total time: {totalTime.Elapsed}");
        Console.WriteLine();
        Console.WriteLine($"1) Timer.FillSemifrontier={TimerFillSemifrontier}");
        Console.WriteLine($"2) Timer.FillFrontier={TimerFillFrontier}");
        Console.WriteLine();
        GpuSolver.PrintStats();
        UpDownCollector.PrintStats();
        SemifrontierCollector.PrintStats();
        SegmentedFile.PrintStats();
        Console.WriteLine();
        Frontier.PrintStats();
        FrontierStates.PrintStats();
        PackStates.PrintStats();
        PackInts.PrintStats();
        PackBytes.PrintStats();

        info.Close();

        return results.ToArray();
    }
}
