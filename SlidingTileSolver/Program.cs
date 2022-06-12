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

        //PuzzleInfo.SetSemifrontierBufferPow(16);
        PuzzleInfo.SEMIFRONTIER_DIFF_ENCODING = false;
        PuzzleInfo.THREADS = 4;
        var info = new PuzzleInfo(4, 3, 0, /*multislide*/ true);
        //info.MaxSteps = 22;
        //MultislideSolver.Solve(info);
        PuzzleSolver.Solve(info);
    }
}
