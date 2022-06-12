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

        PuzzleInfo.SEMIFRONTIER_DIFF_ENCODING = false;
        PuzzleInfo.THREADS = 6;
        var info = new PuzzleInfo(7, 2, 0, true);
        MultislideSolver.Solve(info);

        /*
        PuzzleInfo.THREADS = 3;
        var info = new PuzzleInfo(7, 2, 0);
        PuzzleSolver.Solve(info);
        */
    }
}
