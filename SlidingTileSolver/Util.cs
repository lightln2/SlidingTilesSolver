using System;
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

    public static void Swap<T>(ref T x, ref T y)
    {
        T temp = x;
        x = y;
        y = temp;
    }

    public static string GB(long size)
    {
        return (size / 1024.0 / 1024.0 / 1024.0).ToString("N1");
    }
}
