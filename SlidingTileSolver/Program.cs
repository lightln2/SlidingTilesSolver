using System;
using System.Diagnostics;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        /*
        var info = new PuzzleInfo(8, 2, 0);
        PuzzleSolver.Solve(info);
        */

        PuzzleInfo.THREADS = 6;
        var info = new PuzzleInfo(4, 4, 0, true);
        MultislideSolver.Solve(info);

    }
}
