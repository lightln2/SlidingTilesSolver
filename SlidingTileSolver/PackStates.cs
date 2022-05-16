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

    private static unsafe void WriteInt(byte[] buffer, int pos, int val)
    {
        fixed(byte* ptr = buffer)
        {
            *(int*)(ptr + pos) = val;
        }
    }

    private static unsafe int ReadInt(byte[] buffer, int pos)
    {
        fixed (byte* ptr = buffer)
        {
            return *(int*)(ptr + pos);
        }
    }

    public unsafe static int Pack(long[] arr, int count, byte[] buffer)
    {
        if (count == 0) return 0;
        Timer.Restart();

        int alignedCount = (count + 7) & ~7;

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

    public unsafe static int Unpack(byte[] buffer, int length, long[] arr)
    {
        Timer.Restart();
        if (length == 0) return 0;
        int count = ReadInt(buffer, 0);
        int alignedCount = (count + 7) & ~7;
        int statesLen = ReadInt(buffer, 4);
        int valsLen = ReadInt(buffer, 8);
        PackBytes.Unpack(buffer, 12, statesLen, arrStates);
        PackInts.Unpack(buffer, 12 + statesLen, valsLen, arrVals);

        long last = 0;
        for (int i = 0; i < count; i++)
        {
            last += arrVals[i];
            arr[i] = (last << 4) | arrStates[i];
        }
        TimeUnpack += Timer.Elapsed;
        return count;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"PackStates: pack={TimePack} unpack={TimeUnpack}");
    }

}
