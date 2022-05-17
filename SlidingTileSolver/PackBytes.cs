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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int Pack(byte[] arr, int count, byte[] buffer, int offset)
    {
        if ((count & 15) != 0) throw new Exception("Count should be divisible by 16");
        Timer.Restart();
        int pos = offset;
        fixed (byte* src = arr, dst = buffer)
        {
            for (int i = 0; i < count; i += 16)
            {
                ulong v1 = *(ulong*)(src + i);
                ulong v2 = *(ulong*)(src + i + 8);
                *(ulong*)(dst + pos) = v1 | (v2 << 4);
                pos += 8;
            }
        }
        TimePack += Timer.Elapsed;
        return pos - offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int Unpack(byte[] buffer, int offset, int length, byte[] arr)
    {
        Timer.Restart();
        int pos = offset;
        int count = 0;

        fixed (byte* src = buffer, dst = arr)
        {
            while (pos < offset + length)
            {
                ulong v = *(ulong*)(src + pos);
                ulong v1 = v & 0x0f0f0f0f0f0f0f0fUL;
                ulong v2 = (v >> 4) & 0x0f0f0f0f0f0f0f0fUL;
                *(ulong*)(dst + count) = v1;
                *(ulong*)(dst + count + 8) = v2;
                pos += 8;
                count += 16;
            }
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
