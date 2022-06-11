using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MultislideSolver
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
        Console.WriteLine("** Multislide **");
        Console.WriteLine(info);
        var results = new List<long>();

        /*
        using var frontier = new MultislideFrontier(info, "E:/PUZ/frontier.1.1", "F:/PUZ/frontier.1.2", "G:/PUZ/frontier.1.3", "H:/PUZ/frontier.1.4", "H:/PUZ/frontier.1.5");
        using var newFrontier = new MultislideFrontier(info, "G:/PUZ/frontier.2.1", "G:/PUZ/frontier.2.2", "H:/PUZ/frontier.2.3", "E:/PUZ/frontier.2.4", "F:/PUZ/frontier.2.5");
        using var semiFrontier = new SegmentedFile(info.SegmentsCount, 
            "H:/PUZ/semifrontier.1", "H:/PUZ/semifrontier.2", "E:/PUZ/semifrontier.3", "F:/PUZ/semifrontier.4", "G:/PUZ/semifrontier.5",
            "F:/PUZ/semifrontier.6", "E:/PUZ/semifrontier.7", "H:/PUZ/semifrontier.8", "G:/PUZ/semifrontier.9", "H:/PUZ/semifrontier.A");
        */

        using var frontierUpDn = new MultislideFrontier(info, "c:/PUZ/frontier.up.dn");
        using var frontierLtRt = new MultislideFrontier(info, "c:/PUZ/frontier.lt.rt");
        using var newFrontierUpDn = new MultislideFrontier(info, "d:/PUZ/frontier.new.up.dn");
        using var newFrontierLtRt = new MultislideFrontier(info, "d:/PUZ/frontier.new.lt.rt");
        using var semiFrontier = new SegmentedFile(info.SegmentsCount, "c:/PUZ/semifrontier");

        var valsBuffersList = new List<uint[]>();
        var valsBuffersList2 = new List<uint[]>();
        var valsBuffersList3 = new List<uint[]>();
        var tempBuffersList = new List<byte[]>();

        var valsBuffersListXX = new List<uint[]>();
        var tempBuffersListXX = new List<byte[]>();

        var frontierCollectorsListUpDn = new List<MultislideFrontierCollector>();
        var frontierCollectorsListLtRt = new List<MultislideFrontierCollector>();

        for (int i = 0; i < PuzzleInfo.THREADS; i++)
        {
            valsBuffersList.Add(new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE]);
            valsBuffersList2.Add(new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE]);
            valsBuffersList3.Add(new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE]);
            tempBuffersList.Add(new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4]);
            valsBuffersListXX.Add(new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE]);
            tempBuffersListXX.Add(new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4]);
            frontierCollectorsListUpDn.Add(new MultislideFrontierCollector(newFrontierUpDn, tempBuffersList[i], valsBuffersList[i]));
            frontierCollectorsListLtRt.Add(new MultislideFrontierCollector(newFrontierLtRt, tempBuffersListXX[i], valsBuffersListXX[i]));
        }

        // Fill initial state
        valsBuffersList[0][0] = (uint)info.InitialIndex;
        frontierUpDn.Write(0, tempBuffersList[0], valsBuffersList[0], 1);
        frontierLtRt.Write(0, tempBuffersList[0], valsBuffersList[0], 1);

        TimeSpan TimerFillSemifrontier = TimeSpan.Zero;
        TimeSpan TimerFillFrontier = TimeSpan.Zero;

        var timer = new Stopwatch();
        var sw = new Stopwatch();

        Console.WriteLine($"Step: {0}; states: {1}");
        results.Add(1);
        long countSoFar = 1;

        var statesList = new List<MultislideFrontierStates>();
        for (int i = 0; i < PuzzleInfo.THREADS; i++)
        {
            statesList.Add(new MultislideFrontierStates(info));
        }

        info.Arena.Reset();

        var semifrontierCollector = new SemifrontierCollector(semiFrontier, info);

        var upDownCollectors = new MultislideUpDownCollector[PuzzleInfo.THREADS];
        for (int i = 0; i < PuzzleInfo.THREADS; i++)
        {
            var coll = new MultislideUpDownCollector(info, semifrontierCollector);
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
                            for (int p = 0; p < frontierUpDn.SegmentParts(s); p++)
                            {
                                int len = frontierUpDn.Read(s, p, tempBuffersList[index], valsBuffersList[index]);
                                collector.Collect(s, valsBuffersList[index], len);
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
                        var frontierCollectorUpDn = frontierCollectorsListUpDn[index];
                        var frontierCollectorLtRt = frontierCollectorsListLtRt[index];
                        state.Reset();
                        while (true)
                        {
                            int s = -1;
                            lock (info)
                            {
                                s = segmentIndex++;
                            }
                            if (s >= info.SegmentsCount) break;

                            // up / down
                            var t1 = Task.Factory.StartNew(() => {
                                for (int p = 0; p < semiFrontier.SegmentParts(s); p++)
                                {
                                    int len = semiFrontier.ReadSegment(s, p, valsBuffersList2[index]);
                                    lock(state)
                                    {
                                        state.AddUpDown(valsBuffersList2[index], len);
                                    }
                                }
                            });
                            // left/right
                            var t2 = Task.Factory.StartNew(() => {
                                for (int p = 0; p < frontierLtRt.SegmentParts(s); p++)
                                {
                                    int len = frontierLtRt.Read(s, p, tempBuffersList[index], valsBuffersList[index]);
                                    lock (state)
                                    {
                                        state.AddLeftRight(valsBuffersList[index], len);
                                    }
                                }
                            });
                            Task.WaitAll(new Task[] { t1, t2 });

                            // Exclude current state

                            var t3 = Task.Factory.StartNew(() => {
                                for (int p = 0; p < frontierUpDn.SegmentParts(s); p++)
                                {
                                    int len = frontierUpDn.Read(s, p, tempBuffersList[index], valsBuffersList[index]);
                                    lock (state)
                                    {
                                        state.Exclude(valsBuffersList[index], len);
                                    }
                                }
                            });

                            var t4 = Task.Factory.StartNew(() => {
                                for (int p = 0; p < frontierLtRt.SegmentParts(s); p++)
                                {
                                    int len = frontierLtRt.Read(s, p, tempBuffersListXX[index], valsBuffersListXX[index]);
                                    lock (state)
                                    {
                                        state.Exclude(valsBuffersListXX[index], len);
                                    }
                                }
                            });

                            Task.WaitAll(new Task[] { t3, t4 });

                            frontierCollectorUpDn.Segment = s;
                            frontierCollectorLtRt.Segment = s;
                            var localCount = state.Collect(frontierCollectorUpDn, frontierCollectorLtRt);
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

            TimerFillFrontier += timer.Elapsed;

            frontierUpDn.Swap(newFrontierUpDn);
            frontierLtRt.Swap(newFrontierLtRt);
            newFrontierUpDn.Clear();
            newFrontierLtRt.Clear();
            semiFrontier.Clear();

            if (count == 0) break;
            results.Add(count);
            countSoFar += count;
            Console.WriteLine($"Step: {step}; states: {count:N0} time: {sw.Elapsed} ({(countSoFar * 100.0 / info.RealStates):N5}% in {totalTime.Elapsed})");
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
