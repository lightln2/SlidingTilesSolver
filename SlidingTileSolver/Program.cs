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

        //PuzzleInfo.SetSemifrontierBufferPow(17);
        //PuzzleInfo.THREADS = 2;
        var info = new PuzzleInfo(6, 2, 0);
        //info.MaxSteps = 25;
        MultislideSolver.Solve(info);
        
    }
}
