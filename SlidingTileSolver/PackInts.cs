using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

public unsafe class PackInts
{
    private static TimeSpan TimePack = TimeSpan.Zero;
    private static TimeSpan TimeUnpack = TimeSpan.Zero;
    private static Stopwatch Timer = new Stopwatch();


    private static int[] Counts;
    private static Vector128<byte>[] UnpackMask;

    static PackInts()
    {
        Counts = new int[256];
        UnpackMask = new Vector128<byte>[256];

        byte[] data = new byte[16];
        fixed (byte* dataPtr = data)
        {
            for (int state = 0; state < 256; state++)
            {
                int sx = 1 + (state & 3);
                int sy = 1 + ((state >> 2) & 3);
                int sz = 1 + ((state >> 4) & 3);
                int st = 1 + ((state >> 6) & 3);

                byte srcPos = 0;
                int dstPos = 0;
                for (int i = 0; i < sx; i++) data[dstPos++] = srcPos++;
                for (int i = sx; i < 4; i++) data[dstPos++] = 255;
                for (int i = 0; i < sy; i++) data[dstPos++] = srcPos++;
                for (int i = sy; i < 4; i++) data[dstPos++] = 255;
                for (int i = 0; i < sz; i++) data[dstPos++] = srcPos++;
                for (int i = sz; i < 4; i++) data[dstPos++] = 255;
                for (int i = 0; i < st; i++) data[dstPos++] = srcPos++;
                for (int i = st; i < 4; i++) data[dstPos++] = 255;
                UnpackMask[state] = Avx2.LoadVector128(dataPtr);

                Counts[state] = sx + sy + sz + st;
                if (srcPos != Counts[state]) throw new Exception("wrong srcPos");
            }
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int BytesCntMinusOne(uint val)
    {
        if (val == 0) return 0;
        return (31 - BitOperations.LeadingZeroCount(val)) >> 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int PackDiff(uint[] arr, int count, byte[] buffer, int offset)
    {
        fixed (uint* src = arr)
        {
            return PackDiff(src, count, buffer, offset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int PackDiff(uint* src, int count, byte[] buffer, int offset)
    {
        if ((count & 15) != 0) throw new Exception("Count should be divisible by 16");
        Timer.Restart();
        int pos = offset;
        uint last = 0;
        fixed (byte* dst = buffer)
        {
            for (int i = 0; i < count; i += 4)
            {
                uint x = src[i], y = src[i + 1], z = src[i + 2], t = src[i + 3];
                uint newLast = t;
                t -= z;
                z -= y;
                y -= x;
                x -= last;
                last = newLast;
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
        TimePack += Timer.Elapsed;
        return pos - offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static int UnpackDiff(byte[] buffer, int offset, int length, uint[] arr)
    {
        Timer.Restart();
        if (length == 0) return 0;
        int pos = offset;
        int count = 0;
        uint last = 0;
        fixed (byte* src = buffer)
        {
            fixed (uint* dst = arr)
            {
                while (pos < offset + length)
                {
                    byte state = src[pos++];
                    Vector128<byte> v = Avx2.LoadVector128(src + pos);
                    pos += Counts[state];
                    Vector128<byte> unpacked = Avx2.Shuffle(v, UnpackMask[state]);
                    Avx2.Store((byte*)(dst + count), unpacked);
                    last = (dst[count++] += last);
                    last = (dst[count++] += last);
                    last = (dst[count++] += last);
                    last = (dst[count++] += last);
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
