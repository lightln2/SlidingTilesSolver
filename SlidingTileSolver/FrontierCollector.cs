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
    private readonly uint[] Vals;
    private readonly byte[] States;
    private int BufferPosition;

    public FrontierCollector(Frontier frontier, int segment, uint[] vals, byte[] states)
    {
        Frontier = frontier;
        Segment = segment;
        Vals = vals;
        States = states;
        BufferPosition = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Add(long value)
    {
        Vals[BufferPosition] = (uint)(value >> 4);
        States[BufferPosition] = (byte)(value & 15);
        BufferPosition++;
        if (BufferPosition == Vals.Length)
        {
            Flush();
        }
    }

    private void Flush()
    {
        Frontier.Write(Segment, Vals, States, BufferPosition);
        BufferPosition = 0;
    }

    public void Close() => Flush();
}
