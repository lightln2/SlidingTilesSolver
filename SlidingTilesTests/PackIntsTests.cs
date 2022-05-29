using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;

[TestClass]
public class PackIntsTests
{
    void Test(uint[] arr)
    {
        int size = arr.Length;
        var exp = arr.ToArray();
        for (int i = 1; i < arr.Length; i++) arr[i] += arr[i - 1];
        var buffer = new byte[10 + arr.Length * 5];
        var sw = Stopwatch.StartNew();
        int bytesLen = PackInts.PackDiff(arr, size, buffer, 9);
        Console.WriteLine($"Pack: {sw.Elapsed}");
        Console.WriteLine($"{size} -> {bytesLen}");
        sw.Restart();
        int len = PackInts.UnpackDiff(buffer, 9, bytesLen, arr);
        for (int i = arr.Length - 1; i >= 1; i--) arr[i] -= arr[i - 1];
        Console.WriteLine($"Unpack: {sw.Elapsed}");
        Assert.AreEqual(size, len);
        for (uint i = 0; i < size; i++)
        {
            if (arr[i] != exp[i]) Assert.Fail($"at {i}: exp={exp[i]} was={arr[i]}");
        }
    }

    void Test(int size, Func<uint, uint> F)
    {
        var arr = new uint[size];
        for (uint i = 0; i < size; i++) arr[i] = F(i);
        for (int i = 1; i < arr.Length; i++) arr[i] += arr[i - 1];
        var buffer = new byte[10 + arr.Length * 5];
        var sw = Stopwatch.StartNew();
        int bytesLen = PackInts.PackDiff(arr, size, buffer, 9);
        Console.WriteLine($"Pack: {sw.Elapsed}");
        Console.WriteLine($"{size} -> {bytesLen}");
        sw.Restart();
        int len = PackInts.UnpackDiff(buffer, 9, bytesLen, arr);
        for (int i = arr.Length - 1; i >= 1; i--) arr[i] -= arr[i - 1];
        Console.WriteLine($"Unpack: {sw.Elapsed}");
        Assert.AreEqual(size, len);
        for (uint i = 0; i < size; i++)
        {
            if (arr[i] != F(i)) Assert.Fail($"at {i}: exp={F(i)} was={arr[i]}");
        }
    }

    [TestMethod]
    public void Test_0_Different()
    {
        Test(new uint[] { 
            0x567803, 0x05, 0x77991222, 0x5667, 0x6674, 0xEA, 0xFFEEDDCC, 0xCDCDC0,
            0x7803, 0x05, 0x77991222, 0x566789, 0x1, 0x03000F, 0xFFEEDDCC, 0xCDC6,
        });
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
        Test(400_000_000, i => i);
    }

    [TestMethod]
    public void Test_4_SmallPerformance()
    {
        Test(400_000_000, i => 10u);
    }

    [TestMethod]
    public void Test_5_MedPerformance()
    {
        Test(400_000_000, i => 300u);
    }

    [TestMethod]
    public void Test_6_AvgPerformance()
    {
        Test(400_000_000, i => 10u + (i % 300 == 0 ? 300u : 0) + (i % 20_000 == 0 ? 20_000u : 0));
    }

    [TestMethod]
    public void Test_7_HighNums()
    {
        Test(16, i => uint.MaxValue);
    }

}
