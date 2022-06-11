using System;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        /*
        PuzzleInfo.SetSemifrontierBufferPow(17);
        PuzzleInfo.THREADS = 3;
        var info = new PuzzleInfo(8, 2, 0);
        info.MaxSteps = 8;
        MultislideSolver.Solve(info);
        */

        var info = new PuzzleInfo(4, 3, 0);
        MultislideSolver.Solve(info);
        
    }
}
