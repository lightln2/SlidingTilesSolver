using System;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        var info = new PuzzleInfo(4, 3, 0);
        //info.MaxSteps = 15;
        PuzzleSolver.Solve(info);
    }
}
