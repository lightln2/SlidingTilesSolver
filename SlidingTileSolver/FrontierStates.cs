using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class FrontierStates
{
    private byte[] States;

    private byte[] Bounds;
    private byte[] CollectCounts;
    private byte[] CollectMap1;
    private byte[] CollectMap2;

    public FrontierStates(PuzzleInfo info)
    {
        States = new byte[info.Total / 2];

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
            long index = val >> 4;
            if (index % 2 == 0)
            {
                if ((val & PuzzleInfo.STATE_LT) != 0) States[index / 2 - 1] |= (PuzzleInfo.STATE_RT << 4);
                if ((val & PuzzleInfo.STATE_RT) != 0) States[index / 2] |= (PuzzleInfo.STATE_LT << 4);
            }
            else
            {
                if ((val & PuzzleInfo.STATE_LT) != 0) States[index / 2] |= PuzzleInfo.STATE_RT;
                if ((val & PuzzleInfo.STATE_RT) != 0) States[index / 2 + 1] |= PuzzleInfo.STATE_LT;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddUp(long[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (buffer[i] % 2 == 0)
            {
                States[buffer[i] / 2] |= PuzzleInfo.STATE_DN;
            }
            else
            {
                States[buffer[i] / 2] |= (PuzzleInfo.STATE_DN << 4);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddDown(long[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (buffer[i] % 2 == 0)
            {
                States[buffer[i] / 2] |= PuzzleInfo.STATE_UP;
            }
            else
            {
                States[buffer[i] / 2] |= (PuzzleInfo.STATE_UP << 4);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe long Collect(List<long> list)
    {
        long count = 0;
        fixed (byte* statesPtr = States)
        {
            ulong* statesPtrUlong = (ulong*)statesPtr;
            for (long i = 0; i < States.Length / 8; i++)
            {
                long baseIndex = (i << 8);
                ulong val = statesPtrUlong[i];
                if (val == 0) continue;
                byte* valPtr = (byte*)&val;
                for (int j = 0; j < 8; j++)
                {
                    byte s = valPtr[j];
                    if (s == 0) continue;
                    count += CollectCounts[s];
                    int mapIndex = (j << 8) | s;
                    byte b1 = CollectMap1[mapIndex];
                    byte b2 = CollectMap2[mapIndex];
                    if (b1 != 0) list.Add(baseIndex | b1);
                    if (b2 != 0) list.Add(baseIndex | b2);
                }
                statesPtrUlong[i] = 0;
            }
        }

        return count;
    }
}
