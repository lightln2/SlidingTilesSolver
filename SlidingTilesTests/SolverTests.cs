using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class SolverTests
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
        PuzzleInfo.SetSemifrontierBufferPow(17);
        PuzzleInfo.THREADS = 2;
        var info = new PuzzleInfo(width, height, 0);
        info.MaxSteps = maxSteps;
        Test(info, maxSteps, counts);
    }

    public void Test(PuzzleInfo info, int expectedMax, string counts)
    {
        long[] res = PuzzleSolver.Solve(info);
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
    public void Test_06_2_3_all()
    {
        Test(3, 2, 21, "1 2 3 5 6 7 10 12 12 16 23 25 28 39 44 40 29 21 18 12 6 1");
        Test(2, 3, 21, "1 2 3 5 6 7 10 12 12 16 23 25 28 39 44 40 29 21 18 12 6 1");

        Test(3, 2, /*initialIndex*/ 0, 21, "1 2 3 5 6 7 10 12 12 16 23 25 28 39 44 40 29 21 18 12 6 1");
        Test(3, 2, /*initialIndex*/ 1, 21, "1 3 4 4 6 10 10 10 16 20 20 26 36 40 40 37 29 20 14 9 4 1");
        Test(3, 2, /*initialIndex*/ 2, 21, "1 2 3 5 6 7 10 12 12 16 23 25 28 39 44 40 29 21 18 12 6 1");
        Test(3, 2, /*initialIndex*/ 3, 21, "1 2 3 5 6 7 10 12 12 16 23 25 28 39 44 40 29 21 18 12 6 1");
        Test(3, 2, /*initialIndex*/ 4, 21, "1 3 4 4 6 10 10 10 16 20 20 26 36 40 40 37 29 20 14 9 4 1");
        Test(3, 2, /*initialIndex*/ 5, 21, "1 2 3 5 6 7 10 12 12 16 23 25 28 39 44 40 29 21 18 12 6 1");

    }

    [TestMethod]
    public void Test_08_2_4()
    {
        Test(2, 4, 36,
                "1 2 3 6 10 14 19 28 42 61 85 119 161 215 293 396 506 632 788 985 " +
                "1194 1414 1664 1884 1999 1958 1770 1463 1076 667 361 190 88 39 19 7 1");
    }

    [TestMethod]
    public void Test_08_4_2()
    {
        Test(4, 2, 36,
                "1 2 3 6 10 14 19 28 42 61 85 119 161 215 293 396 506 632 788 985 " +
                "1194 1414 1664 1884 1999 1958 1770 1463 1076 667 361 190 88 39 19 7 1");
    }

    [TestMethod]
    public void Test_08_4_2_Edge()
    {
        Test(4, 2, /*initialIndex*/ 1, 35,
                "1 3 5 7 10 16 24 34 49 72 100 134 182 252 339 439 557 714 892 1082 1281 1503 1741 1913 1963 1883 1681 1330 887 512 280 146 72 36 16 4");
    }

    [TestMethod]
    public void Test_10_2_5()
    {
        Test(2, 5, 55,
                "1 2 3 6 11 19 30 44 68 112 176 271 411 602 851 1232 1783 2530 3567 4996 6838 9279 " +
                "12463 16597 21848 28227 35682 44464 54597 65966 78433 91725 104896 116966 126335 " +
                "131998 133107 128720 119332 106335 91545 75742 60119 45840 33422 23223 15140 9094 " +
                "5073 2605 1224 528 225 75 20 2"
        );
    }

    [TestMethod]
    public void Test_10_5_2()
    {
        Test(5, 2, 55,
                "1 2 3 6 11 19 30 44 68 112 176 271 411 602 851 1232 1783 2530 3567 4996 6838 9279 " +
                "12463 16597 21848 28227 35682 44464 54597 65966 78433 91725 104896 116966 126335 " +
                "131998 133107 128720 119332 106335 91545 75742 60119 45840 33422 23223 15140 9094 " +
                "5073 2605 1224 528 225 75 20 2"
        );
    }

    [TestMethod]
    public void Test_10_5_2_Edge1()
    {
        Test(5, 2, /*initialIndex*/ 1, 55,
                "1 3 5 8 14 23 34 55 91 141 215 333 482 698 1035 1492 2099 2997 4205 5820 7939 10711 14307 18930 24582 31406 39534 48945 59487 71305 84102 97413 110161 120903 128696 132416 130928 124148 113414 99909 84673 68860 53982 40757 29391 19907 12524 7378 4068 2118 1022 464 194 59 15 1");
    }

    [TestMethod]
    public void Test_10_5_2_Edge2()
    {
        Test(5, 2, /*initialIndex*/ 2, 54,
                "1 3 6 10 14 22 38 60 94 156 236 336 494 736 1066 1555 2231 3116 4356 6073 8244 11103 14810 19486 25258 32204 40434 50009 60709 72461 85317 98643 111169 121710 129106 132268 130372 122962 111659 97829 82440 67072 52697 40106 29107 19599 12181 7144 3984 2034 982 447 183 56 12");
    }


    [TestMethod]
    public void Test_09_3_3()
    {
        Test(3, 3, 31,
            "1 2 4 8 16 20 39 62 116 152 286 396 748 1024 1893 2512 4485 5638 9529 " +
            "10878 16993 17110 23952 20224 24047 15578 14560 6274 3910 760 221 2"
        );
    }

    [TestMethod]
    public void Test_09_3_3_Edge()
    {
        Test(3, 3, /*initialIndex*/ 1, 31,
                "1 3 5 10 14 28 42 80 108 202 278 524 726 1348 1804 3283 4193 7322 8596 13930 14713 21721 19827 25132 18197 18978 9929 7359 2081 878 126 2");
    }

    [TestMethod]
    public void Test_09_3_3_Center()
    {
        Test(3, 3, /*initialIndex*/ 4, 30,
                "1 4 8 8 16 32 60 72 136 200 376 512 964 1296 2368 3084 5482 6736 11132 12208 18612 18444 24968 19632 22289 13600 11842 4340 2398 472 148");
    }

    [TestMethod]
    public void Test_12_4_3_partial()
    {
        TestPartial(4, 3, 30,
            "1 2 4 9 20 37 63 122 232 431 781 1392 2494 4442 7854 13899 24215 41802 71167 119888 198363 323206 515778 811000 1248011 1885279 2782396 4009722 5621354 7647872 10065800");
    }

    [TestMethod]
    public void Test_12_3_4_partial()
    {
        TestPartial(3, 4, 30, 
            "1 2 4 9 20 37 63 122 232 431 781 1392 2494 4442 7854 13899 24215 41802 71167 119888 198363 323206 515778 811000 1248011 1885279 2782396 4009722 5621354 7647872 10065800");
    }


    [TestMethod]
    public void Test_12_2_6_partial()
    {
        TestPartial(2, 6, 30, 
            "1 2 3 6 11 20 36 60 95 155 258 426 688 1106 1723 2615 3901 5885 8851 13205 19508 28593 41179 58899 83582 118109 165136 228596 312542 423797 568233");
    }

    [TestMethod]
    public void Test_12_6_2_partial()
    {
        TestPartial(6, 2, 30, 
            "1 2 3 6 11 20 36 60 95 155 258 426 688 1106 1723 2615 3901 5885 8851 13205 19508 28593 41179 58899 83582 118109 165136 228596 312542 423797 568233");
    }

    [TestMethod]
    public void Test_14_7_2_partial()
    {
        TestPartial(7, 2, 10,
            "1 2 3 6 11 20 37 67 117 198 329");
    }

    [TestMethod]
    public void Test_14_2_7_partial()
    {
        TestPartial(2, 7, 10,
            "1 2 3 6 11 20 37 67 117 198 329");
    }

    [TestMethod]
    public void Test_15_5_3_partial()
    {
        TestPartial(5, 3, 8,
            "1 2 4 9 21 42 89 164 349");
    }

    [TestMethod]
    public void Test_15_3_5_partial()
    {
        TestPartial(3, 5, 8,
            "1 2 4 9 21 42 89 164 349");
    }

    [TestMethod]
    public void Test_16_4_4_partial()
    {
        TestPartial(4, 4, 5, "1 2 4 10 24 54");
    }

    [TestMethod]
    public void Test_16_8_2_partial()
    {
        TestPartial(8, 2, 5, "1 2 3 6 11 20");
    }

}
