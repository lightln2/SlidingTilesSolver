using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class MultislideFrontierCollector
{
    public readonly MultislideFrontier Frontier;
    public int Segment { get; set; } = -1;
    private readonly byte[] TempBuffer;
    private readonly uint[] Vals;
    private int BufferPosition;

    public MultislideFrontierCollector(MultislideFrontier frontier, byte[] tempBuffer, uint[] vals)
    {
        Frontier = frontier;
        TempBuffer = tempBuffer;
        Vals = vals;
        BufferPosition = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Add(uint val)
    {
        Vals[BufferPosition++] = val;
        if (BufferPosition == Vals.Length)
        {
            Flush();
        }
    }

    private void Flush()
    {
        Frontier.Write(Segment, TempBuffer, Vals, BufferPosition);
        BufferPosition = 0;
    }

    public void Close() => Flush();
}
