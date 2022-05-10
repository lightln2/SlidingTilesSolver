using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PuzzleInfo
{
    public const int SEGMENT_SIZE_POW = 32;
    public const long SEGMENT_MASK = (1L << SEGMENT_SIZE_POW) - 1;
    public const int FRONTIER_BUFFER_SIZE = 2 * 1024 * 1024;
    public const int SEMIFRONTIER_BUFFER_SIZE = 1024 * 1024 / 8;

    public readonly int Width, Height, Size;
    public readonly long InitialIndex;
    // real number of states
    public readonly long RealStates;
    // problem size: each line of size 'Size' occupies 16 positions
    public readonly long Total;

    public readonly long StatesMapLength;

    public readonly int SegmentsCount;

    public PuzzleInfo(int width, int height, int initialIndex)
    {
        Width = width;
        Height = height;
        Size = width * height;
        InitialIndex = initialIndex;
        if (InitialIndex >= 16) throw new Exception("Initial index should be from 0 to 16");
        RealStates = Util.Factorial(Size) / 2;
        Total = Util.Factorial(Size - 1) * 16 / 2;
        SegmentsCount = (int)((Total >> SEGMENT_SIZE_POW) + 1);
        StatesMapLength = SegmentsCount == 1 ? Total : 1L << SEGMENT_SIZE_POW;
    }

    public override string ToString()
    {
        return $"Puzzle {Size}={Width}x{Height}, states: {RealStates:N0}, total: {Total:N0}. Segments: {SegmentsCount}";
    }

    public const byte STATE_UP = 1;
    public const byte STATE_DN = 2;
    public const byte STATE_LT = 4;
    public const byte STATE_RT = 8;

    public bool CanGoUp(long index) => (index & 15) >= Width;
    public bool CanGoDown(long index) => (index & 15) < Size - Width;
    public bool CanGoLeft(long index) => (index & 15) % Width != 0;
    public bool CanGoRight(long index) => (index & 15) % Width != Width - 1;

    public byte GetState(long index)
    {
        byte state = 0;
        if (CanGoUp(index)) state |= STATE_UP;
        if (CanGoDown(index)) state |= STATE_DN;
        if (CanGoLeft(index)) state |= STATE_LT;
        if (CanGoRight(index)) state |= STATE_RT;
        return state;
    }

    public string PrintState(long st)
    {
        StringBuilder sb = new StringBuilder();
        if ((st & STATE_UP) != 0) sb.Append($"U");
        if ((st & STATE_DN) != 0) sb.Append($"D");
        if ((st & STATE_LT) != 0) sb.Append($"L");
        if ((st & STATE_RT) != 0) sb.Append($"R");
        return sb.ToString();
    }
}
