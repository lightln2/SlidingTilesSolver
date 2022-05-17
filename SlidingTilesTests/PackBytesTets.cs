using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

[TestClass]
public class PackBytesTests
{
    void Test(int size, Func<int, int> F)
    {
        var arr = new byte[size];
        for (int i = 0; i < size; i++) arr[i] = (byte)(F(i) & 15);
        var buffer = new byte[10 + arr.Length];
        var sw = Stopwatch.StartNew();
        int bytesLen = PackBytes.Pack(arr, size, buffer, 9);
        Console.WriteLine($"Pack: {sw.Elapsed}");
        Console.WriteLine($"{size} -> {bytesLen}");
        sw.Restart();
        int len = PackBytes.Unpack(buffer, 9, bytesLen, arr);
        Console.WriteLine($"Unpack: {sw.Elapsed}");
        Assert.AreEqual(size, len);
        for (int i = 0; i < size; i++)
        {
            if (arr[i] != (byte)(F(i) & 15)) Assert.Fail($"at {i}: exp={F(i) & 15} was={arr[i]}");
        }
    }

    [TestMethod]
    public void Test_1_SmallDiff()
    {
        Test(10_000_000, i => i);
    }

    [TestMethod]
    public void Test_2_MediumDiff()
    {
        Test(100_000_000, i => i);
    }

    [TestMethod]
    public void Test_3_LargeDiff()
    {
        Test(300_000_000, i => i);
    }

    [TestMethod]
    public void Test_4_SmallPerformance()
    {
        Test(400_000_000, i => 10);
    }

    [TestMethod]
    public void Test_5_MedPerformance()
    {
        Test(400_000_000, i => 250);
    }

    [TestMethod]
    public void Test_6_HighNums()
    {
        Test(16, i => 255);
    }

}
