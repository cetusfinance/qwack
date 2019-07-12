using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Qwack.Math.Extensions;
using static System.Math;

namespace Qwack.Math
{
    public static class Integration
    {
        public static double TrapezoidRule(Func<double, double> f, double a, double b, int nSteps)
        {
            //bounds check
            if (a >= b)
                return 0;

            var iSum = 0.0;

            var step = (b - a) / nSteps;

            double vA, vU;
            vA = f(a);
            while (a < b)
            {
                vU = f(a + step);
                iSum += 0.5 * (vU + vA) * (step);
                a += step;
                vA = vU;
            }
            return iSum;
        }

        //https://en.wikipedia.org/wiki/Simpson%27s_rule
        public static double SimpsonsRule(Func<double, double> f, double a, double b, int nSteps)
        {
            if (nSteps % 2 != 0)
                throw new Exception("nSteps must be even");

            //bounds check
            if (a >= b)
                return 0;

            var iSum = f(a);

            var step = (b - a) / nSteps;
            var x = a;
            for (var i = 1; i < nSteps; i++)
            {
                x += step;
                iSum += ((i % 2 == 0) ? 2.0 : 4.0) * f(x);
            }

            iSum += f(b);

            return iSum * step / 3.0;
        }

        public static double SimpsonsRuleExtended(Func<double, double> f, double a, double b, int nSteps)
        {
            if (nSteps % 2 != 0 || nSteps <= 8)
                throw new Exception("nSteps must be even and > 8");

            //bounds check
            if (a >= b)
                return 0;

            var h = (b - a) / nSteps;

            var iSum = 17 * f(a) + 59 * f(a + h) + 43 * f(a + h + h) + 49 * f(a + h + h + h);

            var x = a + h * 3;
            for (var i = 4; i <= nSteps - 4; i++)
            {
                x += h;
                iSum += 48 * f(x);
            }

            iSum += 49 * f(b - h - h - h) + 43 * f(b - h - h) + 59 * f(b - h) + 17 * f(b);

            return iSum * h / 48.0;
        }

        public static double LegendrePolynomial(double x, int n)
        {
            if (n <= 0) return 1.0;
            if (n == 1) return x;

            return ((2 * (n-1) + 1) * x * LegendrePolynomial(x, n - 1) - (n - 1) * LegendrePolynomial(x, n - 2)) / n;
        }

        public static double LegendrePolynomialDerivative(double x, int n)
        {
            switch (n)
            {
                case 0:
                    return 0.0;
                case 1:
                    return 1.0;
                case 2:
                    return 3.0 * x;
                case 3:
                    return 0.5 * (15 * x * x - 3);
                case 4:
                    return 0.125 * (140 * x * x * x - 60 * x);
                case 5:
                    return 0.125 * (315 * x * x * x * x - 210 * x * x + 15);
                default:
                    throw new Exception("Only n<=5 supported");
            }
        }

        public static double[] LegendrePolynomialRoots(int n)
        {
            switch(n)
            {
                case 1:
                    return new[] { 0.0 };
                case 2:
                    var r2 = Sqrt(1.0 / 3.0);
                    return new[] { -r2, r2 };
                case 3:
                    var r3 = Sqrt(3.0 / 5.0);
                    return new[] { -r3, 0.0, r3 };
                case 4:
                    var r4a = Sqrt(3.0 / 7.0 - 2.0 / 7.0 * Sqrt(6.0 / 5.0));
                    var r4b = Sqrt(3.0 / 7.0 + 2.0 / 7.0 * Sqrt(6.0 / 5.0));
                    return new[] { -r4b, -r4a, r4a, r4b };
                case 5:
                    var r5a = Sqrt(5.0 - 2.0 * Sqrt(10.0 / 7.0)) / 3.0;
                    var r5b = Sqrt(5.0 + 2.0 * Sqrt(10.0 / 7.0)) / 3.0;
                    return new[] { -r5b, -r5a, 0.0, r5a, r5b };
                default:
                    throw new Exception("Only n<=5 supported");
            }
        }

