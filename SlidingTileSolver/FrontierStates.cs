using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class FrontierStates
{
    public static long BaseIndexUpDownMask = (1L << PuzzleInfo.SEGMENT_SIZE_POW) - 1;
    public static long BaseIndexLeftRightMask = (16L << PuzzleInfo.SEGMENT_SIZE_POW) - 1;
    private static TimeSpan TimeCollect = TimeSpan.Zero;
    const int STATES_MAP_SKIP_POW = 12;

    private readonly long StatesMapLength;

    private byte[] StatesMap;
    private ulong[] States;

    private ulong[] LeftRightMap;

    private byte[] Bounds;
    private byte[] CollectCounts;
    private byte[] CollectMap1;
    private byte[] CollectMap2;

    private long BaseIndex;

    private Stopwatch Timer = new Stopwatch();
    
    public void SetSegment(long segment)
    {
        BaseIndex = segment << PuzzleInfo.SEGMENT_SIZE_POW;
    }

    public FrontierStates(PuzzleInfo info)
    {
        StatesMapLength = info.StatesMapLength / 16;
        States = new ulong[StatesMapLength];
        StatesMap = new byte[(StatesMapLength >> (STATES_MAP_SKIP_POW - 4)) + 1];

        Bounds = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            byte s = (byte)~info.GetState(i);
            Bounds[i] = s;
        }

        LeftRightMap = new ulong[256];

        for (int b = 0; b < 256; b++)
        {
            ulong x = 0;
            int index = b >> 4;
            int state = b & 15;
            if ((state & PuzzleInfo.STATE_LT) != 0) x |= (ulong)(PuzzleInfo.STATE_RT) << ((index - 1) * 4);
            if ((state & PuzzleInfo.STATE_RT) != 0) x |= (ulong)(PuzzleInfo.STATE_LT) << ((index + 1) * 4);
            LeftRightMap[b] = x;
        }

        CollectCounts = new byte[256];
        CollectMap1 = new byte[8 * 256];
        CollectMap2 = new byte[8 * 256];

        for (int s = 0; s < 256; s++)
        {
            byte s1 = (byte)(s & 15);
            byte s2 = (byte)((s >> 4) & 15);
            byte count = 0;
            if (s1 != 0) count++;
            if (s2 != 0) count++;
            CollectCounts[s] = count;
        }

        for (int j = 0; j < 8; j++)
        {
            for (int s = 0; s < 256; s++)
            {
                byte s1 = (byte)(s & 15);
                byte s2 = (byte)((s >> 4) & 15);
                if (s1 != 0)
                {
                    s1 |= Bounds[(2 * j) & 15];
                    if (s1 != 15)
                    {
                        CollectMap1[(j << 8) + s] = (byte)(((2 * j) << 4) | (byte)(~s1 & 0xF));
                    }
                }
                if (s2 != 0)
                {
                    s2 |= Bounds[(2 * j + 1) & 15];
                    if (s2 != 15)
                    {
                        CollectMap2[(j << 8) + s] = (byte)(((2 * j + 1) << 4) | (byte)(~s2 & 0xF));
                    }
                }
            }
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddLeftRight(long[] buffer, int len)
    {
        for (int i = 0; i < len; i++)
        {
            long val = buffer[i] & BaseIndexLeftRightMask;
            StatesMap[val >> (STATES_MAP_SKIP_POW + 4)] = 1;
            States[val >> 8] |= LeftRightMap[val & 255];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddUp(uint[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            long val = buffer[i];
            int offset = (int)((val & 15) << 2);
            StatesMap[val >> STATES_MAP_SKIP_POW] = 1;
            States[val >> 4] |= (ulong)PuzzleInfo.STATE_DN << offset;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddDown(uint[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            long val = buffer[i];
            int offset = (int)((val & 15) << 2);
            StatesMap[val >> STATES_MAP_SKIP_POW] = 1;
            States[val >> 4] |= (ulong)PuzzleInfo.STATE_UP << offset;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe long Collect(FrontierCollector collector)
    {
        Timer.Restart();
        long baseIndexWithOffset = BaseIndex << 4;
        long count = 0;
        fixed (ulong* statesPtr = States)
        {
            fixed (byte* statesMapPtr = StatesMap)
            {
                for (long q = 0; q < StatesMap.Length; q++)
                {
                    //if (q <= StatesMap.Length - 8 && (q & 7) == 0 && *(ulong*)(statesMapPtr + q) == 0)
                    //{
                    //    q += 7;
                    //    continue;
                    //}
                    if (statesMapPtr[q] == 0) continue;
                    statesMapPtr[q] = 0;
                    long start = q << (STATES_MAP_SKIP_POW - 4);
                    long end = Math.Min(States.Length, (q + 1) << (STATES_MAP_SKIP_POW - 4));
                    for (long i = start; i < end; i++)
                    {
                        ulong val = statesPtr[i];
                        if (val == 0) continue;
                        long baseIndex = baseIndexWithOffset | (i << 8);

                        while (val != 0)
                        {
                            int bit = BitOperations.TrailingZeroCount(val);
                            int j = (bit >> 3);
                            int byteIndex = (j << 3);
                            byte s = (byte)(val >> byteIndex);
                            count += CollectCounts[s];
                            int mapIndex = (j << 8) | s;
                            byte b1 = CollectMap1[mapIndex];
                            byte b2 = CollectMap2[mapIndex];
                            if (b1 != 0) collector.Add(baseIndex | b1);
                            if (b2 != 0) collector.Add(baseIndex | b2);
                            val &= ~(0xFFUL << byteIndex);
                        }
                        statesPtr[i] = 0;
                    }
                }
                collector.Close();
            }
        }

        TimeCollect += Timer.Elapsed;

        return count;
    }


    public static void PrintStats()
    {
        Console.WriteLine($"FrontierStates: collect={TimeCollect}");
    }
}
