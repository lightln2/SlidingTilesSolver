using System;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        int SEGMENTS = 65536;
        var sw = Stopwatch.StartNew();
        using var fileSegment = new SegmentedFile("c:/temp/test", SEGMENTS);
        var buffer = new uint[4 * 1024 * 1024];
        for (uint i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i;
        }
        for (int i = 0; i < 10; i++)
        {
            fileSegment.WriteSegment(7, buffer, 0, buffer.Length);
        }
        fileSegment.WriteSegment(7, buffer, 0, 23);

        Console.WriteLine($"Time: {sw.Elapsed}");
        sw.Restart();
        long v = 0;
        long total = 0;
        for (int i = 0; i < fileSegment.SegmentParts(7); i++)
        {
            int count = fileSegment.ReadSegment(7, i, buffer);
            total += count;
            for (int j = 0; j < count; j++) v += buffer[j];
        }
        Console.WriteLine($"Time: {sw.Elapsed} vals={total:N0}; sum={v}");
        SegmentedFile.PrintStats();
    }
}
