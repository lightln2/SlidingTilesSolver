using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[TestClass]
public class GPUSolverTests
{
    [TestMethod]
    public void Test_Up()
    {
        var str = "4 5 6 7 8 9 10 11";
        var arr = Array.ConvertAll(str.Split(' '), long.Parse);
        GpuSolver.Initialize(4, 3);
        GpuSolver.CalcGPU_Up(arr.Length, arr);
        var result = string.Join(" ", arr);
        Assert.AreEqual("1952 17777 144322 1029603 6336004 32820485 244823046 239500807", result);
        GpuSolver.CalcGPU_Down(arr.Length, arr);
        result = string.Join(" ", arr);
        Assert.AreEqual(str, result);
    }

    [TestMethod]
    public void Test_Dn()
    {
        var str = "0 1 2 3 4 5 6 7";
        var arr = Array.ConvertAll(str.Split(' '), long.Parse);
        GpuSolver.Initialize(4, 3);
        GpuSolver.CalcGPU_Down(arr.Length, arr);
        var result = string.Join(" ", arr);
        Assert.AreEqual("52 533 5286 47527 380168 2661129 15966730 79833611", result);
        GpuSolver.CalcGPU_Up(arr.Length, arr);
        result = string.Join(" ", arr);
        Assert.AreEqual(str, result);
    }


    [TestMethod]
    public void Test_4x3()
    {
        var str = "21 165 1605 16005 160005 1600005 16000005 160000005";
        var arr = Array.ConvertAll(str.Split(' '), long.Parse);
        GpuSolver.Initialize(4, 3);
        GpuSolver.CalcGPU_Up(arr.Length, arr);
        var result = string.Join(" ", arr);
        Assert.AreEqual("17793 17937 760337 19697 779697 992977 16730577 159725617", result);
        GpuSolver.CalcGPU_Down(arr.Length, arr);
        result = string.Join(" ", arr);
        Assert.AreEqual(str, result);
    }

    [TestMethod]
    public void Test_4x4()
    {
        var str = "21 165 1605 16005 160005 1600005 16000005 160000005 1600000005 16000000005 160000000005 1600000000005";
        var arr = Array.ConvertAll(str.Split(' '), long.Parse);
        GpuSolver.Initialize(4, 4);
        GpuSolver.CalcGPU_Up(arr.Length, arr);
        var result = string.Join(" ", arr);
        Assert.AreEqual("47297 47441 1572641 3670241 3157361 701201 16619441 156082481 1600022081 16003654241 160002997361 1599999101201", result);
        foreach (long l in arr) Assert.IsTrue(l % 16 == 1);
        GpuSolver.CalcGPU_Down(arr.Length, arr);
        result = string.Join(" ", arr);
        Assert.AreEqual(str, result);
    }

    [TestMethod]
    public void Test_4x4_Perf()
    {
        var arr = new long[GpuSolver.GPUSIZE];
        for (long i = 0; i < arr.Length; i++)
        {
            arr[i] = i * 16 + 5;
        }
        GpuSolver.Initialize(4, 4);
        GpuSolver.CalcGPU_Up(arr.Length, arr);
        Assert.AreEqual("47281 47297 47313 47329 47345 47361 47377 47393 47409 47425 47441 47457", string.Join(" ", arr.Take(12)));
        foreach (long l in arr) Assert.IsTrue(l % 16 == 1);
        GpuSolver.CalcGPU_Down(arr.Length, arr);
        for (long i = 0; i < arr.Length; i++)
        {
            if (arr[i] != i * 16 + 5)
            {
                Assert.Fail($"at {i}: arr[i]={arr[i]}; i * 16 + 5 = {i * 16 + 5}");
            }
        }
        GpuSolver.PrintStats();
    }

}
