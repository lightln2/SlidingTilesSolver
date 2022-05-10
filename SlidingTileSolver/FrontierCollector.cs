using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class FrontierCollector
{
    public readonly Frontier Frontier;
    public readonly int Segment;
    private readonly long[] Buffer;
    private int BufferPosition;

    public FrontierCollector(Frontier frontier, int segment, long[] buffer)
    {
        Frontier = frontier;
        Segment = segment;
        Buffer = buffer;
        BufferPosition = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Add(long value)
    {
        Buffer[BufferPosition++] = value;
        if (BufferPosition == Buffer.Length)
        {
            Flush();
        }
    }

    private void Flush()
    {
        Frontier.Write(Segment, Buffer, BufferPosition);
        BufferPosition = 0;
    }

    public void Close() => Flush();
}
