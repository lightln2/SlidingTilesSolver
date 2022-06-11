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

    private byte[] Bounds;

    private Stopwatch Timer = new Stopwatch();
    
    public MultislideFrontierStates(PuzzleInfo info)
    {
        StatesMapLength = info.StatesMapLength / 16;
        States = info.Arena.AllocUlong(StatesMapLength);
        StatesMap = new byte[(StatesMapLength >> (STATES_MAP_SKIP_POW - 4)) + 1];

        Bounds = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            byte s = (byte)~info.GetState(i);
            Bounds[i] = s;
        }

        MultislideLeftRightMap = new ulong[256];
        for (int b = 0; b < 256; b++)
        {
            if ((b >> 4) >= info.Size)
            {
                MultislideLeftRightMap[b] = 0;
                continue;
            }
            ulong x = 0;
            {
                int index = b >> 4;
                int state = (b & 15) & (byte)~Bounds[index];
                while ((state & PuzzleInfo.STATE_LT) != 0)
                {
                    x |= (ulong)(PuzzleInfo.STATE_LT_RT) << ((index - 1) * 4);
                    index--;
                    state = (b & 15) & (byte)~Bounds[index];
                }
            }
            {
                int index = b >> 4;
                int state = (b & 15) & (byte)~Bounds[index];
                while ((state & PuzzleInfo.STATE_RT) != 0)
                {
                    x |= (ulong)(PuzzleInfo.STATE_LT_RT) << ((index + 1) * 4);
                    index++;
                    state = (b & 15) & (byte)~Bounds[index];
                }
            }
            MultislideLeftRightMap[b] = x;
        }
    }

    public void Reset()
    {
        for (int i = 0; i < StatesMapLength; i++) States[i] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddLeftRight(uint[] vals, byte[] states, int len)
    {
        for (int i = 0; i < len; i++)
        {
            byte mapIndex = (byte)((vals[i] << 4) | states[i]);
            ulong x = MultislideLeftRightMap[mapIndex];
            StatesMap[vals[i] >> STATES_MAP_SKIP_POW] |= (byte)BitOperations.PopCount(x);
            States[vals[i] >> 4] |= x;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Exclude(uint[] vals, int len)
    {
        for (int i = 0; i < len; i++)
        {
            int index = (int)(vals[i] & 15);
            States[vals[i] >> 4] &= ~((ulong)15 << (index * 4));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddUpDown(uint[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            long val = buffer[i];
            int offset = (int)((val & 15) << 2);
            StatesMap[val >> STATES_MAP_SKIP_POW] = 1;
            States[val >> 4] |= (ulong)PuzzleInfo.STATE_UP_DN << offset;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe long Collect(MultislideFrontierCollector collector)
    {
        Timer.Restart();
        long count = 0;
        for (long q = 0; q < StatesMap.Length; q++)
        {
            if (StatesMap[q] == 0) continue;
            StatesMap[q] = 0;
            long start = q << (STATES_MAP_SKIP_POW - 4);
            long end = Math.Min(StatesMapLength, (q + 1) << (STATES_MAP_SKIP_POW - 4));
            for (long i = start; i < end; i++)
            {
                ulong val = States[i];
                if (val == 0) continue;
                uint baseIndex = (uint)(i << 4);

                while (val != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(val);
                    int j = (bit >> 2);
                    int off = j << 2;
                    byte state = (byte)(((val >> off) & 0xF) | Bounds[j]);
                    count++;

                    collector.Add(baseIndex | (uint)j, (byte)(~state & 15));

                    val &= ~(0xFUL << off);
                }
                States[i] = 0;
            }
        }
        collector.Close();
        TimeCollect += Timer.Elapsed;
        return count;
    }


    public static void PrintStats()
    {
        Console.WriteLine($"FrontierStates: collect={TimeCollect}");
    }
}
