using System;
using System.Collections.Generic;
using System.Text;
using static Qwack.Math.Extensions.DoubleExtensions;
using static System.Math;

namespace Qwack.Math
{
    public static class BesselFunctions
    {
        private const double _marginalImpact = 1e-10;
        private const int _maxCount = 1000;

        public static double Jn(double x, int n)
        {
            var t = ((x / 2).IntPow(n)) / n.Factorial();
            var r = t;
            var m = 1;
            var count = 0;
            while (Abs(t) > _marginalImpact && count < _maxCount)
            {
                t = ((-1.0).IntPow(m) * (x / 2).IntPow(n + 2 * m)) / (n.Factorial() * (n + m).Factorial());
                m++;
                r += t;
                count++;
            }

            return r;
        }
    }
}
