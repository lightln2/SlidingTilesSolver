using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class SolverTestsLong
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
    public void Test_12_4_3()
    {
        Test(4, 3, 53,
                "1 2 4 9 20 37 63 122 232 431 781 1392 2494 4442 7854 13899 24215 41802 71167 119888 198363 323206 " +
                "515778 811000 1248011 1885279 2782396 4009722 5621354 7647872 10065800 12760413 15570786 18171606 " +
                "20299876 21587248 21841159 20906905 18899357 16058335 12772603 9515217 6583181 4242753 2503873 " +
                "1350268 643245 270303 92311 27116 5390 1115 86 18"
        );
    }


    [TestMethod]
    public void Test_12_3_4()
    {
        Test(3, 4, 53,
                "1 2 4 9 20 37 63 122 232 431 781 1392 2494 4442 7854 13899 24215 41802 71167 119888 198363 323206 " +
                "515778 811000 1248011 1885279 2782396 4009722 5621354 7647872 10065800 12760413 15570786 18171606 " +
                "20299876 21587248 21841159 20906905 18899357 16058335 12772603 9515217 6583181 4242753 2503873 " +
                "1350268 643245 270303 92311 27116 5390 1115 86 18"
        );
    }


    [TestMethod]
    public void Test_12_2_6()
    {
        Test(2, 6, 80,
                "1 2 3 6 11 20 36 60 95 155 258 426 688 1106 1723 2615 3901 5885 8851 13205 19508 28593 41179 58899 " +
                "83582 118109 165136 228596 312542 423797 568233 755727 994641 1296097 1667002 2119476 2660415 3300586 " +
                "4038877 4877286 5804505 6810858 7864146 8929585 9958080 10902749 11716813 12356080 12791679 13002649 " +
                "12981651 12723430 12245198 11572814 10738102 9772472 8720063 7623133 6526376 5459196 4457799 3546306 " +
                "2749552 2068975 1510134 1064591 720002 464913 284204 165094 89649 45758 21471 9583 3829 1427 430 129 33 12 2"
        );
    }

    [TestMethod]
    public void Test_12_6_2()
    {
        Test(6, 2, 80,
                "1 2 3 6 11 20 36 60 95 155 258 426 688 1106 1723 2615 3901 5885 8851 13205 19508 28593 41179 58899 " +
                "83582 118109 165136 228596 312542 423797 568233 755727 994641 1296097 1667002 2119476 2660415 3300586 " +
                "4038877 4877286 5804505 6810858 7864146 8929585 9958080 10902749 11716813 12356080 12791679 13002649 " +
                "12981651 12723430 12245198 11572814 10738102 9772472 8720063 7623133 6526376 5459196 4457799 3546306 " +
                "2749552 2068975 1510134 1064591 720002 464913 284204 165094 89649 45758 21471 9583 3829 1427 430 129 33 12 2"
        );
    }

    [TestMethod]
    public void Test_14_7_2_partial()
    {
        TestPartial(7, 2, 30,
            "1 2 3 6 11 20 37 67 117 198 329 557 942 1575 2597 4241 6724 10535 16396 25515 39362 60532 92089 138969 207274 307725 453000 664240 964874 1392975 1992353");
    }

    [TestMethod]
    public void Test_14_2_7_partial()
    {
        TestPartial(2, 7, 30,
            "1 2 3 6 11 20 37 67 117 198 329 557 942 1575 2597 4241 6724 10535 16396 25515 39362 60532 92089 138969 207274 307725 453000 664240 964874 1392975 1992353");
    }

    [TestMethod]
    public void Test_15_5_3_partial()
    {
        TestPartial(5, 3, 20,
            "1 2 4 9 21 42 89 164 349 644 1349 2473 5109 9110 18489 32321 64962 112445 223153 378761 740095");
    }

    [TestMethod]
    public void Test_15_3_5_partial()
    {
        TestPartial(3, 5, 20,
            "1 2 4 9 21 42 89 164 349 644 1349 2473 5109 9110 18489 32321 64962 112445 223153 378761 740095");
    }

    [TestMethod]
    public void Test_16_4_4_partial()
    {
        TestPartial(4, 4, 10, "1 2 4 10 24 54 107 212 446 946 1948");
    }

    [TestMethod]
    public void Test_16_8_2_partial()
    {
        TestPartial(8, 2, 10, "1 2 3 6 11 20 37 68 125 227 394");
    }

}
