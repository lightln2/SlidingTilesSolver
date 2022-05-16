using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class PackBytes
{
    private static TimeSpan TimePack = TimeSpan.Zero;
    private static TimeSpan TimeUnpack = TimeSpan.Zero;
    private static Stopwatch Timer = new Stopwatch();

    public unsafe static int Pack(byte[] arr, int count, byte[] buffer, int offset)
    {
        Timer.Restart();
        int pos = offset;
        for (int i = 0; i < count; i += 2)
        {
            buffer[pos++] = (byte)((arr[i + 1] << 4) | arr[i]);
        }
        TimePack += Timer.Elapsed;
        return pos - offset;
    }

    public unsafe static int Unpack(byte[] buffer, int offset, int length, byte[] arr)
    {
        Timer.Restart();
        int pos = offset;
        int count = 0;

        while (pos < offset + length)
        {
            byte b = buffer[pos++];
            arr[count++] = (byte)(b & 15);
            arr[count++] = (byte)(b >> 4);
        }
        if (pos != offset + length) throw new Exception($"pos={pos} len={length}");
        TimeUnpack += Timer.Elapsed;
        return count;
    }

    public static void PrintStats()
    {
        Console.WriteLine($"PackBytes: pack={TimePack} unpack={TimeUnpack}");
    }

}
