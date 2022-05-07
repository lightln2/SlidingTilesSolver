using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

[TestClass]
public class SegmentedFileTests
{
    [TestMethod]
    public void Test_1_WriteRead()
    {
        using var segmentedFile = new SegmentedFile("./test", 4096);
        var buffer = new uint[4 * 1024 * 1024];
        for (uint i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i;
        }
        for (int i = 0; i < 10; i++)
        {
            segmentedFile.WriteSegment(7, buffer, 0, buffer.Length);
        }
        segmentedFile.WriteSegment(7, buffer, 0, 23);

        long v = 0;
        long total = 0;
        for (int i = 0; i < segmentedFile.SegmentParts(7); i++)
        {
            int count = segmentedFile.ReadSegment(7, i, buffer);
            total += count;
            for (int j = 0; j < count; j++) v += buffer[j];
        }
        Assert.AreEqual(41943063, total);
        Assert.AreEqual(87960909250813, v);
        SegmentedFile.PrintStats();
    }

    [TestMethod]
    public void Test_2_TwoSegments()
    {
        using var segmentedFile = new SegmentedFile("./test", 4096);
        var buffer = new uint[4 * 1024 * 1024];
        for (uint i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i;
        }
        segmentedFile.WriteSegment(7, buffer, 0, buffer.Length);
        segmentedFile.WriteSegment(8, buffer, 0, 23);

        long v7 = 0;
        long total7 = 0;
        for (int i = 0; i < segmentedFile.SegmentParts(7); i++)
        {
            int count = segmentedFile.ReadSegment(7, i, buffer);
            total7 += count;
            for (int j = 0; j < count; j++) v7 += buffer[j];
        }
        Assert.AreEqual(4194304, total7);
        Assert.AreEqual(8796090925056, v7);

        long v8 = 0;
        long total8 = 0;
        for (int i = 0; i < segmentedFile.SegmentParts(8); i++)
        {
            int count = segmentedFile.ReadSegment(8, i, buffer);
            total8 += count;
            for (int j = 0; j < count; j++) v8 += buffer[j];
        }
        Assert.AreEqual(23, total8);
        Assert.AreEqual(253, v8);
        SegmentedFile.PrintStats();
    }

    [TestMethod]
    public void Test_3_Clear()
    {
        using var segmentedFile = new SegmentedFile("./test", 4096);
        var buffer = new uint[4 * 1024 * 1024];
        for (uint i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i;
        }
        segmentedFile.WriteSegment(7, buffer, 0, buffer.Length);
        segmentedFile.Clear();
        segmentedFile.WriteSegment(7, buffer, 0, 23);

        long v = 0;
        long total = 0;
        for (int i = 0; i < segmentedFile.SegmentParts(7); i++)
        {
            int count = segmentedFile.ReadSegment(7, i, buffer);
            total += count;
            for (int j = 0; j < count; j++) v += buffer[j];
        }
        Assert.AreEqual(23, total);
        Assert.AreEqual(253, v);
        SegmentedFile.PrintStats();
    }

    [TestMethod]
    public void Test_2_ManySegments()
    {
        int SEGMENTS = 65536;
        using var segmentedFile = new SegmentedFile("./test", SEGMENTS);
        var buffer = new uint[4 * 1024 * 1024];
        for (uint i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i;
        }
        for (int i = 0; i < SEGMENTS; i++)
        {
            segmentedFile.WriteSegment(i, buffer, i * 10, 1024);
        }

        for (int i = 0; i < SEGMENTS; i++)
        {
            int parts = segmentedFile.SegmentParts(i);
            Assert.AreEqual(1, parts);
            int count = segmentedFile.ReadSegment(i, 0, buffer);
            Assert.AreEqual(1024, count);
            Assert.AreEqual(i * 10, (int)buffer[0]);
        }
        SegmentedFile.PrintStats();
    }


}
