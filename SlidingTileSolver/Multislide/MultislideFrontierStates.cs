using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public unsafe class MultislideFrontierStates
{
    const int STATES_MAP_SKIP_POW = 12;

    private static TimeSpan TimeCollect = TimeSpan.Zero;

    private readonly long StatesMapLength;

    private byte[] StatesMap;
    private ulong* States;

    private ulong[] MultislideLeftRightMap;
    private ulong[] ExcludeMap;

    private Stopwatch Timer = new Stopwatch();
    
    public MultislideFrontierStates(PuzzleInfo info)
    {
        StatesMapLength = (info.StatesMapLength + 31) / 32;
        States = info.Arena.AllocUlong(StatesMapLength);
        StatesMap = new byte[(StatesMapLength >> (STATES_MAP_SKIP_POW - 5)) + 1];

        MultislideLeftRightMap = new ulong[32];

        for (int b = 0; b < 32; b++)
        {
            if ((b & 15) >= info.Size)
            {
                MultislideLeftRightMap[b] = 0;
                continue;
            }

            ulong x = 0;

            int pos = b;
            while (info.CanGoLeft(pos))
            {
                pos--;
                x |= (ulong)PuzzleInfo.MULTISLIDE_LT_RT << (pos * 2);
            }

            pos = b;
            while (info.CanGoRight(pos))
            {
                pos++;
                x |= (ulong)PuzzleInfo.MULTISLIDE_LT_RT << (pos * 2);
            }

            MultislideLeftRightMap[b] = x;
        }

        ExcludeMap = new ulong[32];

        for (int b = 0; b < 32; b++)
        {
            ExcludeMap[b] = ~(3UL << (b * 2));
        }
    }

    public void Reset()
    {
        for (int i = 0; i < StatesMapLength; i++) States[i] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddLeftRight(uint[] vals, int len)
    {
        for (int i = 0; i < len; i++)
        {
            ulong x = MultislideLeftRightMap[vals[i] & 31];
            StatesMap[vals[i] >> STATES_MAP_SKIP_POW] |= (byte)BitOperations.PopCount(x);
            States[vals[i] >> 5] |= x;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Exclude(uint[] vals, int len)
    {
        for (int i = 0; i < len; i++)
        {
            States[vals[i] >> 5] &= ExcludeMap[vals[i] & 31];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddUpDown(uint[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            long val = buffer[i];
            int offset = (int)((val & 31) << 1);
            StatesMap[val >> STATES_MAP_SKIP_POW] = 1;
            States[val >> 5] |= (ulong)PuzzleInfo.MULTISLIDE_UP_DN << offset;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe long Collect(MultislideFrontierCollector collectorUpDn, MultislideFrontierCollector collectorLtRt)
    {
        Timer.Restart();
        long count = 0;
        for (long q = 0; q < StatesMap.Length; q++)
        {
            if (StatesMap[q] == 0) continue;
            StatesMap[q] = 0;
            long start = q << (STATES_MAP_SKIP_POW - 5);
            long end = Math.Min(StatesMapLength, (q + 1) << (STATES_MAP_SKIP_POW - 5));
            for (long i = start; i < end; i++)
            {
                ulong val = States[i];
                if (val == 0) continue;
                uint baseIndex = (uint)(i << 5);

                while (val != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(val);
                    int j = (bit >> 1);
                    int off = j << 1;
                    byte state = (byte)(((val >> off) & 3));
                    count++;

                    switch(state)
                    {
                        case PuzzleInfo.MULTISLIDE_UP_DN:
                            collectorLtRt.Add(baseIndex | (uint)j);
                            break;
                        case PuzzleInfo.MULTISLIDE_LT_RT:
                            collectorUpDn.Add(baseIndex | (uint)j);
                            break;
                        default:
                            break;
                    }
                    val &= ~(3UL << off);
                }
                States[i] = 0;
            }
        }
        collectorUpDn.Close();
        collectorLtRt.Close();
        TimeCollect += Timer.Elapsed;
        return count;
    }


    public static void PrintStats()
    {
        Console.WriteLine($"FrontierStates: collect={TimeCollect}");
    }
}
