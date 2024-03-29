﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class MultislideTestsLong
{
    public void Test(int width, int height, int expectedMax, string counts)
    {
        Test(width, height, 0, expectedMax, counts);
    }

    public void Test(int width, int height, int initialIndex, int expectedMax, string counts)
    {
        PuzzleInfo.THREADS = 4;
        PuzzleInfo.SetSemifrontierBufferPow(17);
        Test(new PuzzleInfo(width, height, initialIndex, /*multislide=*/ true), expectedMax, counts);
    }

    public void TestPartial(int width, int height, int maxSteps, string counts)
    {
        PuzzleInfo.THREADS = 4;
        PuzzleInfo.SetSemifrontierBufferPow(17);
        var info = new PuzzleInfo(width, height, 0, /*multislide=*/ true);
        info.MaxSteps = maxSteps;
        Test(info, maxSteps, counts);
    }

    public void Test(PuzzleInfo info, int expectedMax, string counts)
    {
        long[] res = MultislideSolver.Solve(info);
        int max = res.Length - 1;
        Assert.AreEqual(expectedMax, max);
        Assert.AreEqual(counts, string.Join(" ", res));
    }

    [TestMethod]
    public void Test_12_4_3()
    {
        Test(4, 3, 33,
             "1 5 12 30 72 180 431 1058 2418 5711 12858 29630 65053 145090 303771 640141 1260032 2476812 4490822 7999853 12981931 20265326 28065825 36086638 39470660 37460934 27258384 14584710 4854329 939300 93345 5229 204 5"
        );
    }

    [TestMethod]
    public void Test_12_4_3_Edge_Long_1()
    {
        Test(4, 3, 1, 33,
             "1 5 12 30 72 180 430 1050 2412 5703 12809 29565 64775 144659 301809 636802 1248105 2461550 4436739 7936801 12794069 20106735 27599715 35883882 38950239 37701982 27441399 15339717 5207392 1077108 108057 6729 264 3"
        );
    }

    [TestMethod]
    public void Test_12_4_3_Edge_Short_4()
    {
        Test(4, 3, 4, 33,
             "1 5 12 30 72 180 430 1042 2408 5704 12868 29494 64809 144877 302756 634836 1252328 2455733 4461258 7892439 12889298 20001822 27904695 35596771 39489873 37352758 27835369 14946028 5131128 991700 95418 4531 125 2"
        );
    }

    [TestMethod]
    public void Test_12_4_3_Center()
    {
        Test(4, 3, 5, 33,
             "1 5 12 30 72 180 428 1040 2392 5706 12802 29422 64448 144243 300303 629826 1237776 2434281 4394747 7803776 12670143 19788852 27377829 35346934 38934676 37627719 28051588 15799164 5555197 1163201 117359 6454 192 2"
        );
    }

    [TestMethod]
    public void Test_12_3_4()
    {
        Test(3, 4, 33,
             "1 5 12 30 72 180 431 1058 2418 5711 12858 29630 65053 145090 303771 640141 1260032 2476812 4490822 7999853 12981931 20265326 28065825 36086638 39470660 37460934 27258384 14584710 4854329 939300 93345 5229 204 5"
        );
    }

    [TestMethod]
    public void Test_12_3_4_Edge_Long_3()
    {
        Test(3, 4, 3, 33,
             "1 5 12 30 72 180 430 1050 2412 5703 12809 29565 64775 144659 301809 636802 1248105 2461550 4436739 7936801 12794069 20106735 27599715 35883882 38950239 37701982 27441399 15339717 5207392 1077108 108057 6729 264 3"
        );
    }

    [TestMethod]
    public void Test_12_3_4_Edge_Short_1()
    {
        Test(3, 4, 1, 33,
             "1 5 12 30 72 180 430 1042 2408 5704 12868 29494 64809 144877 302756 634836 1252328 2455733 4461258 7892439 12889298 20001822 27904695 35596771 39489873 37352758 27835369 14946028 5131128 991700 95418 4531 125 2"
        );
    }

    [TestMethod]
    public void Test_12_3_4_Center()
    {
        Test(3, 4, 4, 33,
             "1 5 12 30 72 180 428 1040 2392 5706 12802 29422 64448 144243 300303 629826 1237776 2434281 4394747 7803776 12670143 19788852 27377829 35346934 38934676 37627719 28051588 15799164 5555197 1163201 117359 6454 192 2"
        );
    }

    [TestMethod]
    public void Test_12_6_2()
    {
        Test(6, 2, 41,
             "1 6 10 30 50 150 249 720 1152 2880 4610 11544 18128 42684 65924 149514 228778 488796 728433 1467000 2136054 3978876 5574328 9510150 12699920 19077354 23527929 29862012 32688655 31474098 27976007 18457764 12016184 4687338 1939160 519318 141371 19092 3257 1038 200 36"
        );
    }

    [TestMethod]
    public void Test_12_6_2_Edge1()
    {
        Test(6, 2, 1, 42,
             "1 6 10 30 50 150 248 720 1154 2880 4596 11544 18127 42684 65937 149514 228556 488796 727609 1467000 2132910 3978876 5565680 9510150 12670471 19077354 23460308 29862012 32523759 31474098 27931033 18457764 12228170 4687338 2033300 519318 154388 19092 3866 1038 224 36 3"
        );
    }

    [TestMethod]
    public void Test_12_6_2_Edge2()
    {
        Test(6, 2, 2, 42,
             "1 6 10 30 50 150 248 720 1154 2880 4582 11544 18215 42684 66042 149514 227564 488796 728283 1467000 2117907 3978876 5550877 9510150 12565019 19077354 23347924 29862012 32515616 31474098 28055756 18457764 12343707 4687338 2050361 519318 152637 19092 4251 1038 190 36 6"
        );
    }

    [TestMethod]
    public void Test_14_7_2_Partial()
    {
        var info = new PuzzleInfo(7, 2, 0, /*multislide=*/ true);
        info.MaxSteps = 23;
        Test(info, 23,
             "1 7 12 42 72 252 431 1470 2430 7070 11728 34125 55550 153188 247010 659302 1055500 2672600 4206121 10228379 15906111 36437156 55403239 120204434"
        );
    }

    [TestMethod]
    public void Test_15_5_3_Partial()
    {
        var info = new PuzzleInfo(5, 3, 0, /*multislide=*/ true);
        info.MaxSteps = 20;
        Test(info, 20,
            "1 6 16 48 128 384 1023 3036 7796 22155 55915 155931 385558 1049703 2524357 6646590 15411981 38957481 86507301 207621178 436566702"
        );
    }

    [TestMethod]
    public void Test_16_4_4_Partial()
    {
        var info = new PuzzleInfo(4, 4, 0, /*multislide=*/ true);
        info.MaxSteps = 15;
        Test(info, 15,
            "1 6 18 54 162 486 1457 4334 12568 36046 102801 289534 808623 2231878 6076994 16288752"
        );
    }

    [TestMethod]
    public void Test_16_8_2_Partial()
    {
        var info = new PuzzleInfo(8, 2, 0, /*multislide=*/ true);
        info.MaxSteps = 15;
        Test(info, 15,
            "1 8 14 56 98 392 685 2688 4552 15120 25736 85280 142472 449216 746786 2286992"
        );
    }

}
