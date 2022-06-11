using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

public interface IMultislideUpDownCollector
{
    void Collect(int segment, uint[] vals, int len);
    void Close();
}
