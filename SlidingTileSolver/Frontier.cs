using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class Frontier : IDisposable
{
    private static TimeSpan TimeWrite = TimeSpan.Zero;
    private static TimeSpan TimeRead = TimeSpan.Zero;

    private SegmentedFile File;
    private Stopwatch Timer = new Stopwatch();

    public Frontier(PuzzleInfo info, params string[] files)
    {
        File = new SegmentedFile(info.SegmentsCount, files);
    }

    public void Swap(Frontier other)
    {
        Util.Swap(ref File, ref other.File);
    }

    public int SegmentsCount => File.SegmentsCount;

    public int SegmentParts(int segment) => File.SegmentParts(segment);

    public void Clear()
    {
        File.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Write(int segment, byte[] tempBuffer, uint[] vals, byte[] states, int count)
    {
        if (count == 0) return;
        Timer.Restart();
        int byteLen = PackStates.Pack(vals, states, count, tempBuffer);
        File.WriteSegment(segment, tempBuffer, 0, byteLen);
        TimeWrite += Timer.Elapsed;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int Read(int segment, int part, byte[] tempBuffer, uint[] vals, byte[] states)
    {
        Timer.Restart();
        int byteLen = File.ReadSegment(segment, part, tempBuffer);
        if (byteLen == 0) return 0;
        int count = PackStates.Unpack(tempBuffer, byteLen, vals, states);
        TimeRead += Timer.Elapsed;
        return count;
    }

    public long TotalSize() => File.TotalSize();

    public void Dispose()
    {
        File.Dispose();
    }

    public static void PrintStats()
    {
        Console.WriteLine($"Frontier: write={TimeWrite}, read={TimeRead}");
    }

}
