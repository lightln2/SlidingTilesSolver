using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class MultislideFrontierCollector
{
    public readonly Frontier Frontier;
    public int Segment { get; set; } = -1;
    private readonly byte[] TempBuffer;
    private readonly uint[] Vals;
    private readonly byte[] States;
    private int BufferPosition;

    public MultislideFrontierCollector(Frontier frontier, byte[] tempBuffer, uint[] vals, byte[] states)
    {
        Frontier = frontier;
        TempBuffer = tempBuffer;
        Vals = vals;
        States = states;
        BufferPosition = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Add(uint val, byte state)
    {
        Vals[BufferPosition] = val;
        States[BufferPosition] = state;
        //BufferPosition++;
        BufferPosition += (state == 0) ? 0 : 1;
        if (BufferPosition == Vals.Length)
        {
            Flush();
        }
    }

    private void Flush()
    {
        Frontier.Write(Segment, TempBuffer, Vals, States, BufferPosition);
        BufferPosition = 0;
    }

    public void Close() => Flush();
}
