using System;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        PuzzleInfo.SetSemifrontierBufferPow(17);
        PuzzleInfo.THREADS = 3;
        var info = new PuzzleInfo(4, 4, 0);
        info.MaxSteps = 30;
        PuzzleSolver.Solve(info);
    }
}
