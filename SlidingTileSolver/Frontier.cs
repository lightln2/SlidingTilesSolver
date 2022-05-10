using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Frontier : IDisposable
{
    private readonly SegmentedFileLong File;

    public readonly long[] Buffer = new long[1024 * 1024];

    public Frontier(string file, PuzzleInfo info)
    {
        File = new SegmentedFileLong(file, info.SegmentsCount);
    }

    public int SegmentsCount => File.SegmentsCount;

    public int SegmentParts(int segment) => File.SegmentParts(segment);

    public void Clear() => File.Clear();

    public void Write(int segment, long[] arr, int count) => File.WriteSegment(segment, arr, 0, count);

    public int Read(int segment, int part, long[] arr) => File.ReadSegment(segment, part, arr);

    public void Dispose()
    {
        File.Dispose();
    }
}
