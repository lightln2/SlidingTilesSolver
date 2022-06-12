using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PuzzleInfo
{
    public const int SEGMENT_SIZE_POW = 32;

    public static bool SEMIFRONTIER_DIFF_ENCODING = true;
    public static int THREADS = 4;
    public const int FRONTIER_BUFFER_SIZE = 4 * 1024 * 1024;

    // 18 is 1MB = 256K uint's
    public static int SEMIFRONTIER_BUFFER_POW { get; private set; } = 18;
    public static int SEMIFRONTIER_BUFFER_SIZE { get; private set; } = (1 << SEMIFRONTIER_BUFFER_POW);

    public static void SetSemifrontierBufferPow(int pow)
    {
        SEMIFRONTIER_BUFFER_POW = pow;
        SEMIFRONTIER_BUFFER_SIZE = (1 << SEMIFRONTIER_BUFFER_POW);
    }

    public int MaxSteps = 10000;

    public readonly bool Multislide;
    public readonly int Width, Height, Size;
    public readonly long InitialIndex;
    // real number of states
    public readonly long RealStates;
    // problem size: each line of size 'Size' occupies 16 positions
    public readonly long Total;

    public readonly long StatesMapLength;

    public readonly int SegmentsCount;

    public long BytesNeeded;
    public MemArena Arena;

    public PuzzleInfo(int width, int height, int initialIndex, bool multislide = false)
    {
        Multislide = multislide;
        Width = width;
        Height = height;
        Size = width * height;
        InitialIndex = initialIndex;
        if (InitialIndex >= Size) throw new Exception($"Initial index should be from 0 to {Size}");
        RealStates = Util.Factorial(Size) / 2;
        Total = Util.Factorial(Size - 1) * 16 / 2;
        SegmentsCount = (int)((Total >> SEGMENT_SIZE_POW) + 1);
        StatesMapLength = SegmentsCount == 1 ? Total : 1L << SEGMENT_SIZE_POW;

        long statesMem = THREADS * StatesMapLength / 2;
        if (Multislide) statesMem /= 2;
        long sfMem = SegmentsCount * 2L * SEMIFRONTIER_BUFFER_SIZE * 4;
        long updownMem = THREADS * 2L * GpuSolver.GPUSIZE * 8;
        BytesNeeded = Math.Max(statesMem, sfMem + updownMem);
        if (Multislide)
            Console.WriteLine($"States: {THREADS} x {StatesMapLength / 4:N0} = {statesMem:N0} (multislide), s/f: {sfMem:N0}; up/dn: {updownMem:N0} Needed: {BytesNeeded:N0} bytes");
        else
            Console.WriteLine($"States: {THREADS} x {StatesMapLength / 2:N0} = {statesMem:N0}, s/f: {sfMem:N0}; up/dn: {updownMem:N0} Needed: {BytesNeeded:N0} bytes");
        Arena = new MemArena(BytesNeeded);
    }

    public override string ToString()
    {
        return $"Puzzle {Size}={Width}x{Height}, states: {RealStates:N0}, total: {Total:N0}. Segments: {SegmentsCount}";
    }

    public const byte STATE_UP = 1;
    public const byte STATE_DN = 2;
    public const byte STATE_LT = 4;
    public const byte STATE_RT = 8;

    public const byte STATE_UP_DN = STATE_UP | STATE_DN;
    public const byte STATE_LT_RT = STATE_LT | STATE_RT;

    public const byte MULTISLIDE_UP_DN = 1;
    public const byte MULTISLIDE_LT_RT = 2;

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

    public void Close()
    {
        Arena.Close();
    }
}
