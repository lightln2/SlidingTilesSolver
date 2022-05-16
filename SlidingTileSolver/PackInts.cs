using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class PackInts
{
    private static TimeSpan TimePack = TimeSpan.Zero;
    private static TimeSpan TimeUnpack = TimeSpan.Zero;
    private static Stopwatch Timer = new Stopwatch();

    public unsafe static int Pack(uint[] arr, int count, byte[] buffer, int offset)
    {
        Timer.Restart();
        int pos = offset;
        WriteVInt(arr[0], buffer, ref pos);
        for (int i = 1; i < count; i++)
        {
            WriteVInt(arr[i], buffer, ref pos);
        }
        TimePack += Timer.Elapsed;
        return pos - offset;
    }

    public unsafe static int Unpack(byte[] buffer, int offset, int length, uint[] arr)
    {
        Timer.Restart();
        if (length == 0) return 0;
        int pos = offset;
        int count = 0;

        arr[count++] = NextVInt(buffer, ref pos);
        while (pos < offset + length)
        {
            arr[count] = NextVInt(buffer, ref pos);
            count++;
        }
        if (pos != offset + length) throw new Exception($"pos={pos} len={length}");

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
    private static uint NextVInt(byte[] buffer, ref int pos)
    {
        uint val = buffer[pos++];
        if (val < 128) return val;
        val &= ~128u;
        int off = 7;
        while (true)
        {
            uint next = buffer[pos++];
            if (next < 128) return val | (next << off);
            next &= ~128u;
            val = val | (next << off);
            off += 7;
        }
    }

    public static void PrintStats()
    {
        Console.WriteLine($"PackInts: pack={TimePack} unpack={TimeUnpack}");
    }

}
