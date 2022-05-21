using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

[TestClass]
public class PackStatesTests
{
    void Test(int size, Func<long, long> F)
    {
        var arr = new long[size];
        for (int i = 0; i < arr.Length; i++) arr[i] = F(i);
        var buffer = new byte[arr.Length * 5];
        var sw = Stopwatch.StartNew();
        int bytesLen = PackStates.Pack(arr, arr.Length, buffer);
        Console.WriteLine($"Pack: {sw.Elapsed}");
        Console.WriteLine($"{size} -> {bytesLen}");
        sw.Restart();
        int len = PackStates.Unpack(buffer, bytesLen, arr);
        Console.WriteLine($"Unpack: {sw.Elapsed}");
        Assert.AreEqual(arr.Length, len);
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] != F(i)) Assert.Fail($"at {i}: exp={F(i)} was={arr[i]}");
        }
    }

    [TestMethod]
    public void Test_1_SmallDiff()
    {
        Test(1_000_000, i => (i << 4) | 13);
    }

    [TestMethod]
    public void Test_2_MediumDiff()
    {
        Test(1_000_001, i => ((i * 1000) << 4) | 11);
    }

    [TestMethod]
    public void Test_3_LargeDiff()
    {
        Test(1_000_003, i => ((i * 20000) << 4) | (i & 15));
    }

    [TestMethod]
    public void Test_4_XLargeDiff()
    {
        Test(1_005, i => ((i * 4_000_000) << 4) | 3);
    }

    [TestMethod]
    public void Test_5_SmallPerformance()
    {
        Test(400_000_000, i => 7 | (i << 4));
    }

    [TestMethod]
    public void Test_6_MedPerformance()
    {
        Test(200_000_001, i => 7 | ((i * 13) << 4));
    }

    [TestMethod]
    public void Test_7_AvgPerformance()
    {
        Test(200_000_002, i => 7 | ((i * 13 + i / 200 * 200 + i / 20000 * 20000 + i / 2_000_000 * 1_000_000) << 4));
    }

}
