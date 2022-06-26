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
        if (!info.Multislide) throw new Exception("PuzzleInfo.Multislide should be set");
        var totalTime = Stopwatch.StartNew();

        GpuSolver.Initialize(info.Width, info.Height);
        Console.WriteLine("** Multislide **");
        Console.WriteLine(info);
        var results = new List<long>();


        using var frontierUpDn = new MultislideFrontier(info, 
            "E:/PUZ/frontier.up.dn.1", "F:/PUZ/frontier.up.dn.2", "G:/PUZ/frontier.up.dn.3", "H:/PUZ/frontier.up.dn.4", "H:/PUZ/frontier.up.dn.5");
        using var frontierLtRt = new MultislideFrontier(info, 
            "F:/PUZ/frontier.lt.rt.1", "G:/PUZ/frontier.lt.rt.2", "H:/PUZ/frontier.lt.rt.3", "H:/PUZ/frontier.lt.rt.4", "E:/PUZ/frontier.lt.rt.5");
        using var newFrontierUpDn = new MultislideFrontier(info, 
            "G:/PUZ/frontier.new.up.dn.1", "H:/PUZ/frontier.new.up.dn.2", "H:/PUZ/frontier.new.up.dn.3", "E:/PUZ/frontier.new.up.dn.4", "F:/PUZ/frontier.new.up.dn.5");
        using var newFrontierLtRt = new MultislideFrontier(info, 
            "H:/PUZ/frontier.new.lt.rt.1", "H:/PUZ/frontier.new.lt.rt.2", "E:/PUZ/frontier.new.lt.rt.3", "F:/PUZ/frontier.new.lt.rt.4", "G:/PUZ/frontier.new.lt.rt.5");
        using var semiFrontier = new SegmentedFile(info.SegmentsCount, 
            "H:/PUZ/semifrontier.1", "E:/PUZ/semifrontier.2", "F:/PUZ/semifrontier.3", "G:/PUZ/semifrontier.4", "H:/PUZ/semifrontier.5");

        /*
        using var frontierUpDn = new MultislideFrontier(info, "c:/PUZ/frontier.up.dn", "d:/PUZ/frontier.up.dn");
        using var frontierLtRt = new MultislideFrontier(info, "c:/PUZ/frontier.lt.rt", "d:/PUZ/frontier.lt.rt");
        using var newFrontierUpDn = new MultislideFrontier(info, "c:/PUZ/frontier.new.up.dn", "d:/PUZ/frontier.new.up.dn");
        using var newFrontierLtRt = new MultislideFrontier(info, "c:/PUZ/frontier.new.lt.rt", "d:/PUZ/frontier.new.lt.rt");
        using var semiFrontier = new SegmentedFile(info.SegmentsCount, "c:/PUZ/semifrontier", "d:/PUZ/semifrontier");
        */

        var valsBuffersList = new List<uint[]>();
        var valsBuffersList2 = new List<uint[]>();

        var tempBuffersList = new List<byte[]>();
        var tempBuffersList2 = new List<byte[]>();

        var frontierCollectorsListUpDn = new List<MultislideFrontierCollector>();
        var frontierCollectorsListLtRt = new List<MultislideFrontierCollector>();

        for (int i = 0; i < PuzzleInfo.THREADS; i++)
        {
            valsBuffersList.Add(new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE]);
            valsBuffersList2.Add(new uint[PuzzleInfo.FRONTIER_BUFFER_SIZE]);
            tempBuffersList.Add(new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4]);
            tempBuffersList2.Add(new byte[PuzzleInfo.FRONTIER_BUFFER_SIZE * 4]);
            frontierCollectorsListUpDn.Add(new MultislideFrontierCollector(newFrontierUpDn, tempBuffersList[i], valsBuffersList[i]));
            frontierCollectorsListLtRt.Add(new MultislideFrontierCollector(newFrontierLtRt, tempBuffersList2[i], valsBuffersList2[i]));
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

        var semifrontierCollector = new MultislideSemifrontierCollector(semiFrontier, info);

        var upDownCollectors = new IMultislideUpDownCollector[PuzzleInfo.THREADS];
        for (int i = 0; i < PuzzleInfo.THREADS; i++)
        {
            IMultislideUpDownCollector coll = info.Height == 2 ?
                new MultislideUpDownCollector2(info, semifrontierCollector) :
                new MultislideUpDownCollector(info, semifrontierCollector);
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

            // frontierUpDn is not needed if Height is 2
            if (info.Height == 2)
            {
                frontierUpDn.Clear();
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
                            lock (info) s = segmentIndex++;
                            if (s >= info.SegmentsCount) break;

                            // add up/down
                            for (int p = 0; p < semiFrontier.SegmentParts(s); p++)
                            {
                                if (PuzzleInfo.SEMIFRONTIER_DIFF_ENCODING)
                                {
                                    int size = semiFrontier.ReadSegment(s, p, tempBuffersList[index]);
                                    int len = PackStates.UnpackVals(tempBuffersList[index], size, valsBuffersList[index]);
                                    state.AddUpDown(valsBuffersList[index], len);
                                }
                                else
                                {
                                    int len = semiFrontier.ReadSegment(s, p, valsBuffersList[index]);
                                    state.AddUpDown(valsBuffersList[index], len);
                                }
                            }

                            // addd left/right and exclude left / right
                            for (int p = 0; p < frontierLtRt.SegmentParts(s); p++)
                            {
                                int len = frontierLtRt.Read(s, p, tempBuffersList[index], valsBuffersList[index]);
                                state.AddLeftRightAndExclude(valsBuffersList[index], len);
                            }
                            state.FinishAddLeftRightAndExclude();

                            // Exclude up/down
                            if (info.Height != 2)
                            {
                                for (int p = 0; p < frontierUpDn.SegmentParts(s); p++)
                                {
                                    int len = frontierUpDn.Read(s, p, tempBuffersList[index], valsBuffersList[index]);
                                    state.Exclude(valsBuffersList[index], len);
                                }
                            }

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

            long semifrontierSize = semiFrontier.TotalSize();
            long frontierSize = frontierUpDn.TotalSize() + frontierLtRt.TotalSize();
            long newFrontierSize = newFrontierUpDn.TotalSize() + newFrontierLtRt.TotalSize();
            long currentSize = semifrontierSize + frontierSize + newFrontierSize;

            frontierUpDn.Swap(newFrontierUpDn);
            frontierLtRt.Swap(newFrontierLtRt);
            newFrontierUpDn.Clear();
            newFrontierLtRt.Clear();
            semiFrontier.Clear();

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
        if (info.Height == 2) MultislideUpDownCollector2.PrintStats();
        else MultislideUpDownCollector.PrintStats();
        MultislideSemifrontierCollector.PrintStats();
        SegmentedFile.PrintStats();
        Console.WriteLine();
        MultislideFrontier.PrintStats();
        MultislideFrontierStates.PrintStats();
        PackStates.PrintStats();
        PackInts.PrintStats();

        info.Close();

        return results.ToArray();
    }
}
