using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Qwack.Math.Interpolation
{
    public class BilinearInterpolator : IInterpolator2D
    {
        private readonly double[] _x;
        private readonly double[] _y;
        private readonly double[,] _z;

        public BilinearInterpolator()
        {

        }

        public BilinearInterpolator(double[] x, double[] y, double[,] z)
        {
            _x = x;
            _y = y;
            _z = z;
        }
    
        private bool OutOfRange(double x, double y)
        {
            return x > _x.Last() || x < _x.First() || y > _y.Last() || y < _y.First();
        }

        public double Interpolate(double x, double y)
        {
            if (OutOfRange(x, y))
                return double.NaN;

            var iCol = Array.BinarySearch(_x, x);
            if (iCol < 0) iCol = ~iCol;
            var iRow = Array.BinarySearch(_y, y);
            if (iRow < 0) iRow = ~iRow;

            var C1 = _z[iRow - 1, iCol - 1];
            var C2 = _z[iRow - 1, iCol];
            var C3 = _z[iRow, iCol - 1];
            var C4 = _z[iRow, iCol];

            var Dx = (x - _x[iCol - 1]) / (_x[iCol] - _x[iCol - 1]);
            var Dy = (y - _y[iRow - 1]) / (_y[iRow] - _y[iRow - 1]);

            var C5 = C1 + Dx * (C2 - C1);
            var C6 = C3 + Dx * (C4 - C3);

            var interpVal = C5 + Dy * (C6 - C5);

            return interpVal;
        }
    }
}