        public static double GaussLegendre(Func<double, double> f, double a, double b, int nPoints)
        {
            var q1 = (b - a) / 2;
            var q2 = (a + b) / 2;

            var xi = LegendrePolynomialRoots(nPoints);
            //var wi = xi.Select(x => 2.0 / ((1.0 - x * x) * (LegendrePolynomialDerivative(x, nPoints)).IntPow(2))).ToArray();
            var wi = xi.Select(x => 2.0 * (1 - x * x) / ((nPoints + 1) * (nPoints + 1) * LegendrePolynomial(x, nPoints + 1).IntPow(2))).ToArray();
            var iSum = xi.Select((x, ix) => wi[ix] * f(q1*x+q2));

            return q1*iSum.Sum();
        }

        //http://mathfaculty.fullerton.edu/mathews/n2003/SimpsonsRule2DMod.html
        public static double TwoDimensionalSimpsons(Func<double, double, double> fxy, double ax, double bx, double ay, double by, int nSteps)
        {
            if (nSteps % 2 != 0 || nSteps <= 8)
                throw new Exception("nSteps must be even and > 8");

            //bounds check
            if (ax >= bx || ay >= by)
                return 0;

            var hx = (bx - ax) / nSteps / 2.0;
            var hy = (by - ay) / nSteps / 2.0;

            var ys = new Func<double, double>(ix => ay + ix * hy);
            var xs = new Func<double, double>(ix => ax + ix * hx);
            
            var iSum = fxy(ax, ay) + fxy(bx, ay) + fxy(ax, by) + fxy(bx, by);

            for (var i = 1; i <= nSteps; i++)
            {
                iSum += 4 * fxy(ax, ys(2 * i - 1));
                iSum += 4 * fxy(bx, ys(2 * i - 1));
                iSum += 4 * fxy(xs(2 * i - 1), ay);
                iSum += 4 * fxy(xs(2 * i - 1), by);
                if (i < nSteps)
                {
                    iSum += 2 * fxy(ax, ys(2 * i));
                    iSum += 2 * fxy(bx, ys(2 * i));
                    iSum += 2 * fxy(xs(2 * i), ay);
                    iSum += 2 * fxy(xs(2 * i), by);
                }

                for (var j = 1; j <= nSteps; j++)
                {
                    iSum += 16 * fxy(xs(2 * i-1), ys(2 * j - 1));

                    if (i < nSteps)
                    {
                        iSum += 8 * fxy(xs(2 * i), ys(2 * j - 1));
                    }
                }

                for (var j = 1; j < nSteps; j++)
                {
                    iSum += 8 * fxy(xs(2 * i-1), ys(2 * j));

                    if (i < nSteps)
                    {
                        iSum += 4 * fxy(xs(2 * i), ys(2 * j));
                    }
                }
            } 

            iSum *= hx * hy / 9.0;

            return iSum;
        }

        public static double TwoDimensionalTrapezoid(Func<double, double, double> fxy, double ax, double bx, double ay, double by, int nSteps)
        {
            //bounds check
            if (ax >= bx || ay >= by)
                return 0;

            var hx = (bx - ax) / nSteps;
            var hy = (by - ay) / nSteps;

            var ys = Enumerable.Range(0, nSteps + 1)
                .Select(ix => ay + ix * hy)
                .ToArray();
            var xs = Enumerable.Range(0, nSteps + 1)
                .Select(ix => ax + ix * hx)
                .ToArray();

            var iSum = fxy(ax, ay) + fxy(bx, ay) + fxy(ax, by) + fxy(bx, by);

            for (var i = 1; i < nSteps; i++)
            {
                iSum += 2 * fxy(xs[i], ay);
                iSum += 2 * fxy(xs[i], by);
                iSum += 2 * fxy(ax, ys[i]);
                iSum += 2 * fxy(bx, ys[i]);
                for (var j = 1; j < nSteps; j++)
                {
                    iSum += 4 * fxy(xs[i], ys[j]);
                }
            }

            iSum *= hx * hy / 4.0;

            return iSum;
        }
    }
}
