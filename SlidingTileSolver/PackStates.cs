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
    private static uint[] arrVals = new uint[400000000];
    private static byte[] arrStates = new byte[400000000];

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
    public unsafe static int Pack(long[] arr, int count, byte[] buffer)
    {
        if (count == 0) return 0;
        Timer.Restart();

        int alignedCount = (count + 15) & ~15;

        WriteInt(buffer, 0, count);

        uint last = 0;
        for (int i = 0; i < count; i++)
        {
            arrStates[i] = (byte)(arr[i] & 15);
            uint next = (uint)(arr[i] >> 4);
            arrVals[i] = next - last;
            last = next;
        }

        for (int i = count; i < alignedCount; i++)
        {
            arrStates[i] = arrStates[count - 1];
            arrVals[i] = 0;
        }

        int statesLen = PackBytes.Pack(arrStates, alignedCount, buffer, 12);
        int valsLen = PackInts.Pack(arrVals, alignedCount, buffer, 12 + statesLen);

        WriteInt(buffer, 4, statesLen);
        WriteInt(buffer, 8, valsLen);

        TimePack += Timer.Elapsed;
        return 12 + statesLen + valsLen;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int Unpack(byte[] buffer, int length, long[] arr)
    {
        Timer.Restart();
        if (length == 0) return 0;
        int count = ReadInt(buffer, 0);
        int alignedCount = (count + 15) & ~15;
        int statesLen = ReadInt(buffer, 4);
        int valsLen = ReadInt(buffer, 8);
        int statesCnt = PackBytes.Unpack(buffer, 12, statesLen, arrStates);
        int valsCnt = PackInts.Unpack(buffer, 12 + statesLen, valsLen, arrVals);
        if (statesCnt != alignedCount) throw new Exception($"States cnt={statesCnt} exp={alignedCount}");
        if (valsCnt != alignedCount) throw new Exception($"Vals cnt={valsCnt} exp={alignedCount}");

        long last = 0;
        for (int i = 0; i < count; i++)
        {
            last += arrVals[i];
            arr[i] = (last << 4) | arrStates[i];
        }
        TimeUnpack += Timer.Elapsed;
        return count;
    }


    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int Pack(uint[] vals, byte[] states, int count, byte[] buffer)
    {
        if (count == 0) return 0;
        Timer.Restart();

        int alignedCount = (count + 15) & ~15;

        for (int i = count - 1; i >= 1; i--)
        {
            vals[i] -= vals[i - 1];
        }

        for (int i = count; i < alignedCount; i++)
        {
            vals[i] = 0;
            states[i] = 0;
        }

        int statesLen = PackBytes.Pack(states, alignedCount, buffer, 12);
        int valsLen = PackInts.Pack(vals, alignedCount, buffer, 12 + statesLen);

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
        int valsCnt = PackInts.Unpack(buffer, 12 + statesLen, valsLen, vals);
        if (statesCnt != alignedCount) throw new Exception($"States cnt={statesCnt} exp={alignedCount}");
        if (valsCnt != alignedCount) throw new Exception($"Vals cnt={valsCnt} exp={alignedCount}");

        for (int i = 1; i < count; i++)
        {
            vals[i] += vals[i - 1];
        }
        TimeUnpack += Timer.Elapsed;
        return count;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"PackStates: pack={TimePack} unpack={TimeUnpack}");
    }

}
