using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public class MultislideFrontier : IDisposable
{
    private static TimeSpan TimeWrite = TimeSpan.Zero;
    private static TimeSpan TimeRead = TimeSpan.Zero;

    private SegmentedFile File;
    private Stopwatch Timer = new Stopwatch();

    public MultislideFrontier(PuzzleInfo info, params string[] files)
    {
        File = new SegmentedFile(info.SegmentsCount, files);
    }

    public void Swap(MultislideFrontier other)
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
    public void Write(int segment, byte[] tempBuffer, uint[] vals, int count)
    {
        if (count == 0) return;
        Timer.Restart();
        int byteLen = PackStates.PackVals(vals, count, tempBuffer);
        File.WriteSegment(segment, tempBuffer, 0, byteLen);
        TimeWrite += Timer.Elapsed;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int Read(int segment, int part, byte[] tempBuffer, uint[] vals)
    {
        Timer.Restart();
        int byteLen = File.ReadSegment(segment, part, tempBuffer);
        if (byteLen == 0) return 0;
        int count = PackStates.UnpackVals(tempBuffer, byteLen, vals);
        TimeRead += Timer.Elapsed;
        return count;
    }

    public void Dispose()
    {
        File.Dispose();
    }

    public long TotalSize() => File.TotalSize();

    public static void PrintStats()
    {
        Console.WriteLine($"Frontier: write={TimeWrite}, read={TimeRead}");
    }

}
