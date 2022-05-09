using System;
using System.Linq;
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

            return ((2 * (n - 1) + 1) * x * LegendrePolynomial(x, n - 1) - (n - 1) * LegendrePolynomial(x, n - 2)) / n;
        }

        public static double[] LegendrePolynomialRoots(int n)
        {
            switch (n)
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
                case 6:
                    var x6a = 0.238619186083197;
                    var x6b = 0.661209386466265;
                    var x6c = 0.932469514203152;
                    return new[] { -x6a, -x6b, -x6c, x6a, x6b, x6c };
                case 7:
                    var x7a = 0.405845151377397;
                    var x7b = 0.741531185599394;
                    var x7c = 0.949107912342759;
                    return new[] { -x7a, -x7b, -x7c, 0.0, x7a, x7b, x7c };
                case 8:
                    var x8a = 0.183434642495650;
                    var x8b = 0.525532409916329;
                    var x8c = 0.796666477413627;
                    var x8d = 0.960289856497536;
                    return new[] { -x8a, -x8b, -x8c, -x8d, x8a, x8b, x8c, x8d };
                case 9:
                    var x9a = 0.324253423403809;
                    var x9b = 0.613371432700590;
                    var x9c = 0.836031107326636;
                    var x9d = 0.968160239507626;
                    return new[] { -x9a, -x9b, -x9c, -x9d, 0.0, x9a, x9b, x9c, x9d };
                case 10:
                    var x10a = 0.148874338981631;
                    var x10b = 0.433395394129247;
                    var x10c = 0.679409568299024;
                    var x10d = 0.865063366688985;
                    var x10e = 0.973906528517172;
                    return new[] { -x10a, -x10b, -x10c, -x10d, -x10e, x10a, x10b, x10c, x10d, x10e };
                case 11:
                    var x11a = 0.269543155952345;
                    var x11b = 0.519096129110681;
                    var x11c = 0.730152005574049;
                    var x11d = 0.887062599768095;
                    var x11e = 0.978228658146057;
                    return new[] { -x11a, -x11b, -x11c, -x11d, -x11e, 0.0, x11a, x11b, x11c, x11d, x11e };
                case 12:
                    var x12a = 0.125333408511469;
                    var x12b = 0.367831498918180;
                    var x12c = 0.587317954286617;
                    var x12d = 0.769902674194305;
                    var x12e = 0.904117256370475;
                    var x12f = 0.981560634246719;
                    return new[] { -x12a, -x12b, -x12c, -x12d, -x12e, -x12f, x12a, x12b, x12c, x12d, x12e, x12f };
                case 13:
                    var x13a = 0.230458315955135;
                    var x13b = 0.448492751036447;
                    var x13c = 0.642349339440340;
                    var x13d = 0.801578090733310;
                    var x13e = 0.917598399222978;
                    var x13f = 0.984183054718588;
                    return new[] { -x13a, -x13b, -x13c, -x13d, -x13e, -x13f, 0.0, x13a, x13b, x13c, x13d, x13e, x13f };
                case 14:
                    var x14a = 0.108054948707344;
                    var x14b = 0.319112368927890;
                    var x14c = 0.515248636358154;
                    var x14d = 0.687292904811685;
                    var x14e = 0.827201315069765;
                    var x14f = 0.928434883663574;
                    var x14g = 0.986283808696812;
                    return new[] { -x14a, -x14b, -x14c, -x14d, -x14e, -x14f, -x14g, x14a, x14b, x14c, x14d, x14e, x14f, x14g };
                case 15:
                    var x15a = 0.201194093997435;
                    var x15b = 0.394151347077563;
                    var x15c = 0.570972172608539;
                    var x15d = 0.724417731360170;
                    var x15e = 0.848206583410427;
                    var x15f = 0.937273392400706;
                    var x15g = 0.987992518020485;
                    return new[] { -x15a, -x15b, -x15c, -x15d, -x15e, -x15f, -x15g, 0.0, x15a, x15b, x15c, x15d, x15e, x15f, x15g };
                case 16:
                    var x16a = 0.095012509837637;
                    var x16b = 0.281603550779259;
                    var x16c = 0.458016777657227;
                    var x16d = 0.617876244402644;
                    var x16e = 0.755404408355003;
                    var x16f = 0.865631202387832;
                    var x16g = 0.944575023073233;
                    var x16h = 0.989400934991650;
                    return new[] { -x16a, -x16b, -x16c, -x16d, -x16e, -x16f, -x16g, -x16h, x16a, x16b, x16c, x16d, x16e, x16f, x16g, x16h };
                default:
                    throw new Exception("Only n<=16 supported");
            }
        }

        public static double GaussLegendre(Func<double, double> f, double a, double b, int nPoints)
        {
            var q1 = (b - a) / 2;
            var q2 = (a + b) / 2;

            var xi = LegendrePolynomialRoots(nPoints);
            var wi = xi.Select(x => 2.0 * (1 - x * x) / ((nPoints + 1) * (nPoints + 1) * LegendrePolynomial(x, nPoints + 1).IntPow(2))).ToArray();
            var iSum = xi.Select((x, ix) => wi[ix] * f(q1 * x + q2));

            return q1 * iSum.Sum();
        }

        public static double TwoDimensionalGaussLegendre(Func<double, double, double> fxy, double ax, double bx, double ay, double by, int nPoints)
        {
            var q1x = (bx - ax) / 2;
            var q2x = (ax + bx) / 2;
            var q1y = (by - ay) / 2;
            var q2y = (ay + by) / 2;

            var xi = LegendrePolynomialRoots(nPoints);
            var wi = xi.Select(x => 2.0 * (1 - x * x) / ((nPoints + 1) * (nPoints + 1) * LegendrePolynomial(x, nPoints + 1).IntPow(2))).ToArray();

            var iSum = 0.0;
            for (var i = 0; i < wi.Length; i++)
            {
                for (var j = 0; j < wi.Length; j++)
                {
                    iSum += wi[i] * wi[j] * fxy(q1x * xi[i] + q2x, q1y * xi[j] + q2y);
                }
            }
            return q1x * q1y * iSum;
        }

        //http://mathfaculty.fullerton.edu/mathews/n2003/SimpsonsRule2DMod.html
        public static double TwoDimensionalSimpsons(Func<double, double, double> fxy, double ax, double bx, double ay, double by, int nSteps)
        {
            if (nSteps % 2 != 0)
                throw new Exception("nSteps must be even");

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
                    iSum += 16 * fxy(xs(2 * i - 1), ys(2 * j - 1));

                    if (i < nSteps)
                    {
                        iSum += 8 * fxy(xs(2 * i), ys(2 * j - 1));
                    }
                }

                for (var j = 1; j < nSteps; j++)
                {
                    iSum += 8 * fxy(xs(2 * i - 1), ys(2 * j));

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
