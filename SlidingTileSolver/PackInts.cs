using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class PackInts
{
    private static TimeSpan TimePack = TimeSpan.Zero;
    private static TimeSpan TimeUnpack = TimeSpan.Zero;
    private static Stopwatch Timer = new Stopwatch();

    public static int BytesCntMinusOne(uint val)
    {
        if (val == 0) return 0;
        return (31 - BitOperations.LeadingZeroCount(val)) >> 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int Pack(uint[] arr, int count, byte[] buffer, int offset)
    {
        if ((count & 15) != 0) throw new Exception("Count should be divisible by 16");
        Timer.Restart();
        int pos = offset;
        fixed (uint* src = arr)
        {
            fixed (byte* dst = buffer)
            {
                for (int i = 0; i < count; i += 4)
                {
                    uint x = src[i], y = src[i + 1], z = src[i + 2], t = src[i + 3];
                    int sx = BytesCntMinusOne(x);
                    int sy = BytesCntMinusOne(y);
                    int sz = BytesCntMinusOne(z);
                    int st = BytesCntMinusOne(t);

                    byte state = (byte)(sx | (sy << 2) | (sz << 4) | (st << 6));
                    dst[pos++] = state;

                    *(uint*)(dst + pos) = x;
                    pos += sx + 1;
                    *(uint*)(dst + pos) = y;
                    pos += sy + 1;
                    *(uint*)(dst + pos) = z;
                    pos += sz + 1;
                    *(uint*)(dst + pos) = t;
                    pos += st + 1;

                }
            }
        }
        TimePack += Timer.Elapsed;
        return pos - offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int Unpack(byte[] buffer, int offset, int length, uint[] arr)
    {
        Timer.Restart();
        if (length == 0) return 0;
        int pos = offset;
        int count = 0;

        fixed (byte* src = buffer)
        {
            fixed (uint* dst = arr)
            {
                while (pos < offset + length)
                {
                    byte state = src[pos++];
                    int sx = 1 + (state & 3);
                    int sy = 1 + ((state >> 2) & 3);
                    int sz = 1 + ((state >> 4) & 3);
                    int st = 1 + ((state >> 6) & 3);
                    uint maskx = (uint)((1ul << (sx * 8)) - 1);
                    uint masky = (uint)((1ul << (sy * 8)) - 1);
                    uint maskz = (uint)((1ul << (sz * 8)) - 1);
                    uint maskt = (uint)((1ul << (st * 8)) - 1);
                    uint x = *(uint*)(src + pos) & maskx;
                    pos += sx;
                    uint y = *(uint*)(src + pos) & masky;
                    pos += sy;
                    uint z = *(uint*)(src + pos) & maskz;
                    pos += sz;
                    uint t = *(uint*)(src + pos) & maskt;
                    pos += st;
                    dst[count++] = x;
                    dst[count++] = y;
                    dst[count++] = z;
                    dst[count++] = t;
                }
            }
        }


        if (pos != offset + length) throw new Exception($"pos={pos} len={length}");

        TimeUnpack += Timer.Elapsed;
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe void WriteVInt(long x, byte* buffer, ref int pos)
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
