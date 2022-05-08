using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class FrontierStates
{
    const int STATES_MAP_POW = 12;
    private byte[] StatesMap;
    private ulong[] States;

    private ulong[] LeftRightMap;

    private byte[] Bounds;
    private byte[] CollectCounts;
    private byte[] CollectMap1;
    private byte[] CollectMap2;

    public FrontierStates(PuzzleInfo info)
    {
        States = new ulong[info.Total / 16];
        StatesMap = new byte[(States.Length >> (STATES_MAP_POW - 4)) + 1];

        Bounds = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            byte s = 0;
            if (!info.CanGoUp(i)) s |= PuzzleInfo.STATE_UP;
            if (!info.CanGoDown(i)) s |= PuzzleInfo.STATE_DN;
            if (!info.CanGoLeft(i)) s |= PuzzleInfo.STATE_LT;
            if (!info.CanGoRight(i)) s |= PuzzleInfo.STATE_RT;
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
    public void AddLeftRight(List<long> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            long val = list[i];
            StatesMap[val >> (STATES_MAP_POW + 4)] = 1;
            States[val >> 8] |= LeftRightMap[val & 255];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddUp(long[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            long val = buffer[i];
            int offset = (int)((val & 15) << 2);
            StatesMap[val >> STATES_MAP_POW] = 1;
            States[val >> 4] |= (ulong)PuzzleInfo.STATE_DN << offset;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddDown(long[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            long val = buffer[i];
            int offset = (int)((val & 15) << 2);
            StatesMap[val >> STATES_MAP_POW] = 1;
            States[val >> 4] |= (ulong)PuzzleInfo.STATE_UP << offset;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe long Collect(List<long> list)
    {
        long count = 0;
        fixed (ulong* statesPtr = States)
        {
            for (long q = 0; q < StatesMap.Length; q++)
            {
                if (StatesMap[q] == 0) continue;
                StatesMap[q] = 0;
                long start = q << (STATES_MAP_POW - 4);
                long end = Math.Min(States.Length, (q + 1) << (STATES_MAP_POW - 4));
                for (long i = start; i < end; i++)
                {
                    ulong val = statesPtr[i];
                    if (val == 0) continue;
                    long baseIndex = (i << 8);

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
                        if (b1 != 0) list.Add(baseIndex | b1);
                        if (b2 != 0) list.Add(baseIndex | b2);

                        val &= ~(0xFFUL << byteIndex);
                    }
                    statesPtr[i] = 0;
                }
            }
        }

        return count;
    }
}
