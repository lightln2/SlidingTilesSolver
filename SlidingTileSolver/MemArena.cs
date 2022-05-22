using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

public unsafe class MemArena
{
    private byte* Arena;
    private readonly long Length;
    private long Position;
    public MemArena(long bytes)
    {
        Length = bytes;
        Arena = (byte*)Marshal.AllocHGlobal(new IntPtr(bytes)).ToPointer();
        Position = 0;
    }

    public byte* AllocBytes(long bytes)
    {
        if (Position + bytes > Length) throw new Exception("Out of memory in arena");
        byte* ptr = Arena + Position;
        Position += bytes;
        return ptr;
    }

    public void Reset()
    {
        Position = 0;
    }

    public ulong* AllocUlong(long ulongs)
    {
        return (ulong*)AllocBytes(ulongs * 8);
    }

    public uint* AllocUint(long uints)
    {
        return (uint*)AllocBytes(uints * 4);
    }

}
