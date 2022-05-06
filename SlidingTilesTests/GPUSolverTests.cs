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
    public void Test_4x3()
    {
        var inp = new long[] {
                1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000
            };
        GpuSolver.Initialize(4, 3);
        var outp = new long[1000];
        GpuSolver.CalcGPU(inp.Length, inp, outp);
        var result = string.Join(" ", outp.Take(inp.Length * 2));
        Assert.AreEqual("-1 533 244823046 -1 47520 380264 6336996 -1 -1 73572 -1 44484 -1 906388 -1 9930436 -1 100001140", result);
    }

    [TestMethod]
    public void Test_4x3_perf()
    {
        var inp = Enumerable.Range(0, 10_000_000).Select(i => (long)i).ToArray();
        GpuSolver.Initialize(4, 3);
        var outp = new long[inp.Length * 2];
        GpuSolver.CalcGPU(inp.Length, inp, outp);
        var hash = outp.Sum();
        Assert.AreEqual(526310943196224, hash);
    }

    [TestMethod]
    public void Test_4x4()
    {
        var inp = new long[] {
                1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000,
                1_000_000_000, 10_000_000_000, 100_000_000_000, 1_000_000_000_000, 10_000_000_000_000
            };
        GpuSolver.Initialize(4, 4);
        var outp = new long[1000];
        GpuSolver.CalcGPU(inp.Length, inp, outp);
        var result = string.Join(" ", outp.Take(inp.Length * 2));
        Assert.AreEqual("-1 725 4727923206 697426329614 131040 1572584 63948516 12454042604 -1 86164 -1 119796 -1 990436 -1 10445044 -1 99804660 -1 999968820 -1 10000292900 -1 99999932500 -1 1000000248788 -1 10000000445044", result);
    }


}
