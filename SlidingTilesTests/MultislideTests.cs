using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class MultislideTests
{
    public void Test(int width, int height, int expectedMax, string counts)
    {
        Test(width, height, 0, expectedMax, counts);
    }

    public void Test(int width, int height, int initialIndex, int expectedMax, string counts)
    {
        Test(new PuzzleInfo(width, height, initialIndex), expectedMax, counts);
    }

    public void TestPartial(int width, int height, int maxSteps, string counts)
    {
        var info = new PuzzleInfo(width, height, 0);
        info.MaxSteps = maxSteps;
        Test(info, maxSteps, counts);
    }

    public void Test(PuzzleInfo info, int expectedMax, string counts)
    {
        PuzzleInfo.THREADS = 3;
        PuzzleInfo.SetSemifrontierBufferPow(16);
        long[] res = MultislideSolver.Solve(info);
        int max = res.Length - 1;
        Assert.AreEqual(expectedMax, max);
        Assert.AreEqual(counts, string.Join(" ", res));
    }

    [TestMethod]
    public void Test_04_2_2()
    {
        Test(2, 2, 6, "1 2 2 2 2 2 1");
    }

    [TestMethod]
    public void Test_06_3_2()
    {
        Test(3, 2, 20, "1 3 4 6 8 12 15 18 22 30 36 45 42 30 28 24 18 9 5 3 1");
    }

    [TestMethod]
    public void Test_06_2_3()
    {
        Test(2, 3, 20, "1 3 4 6 8 12 15 18 22 30 36 45 42 30 28 24 18 9 5 3 1");
    }

    [TestMethod]
    public void Test_06_3_2_Edge()
    {
        Test(3, 2, 1, 20, "1 3 4 6 8 12 14 18 24 30 36 45 43 30 24 24 20 9 5 3 1");
    }

    [TestMethod]
    public void Test_06_2_3_Edge()
    {
        Test(2, 3, 2, 20, "1 3 4 6 8 12 14 18 24 30 36 45 43 30 24 24 20 9 5 3 1");
    }

    [TestMethod]
    public void Test_06_3_2_All()
    {
        Test(3, 2, 0, 20, "1 3 4 6 8 12 15 18 22 30 36 45 42 30 28 24 18 9 5 3 1");
        Test(3, 2, 2, 20, "1 3 4 6 8 12 15 18 22 30 36 45 42 30 28 24 18 9 5 3 1");
        Test(3, 2, 3, 20, "1 3 4 6 8 12 15 18 22 30 36 45 42 30 28 24 18 9 5 3 1");
        Test(3, 2, 5, 20, "1 3 4 6 8 12 15 18 22 30 36 45 42 30 28 24 18 9 5 3 1");

        Test(3, 2, 1, 20, "1 3 4 6 8 12 14 18 24 30 36 45 43 30 24 24 20 9 5 3 1");
        Test(3, 2, 4, 20, "1 3 4 6 8 12 14 18 24 30 36 45 43 30 24 24 20 9 5 3 1");
    }

    [TestMethod]
    public void Test_06_2_3_All()
    {
        Test(2, 3, 0, 20, "1 3 4 6 8 12 15 18 22 30 36 45 42 30 28 24 18 9 5 3 1");
        Test(2, 3, 1, 20, "1 3 4 6 8 12 15 18 22 30 36 45 42 30 28 24 18 9 5 3 1");
        Test(2, 3, 4, 20, "1 3 4 6 8 12 15 18 22 30 36 45 42 30 28 24 18 9 5 3 1");
        Test(2, 3, 5, 20, "1 3 4 6 8 12 15 18 22 30 36 45 42 30 28 24 18 9 5 3 1");

        Test(2, 3, 2, 20, "1 3 4 6 8 12 14 18 24 30 36 45 43 30 24 24 20 9 5 3 1");
        Test(2, 3, 3, 20, "1 3 4 6 8 12 14 18 24 30 36 45 43 30 24 24 20 9 5 3 1");
    }

    [TestMethod]
    public void Test_08_4_2()
    {
        Test(4, 2, 25,
             "1 4 6 12 18 36 53 96 136 232 324 544 728 1064 1366 1928 2321 2780 2884 2436 1825 800 368 140 50 8");
    }

    [TestMethod]
    public void Test_08_2_4()
    {
        Test(2, 4, 25,
             "1 4 6 12 18 36 53 96 136 232 324 544 728 1064 1366 1928 2321 2780 2884 2436 1825 800 368 140 50 8");
    }

    [TestMethod]
    public void Test_08_4_2_Edge()
    {
        Test(4, 2, 1, 26,
             "1 4 6 12 18 36 52 96 138 232 322 544 723 1064 1375 1928 2343 2780 2837 2436 1821 800 391 140 50 8 3");
    }

    [TestMethod]
    public void Test_08_2_4_Edge()
    {
        Test(2, 4, 2, 26,
             "1 4 6 12 18 36 52 96 138 232 322 544 723 1064 1375 1928 2343 2780 2837 2436 1821 800 391 140 50 8 3");
    }

    [TestMethod]
    public void Test_09_3_3()
    {
        Test(3, 3, 24,
            "1 4 8 16 32 64 127 244 454 856 1576 2854 5117 8588 13466 19739 26558 31485 30985 23494 11751 3390 589 41 1");
    }

    [TestMethod]
    public void Test_09_3_3_Edge()
    {
        Test(3, 3, 1, 24,
             "1 4 8 16 32 64 126 238 456 862 1590 2863 5114 8618 13449 19642 26478 31502 30815 23401 12148 3381 588 42 2");
    }

    [TestMethod]
    public void Test_09_3_3_Center()
    {
        Test(3, 3, 4, 24,
             "1 4 8 16 32 64 124 236 452 872 1598 2880 5048 8632 13500 19412 26136 31859 30304 23402 12743 3424 654 35 4");
    }

    [TestMethod]
    public void Test_10_5_2()
    {
        Test(5, 2, 36,
             "1 5 8 20 32 80 127 300 458 960 1458 3055 4540 8780 12694 23520 33370 57200 77373 119315 151791 204225 235617 256305 246642 178775 120853 49655 20885 4530 1229 440 113 30 8 5 1"
        );
    }

    [TestMethod]
    public void Test_10_5_2_Edge1()
    {
        Test(5, 2, 1, 36,
             "1 5 8 20 32 80 126 300 460 960 1452 3055 4535 8780 12713 23520 33327 57200 77109 119315 151662 204225 235561 256305 245483 178775 121302 49655 21776 4530 1520 440 123 30 9 5 1"
        );
    }

    [TestMethod]
    public void Test_10_5_2_Edge2()
    {
        Test(5, 2, 2, 36,
             "1 5 8 20 32 80 126 300 460 960 1448 3055 4545 8780 12750 23520 33058 57200 76893 119315 150211 204225 235212 256305 245994 178775 122405 49655 22293 4530 1633 440 122 30 8 5 1"
        );
    }

    [TestMethod]
    public void Test_12_4_3_Partial()
    {
        var info = new PuzzleInfo(4, 3, 0);
        info.MaxSteps = 20;
        Test(info, 20,
             "1 5 12 30 72 180 431 1058 2418 5711 12858 29630 65053 145090 303771 640141 1260032 2476812 4490822 7999853 12981931"
        );
    }

    [TestMethod]
    public void Test_12_3_4_Partial()
    {
        var info = new PuzzleInfo(3, 4, 0);
        info.MaxSteps = 20;
        Test(info, 20,
             "1 5 12 30 72 180 431 1058 2418 5711 12858 29630 65053 145090 303771 640141 1260032 2476812 4490822 7999853 12981931"
        );
    }

    [TestMethod]
    public void Test_12_6_2_Partial()
    {
        var info = new PuzzleInfo(6, 2, 0);
        info.MaxSteps = 20;
        Test(info, 20,
             "1 6 10 30 50 150 249 720 1152 2880 4610 11544 18128 42684 65924 149514 228778 488796 728433 1467000 2136054"
        );
    }

    [TestMethod]
    public void Test_14_7_2_Partial()
    {
        var info = new PuzzleInfo(7, 2, 0);
        info.MaxSteps = 10;
        Test(info, 10,
             "1 7 12 42 72 252 431 1470 2430 7070 11728"
        );
    }

    [TestMethod]
    public void Test_15_5_3_Partial()
    {
        var info = new PuzzleInfo(5, 3, 0);
        info.MaxSteps = 10;
        Test(info, 10,
            "1 6 16 48 128 384 1023 3036 7796 22155 55915"
        );
    }


    [TestMethod]
    public void Test_16_4_4_Partial()
    {
        var info = new PuzzleInfo(4, 4, 0);
        info.MaxSteps = 8;
        Test(info, 8,
            "1 6 18 54 162 486 1457 4334 12568"
        );
    }

    [TestMethod]
    public void Test_16_8_2_Partial()
    {
        var info = new PuzzleInfo(8, 2, 0);
        info.MaxSteps = 8;
        Test(info, 8,
            "1 8 14 56 98 392 685 2688 4552"
        );
    }
}
