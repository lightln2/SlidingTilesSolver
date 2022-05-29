using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class PackStates
{
    private static TimeSpan TimePack = TimeSpan.Zero;
    private static TimeSpan TimeUnpack = TimeSpan.Zero;
    private static Stopwatch Timer = new Stopwatch();

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe void WriteInt(byte[] buffer, int pos, int val)
    {
        fixed(byte* ptr = buffer)
        {
            *(int*)(ptr + pos) = val;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe int ReadInt(byte[] buffer, int pos)
    {
        fixed (byte* ptr = buffer)
        {
            return *(int*)(ptr + pos);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int Pack(uint[] vals, byte[] states, int count, byte[] buffer)
    {
        if (count == 0) return 0;
        Timer.Restart();

        int alignedCount = (count + 15) & ~15;

        for (int i = count; i < alignedCount; i++)
        {
            vals[i] = 0;
            states[i] = states[count];
        }

        int statesLen = PackBytes.Pack(states, alignedCount, buffer, 12);
        int valsLen = PackInts.PackDiff(vals, alignedCount, buffer, 12 + statesLen);

        WriteInt(buffer, 0, count);
        WriteInt(buffer, 4, statesLen);
        WriteInt(buffer, 8, valsLen);

        TimePack += Timer.Elapsed;
        return 12 + statesLen + valsLen;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int Unpack(byte[] buffer, int length, uint[] vals, byte[] states)
    {
        Timer.Restart();
        if (length == 0) return 0;
        int count = ReadInt(buffer, 0);
        int alignedCount = (count + 15) & ~15;
        int statesLen = ReadInt(buffer, 4);
        int valsLen = ReadInt(buffer, 8);
        int statesCnt = PackBytes.Unpack(buffer, 12, statesLen, states);
        int valsCnt = PackInts.UnpackDiff(buffer, 12 + statesLen, valsLen, vals);
        if (statesCnt != alignedCount) throw new Exception($"States cnt={statesCnt} exp={alignedCount}");
        if (valsCnt != alignedCount) throw new Exception($"Vals cnt={valsCnt} exp={alignedCount}");
        TimeUnpack += Timer.Elapsed;
        return count;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"PackStates: pack={TimePack} unpack={TimeUnpack}");
    }

}
