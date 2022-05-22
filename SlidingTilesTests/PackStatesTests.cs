using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

[TestClass]
public class PackStatesTests
{
    void Test(int size, Func<long, long> F, Func<int, int> S)
    {
        var vals = new uint[size / 16 * 16 + 32];
        var states = new byte[size / 16 * 16 + 32];
        for (int i = 0; i < size; i++)
        {
            vals[i] = (uint)F(i);
            states[i] = (byte)(S(i) % 16);
        }
        var buffer = new byte[size * 5 + 32];
        var sw = Stopwatch.StartNew();
        int bytesLen = PackStates.Pack(vals, states, size, buffer);
        Console.WriteLine($"Pack: {sw.Elapsed}");
        Console.WriteLine($"{size} -> {bytesLen}");
        sw.Restart();
        int len = PackStates.Unpack(buffer, bytesLen, vals, states);
        Console.WriteLine($"Unpack: {sw.Elapsed}");
        Assert.AreEqual(size, len);
        for (int i = 0; i < size; i++)
        {
            if (vals[i] != (uint)F(i)) Assert.Fail($"val at {i}: exp={(uint)F(i)} was={vals[i]}");
            if (states[i] != S(i) % 16) Assert.Fail($"state at {i}: exp={S(i) % 16} was={states[i]}");
        }
    }

    [TestMethod]
    public void Test_1_SmallDiff()
    {
        Test(1_000_000, i => i, i => 13);
    }

    [TestMethod]
    public void Test_2_MediumDiff()
    {
        Test(1_000_001, i => i * 1000, i=> 11);
    }

    [TestMethod]
    public void Test_3_LargeDiff()
    {
        Test(1_000_003, i => i * 20000, i => i);
    }

    [TestMethod]
    public void Test_4_XLargeDiff()
    {
        Test(1_005, i => i * 4_000_000, i=> 3);
    }

    [TestMethod]
    public void Test_5_SmallPerformance()
    {
        Test(400_000_000, i => i, i => 7);
    }

    [TestMethod]
    public void Test_6_MedPerformance()
    {
        Test(200_000_001, i => i * 13, i => 7);
    }

    [TestMethod]
    public void Test_7_AvgPerformance()
    {
        Test(200_000_002, i => i * 13 + i / 200 * 200 + i / 20000 * 20000 + i / 2_000_000 * 1_000_000, i => 7);
    }

}
