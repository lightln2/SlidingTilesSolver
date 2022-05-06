﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Util
{
    public static long Factorial(int n)
    {
        long x = 1;
        for (int i = 2; i <= n; i++)
        {
            x *= i;
        }
        return x;
    }

    public static FileStream OpenFile(string path)
    {
        return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
    }
}
