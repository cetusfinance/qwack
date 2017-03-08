using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Math.Matrix
{
    public interface IFastMatrix : IDisposable
    {
        double this[int row, int colum] { get; set; }
        unsafe double* Pointer { get; }
        int Rows { get; }
        int Columns { get; }
        int GetIndex(int row, int column);
    }
}
