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

    public unsafe static int Pack(long[] arr, int count, byte[] buffer)
    {
        if (count == 0) return 0;
        Timer.Restart();
        int pos = 0;
        /*
        if ((count & 1) == 1)
        {
            count++;
            arr[count - 1] = arr[count - 2];
        }

        byte* cntPtr = (byte*)&count;
        buffer[pos++] = cntPtr[0];
        buffer[pos++] = cntPtr[1];
        buffer[pos++] = cntPtr[2];
        buffer[pos++] = cntPtr[3];

        for (int i = 0; i < count; i += 2)
        {
            buffer[pos++] = (byte)(((arr[i] & 15) << 4) | (arr[i + 1] & 15));
            arr[i] >>= 4;
            arr[i + 1] >>= 4;
        }
        */

        WriteVInt(arr[0], buffer, ref pos);
        for (int i = 1; i < count; i++)
        {
            WriteVInt(arr[i] - arr[i - 1], buffer, ref pos);
        }
        TimePack += Timer.Elapsed;
        return pos;
    }

    public unsafe static int Unpack(byte[] buffer, int length, long[] arr)
    {
        Timer.Restart();
        if (length == 0) return 0;
        int pos = 0;
        int count = 0;
        /*
        int storedCount = 0;
        byte* cntPtr = (byte*)&storedCount;
        cntPtr[0] = buffer[pos++];
        cntPtr[1] = buffer[pos++];
        cntPtr[2] = buffer[pos++];
        cntPtr[3] = buffer[pos++];

        pos = 4 + storedCount / 2;
        */

        arr[count++] = NextVInt(buffer, ref pos);
        while (pos < length)
        {
            arr[count] = arr[count - 1] + NextVInt(buffer, ref pos);
            count++;
        }
        if (pos != length) throw new Exception($"pos={pos} len={length}");
        /*
        for (int i = 0; i < storedCount; i += 2)
        {
            arr[i] = (arr[i] << 4) | (long)(buffer[4 + (i >> 1)] >> 4);
            arr[i + 1] = (arr[i + 1] << 4) | (long)(buffer[4 + (i >> 1)] & 15);
        }
        */
        TimeUnpack += Timer.Elapsed;
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void WriteVInt(long x, byte[] buffer, ref int pos)
    {
        while (x > 127)
        {
            buffer[pos++] = (byte)(x | 128);
            x >>= 7;
        }
        buffer[pos++] = (byte)x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static long NextVInt(byte[] buffer, ref int pos)
    {
        long val = buffer[pos++];
        if (val < 128) return val;
        val &= ~128;
        int off = 7;
        while (true)
        {
            long next = buffer[pos++];
            if (next < 128) return val | (next << off);
            next &= ~128;
            val = val | (next << off);
            off += 7;
        }
    }

    public static void PrintStats()
    {
        Console.WriteLine($"PackStates: pack={TimePack} unpack={TimeUnpack}");
    }

}
