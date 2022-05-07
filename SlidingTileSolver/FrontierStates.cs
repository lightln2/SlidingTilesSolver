using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class FrontierStates
{

    private byte[] Bounds;
    private byte[] States;

    public FrontierStates(PuzzleInfo info)
    {
        States = new byte[info.Total];

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
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddLeftRight(List<long> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            long val = list[i];
            long index = val >> 4;
            if ((val & PuzzleInfo.STATE_LT) != 0) States[index - 1] |= PuzzleInfo.STATE_RT;
            if ((val & PuzzleInfo.STATE_RT) != 0) States[index + 1] |= PuzzleInfo.STATE_LT;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddUp(long[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            States[buffer[i]] |= PuzzleInfo.STATE_DN;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void AddDown(long[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            States[buffer[i]] |= PuzzleInfo.STATE_UP;
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
                ulong val = statesPtrUlong[i];
                if (val == 0) continue;
                byte* valPtr = (byte*)&val;
                long i8 = i * 8;
                for (int j = 0; j < 8; j++)
                {
                    byte s = valPtr[j];
                    if (s == 0) continue;
                    count++;
                    s |= Bounds[(i8 + j) & 15];
                    if (s == 15) continue;
                    list.Add(((i8 + j) << 4) | (byte)(~s & 0xF));
                }
                statesPtrUlong[i] = 0;
            }
        }
        return count;
    }
}
