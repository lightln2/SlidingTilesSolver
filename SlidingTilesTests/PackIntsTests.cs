using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

[TestClass]
public class PackIntsTests
{
    void Test(int size, Func<uint, uint> F)
    {
        var arr = new uint[size];
        for (uint i = 0; i < size; i++) arr[i] = F(i);
        var buffer = new byte[10 + arr.Length * 5];
        int bytesLen = PackInts.Pack(arr, size, buffer, 9);
        Console.WriteLine($"{size} -> {bytesLen}");
        int len = PackInts.Unpack(buffer, 9, bytesLen, arr);
        Assert.AreEqual(size, len);
        for (uint i = 0; i < size; i++)
        {
            if (arr[i] != F(i)) Assert.Fail($"at {i}: exp={F(i)} was={arr[i]}");
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
