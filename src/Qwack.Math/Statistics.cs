using Qwack.Math.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Math;

namespace Qwack.Math
{
    /// <summary>
    /// A collection of statistical utility functions
    /// </summary>
    public static class Statistics
    {
        public static double CumulativeNormalDistribution(double d) => FiNormSDist(d);
        public static double ProbabilityDensityFunction(double z) => StandardNormalDistribution(z);
        public static double StandardNormalDistribution(double x) => Exp(-x * x / 2) / DoubleExtensions.Sqrt2Pi;
        public static double NormalDistribution(double x, double mu, double sigma) => Exp(-((x - mu) / 2.0 * sigma).IntPow(2)) / DoubleExtensions.Sqrt2Pi / sigma / sigma;

        public static double NormInv(double p) => NormInv(p, 0.0, 1.0);

        public static double NormInv(double p, double mu, double sigma)
        {
            if (p < 0 || p > 1)
            {
                throw new ArgumentOutOfRangeException("The probality p must be bigger than 0 and smaller than 1");
            }
            if (sigma < 0)
            {
                throw new ArgumentOutOfRangeException("The standard deviation sigma must be positive");
            }

            if (p == 0)
            {
                return double.NegativeInfinity;
            }
            if (p == 1)
            {
                return double.PositiveInfinity;
            }
            if (sigma == 0)
            {
                return mu;
            }

            double q, r, val;

            q = p - 0.5;

            /*-- use AS 241 --- */
            /* double ppnd16_(double *p, long *ifault)*/
            /*      ALGORITHM AS241  APPL. STATIST. (1988) VOL. 37, NO. 3

                    Produces the normal deviate Z corresponding to a given lower
                    tail area of P; Z is accurate to about 1 part in 10**16.
            */
            if (Abs(q) <= .425)
            {/* 0.075 <= p <= 0.925 */
                r = .180625 - q * q;
                val =
                       q * (((((((r * 2509.0809287301226727 +
                                  33430.575583588128105) * r + 67265.770927008700853) * r +
                                45921.953931549871457) * r + 13731.693765509461125) * r +
                              1971.5909503065514427) * r + 133.14166789178437745) * r +
                            3.387132872796366608)
                       / (((((((r * 5226.495278852854561 +
                                28729.085735721942674) * r + 39307.89580009271061) * r +
                              21213.794301586595867) * r + 5394.1960214247511077) * r +
                            687.1870074920579083) * r + 42.313330701600911252) * r + 1);
            }
            else
            { /* closer than 0.075 from {0,1} boundary */

                /* r = min(p, 1-p) < 0.075 */
                if (q > 0)
                    r = 1 - p;
                else
                    r = p;

                r = Sqrt(-Log(r));
                /* r = sqrt(-log(r))  <==>  min(p, 1-p) = exp( - r^2 ) */

                if (r <= 5)
                { /* <==> min(p,1-p) >= exp(-25) ~= 1.3888e-11 */
                    r += -1.6;
                    val = (((((((r * 7.7454501427834140764e-4 +
                               .0227238449892691845833) * r + .24178072517745061177) *
                             r + 1.27045825245236838258) * r +
                            3.64784832476320460504) * r + 5.7694972214606914055) *
                          r + 4.6303378461565452959) * r +
                         1.42343711074968357734)
                        / (((((((r *
                                 1.05075007164441684324e-9 + 5.475938084995344946e-4) *
                                r + .0151986665636164571966) * r +
                               .14810397642748007459) * r + .68976733498510000455) *
                             r + 1.6763848301838038494) * r +
                            2.05319162663775882187) * r + 1);
                }
                else
                { /* very close to  0 or 1 */
                    r += -5;
                    val = (((((((r * 2.01033439929228813265e-7 +
                               2.71155556874348757815e-5) * r +
                              .0012426609473880784386) * r + .026532189526576123093) *
                            r + .29656057182850489123) * r +
                           1.7848265399172913358) * r + 5.4637849111641143699) *
                         r + 6.6579046435011037772)
                        / (((((((r *
                                 2.04426310338993978564e-15 + 1.4215117583164458887e-7) *
                                r + 1.8463183175100546818e-5) * r +
                               7.868691311456132591e-4) * r + .0148753612908506148525)
                             * r + .13692988092273580531) * r +
                            .59983220655588793769) * r + 1);
                }

                if (q < 0.0)
                {
                    val = -val;
                }
            }

            return mu + sigma * val;
        }

        /// <summary>
        /// Returns sample variance of an array of values
        /// </summary>
        /// <param name="x">Array of samples</param>
        /// <returns></returns>
        public static double Variance(this IEnumerable<double> x)
        {
            var xAvg = x.Average();
            var n = x.Count();
            var v = 0.0;

            foreach(var xi in x)
            {
                var v1 = xi - xAvg;
                v += (v1 * v1);
            }
            return v / (n - 1);
        }

        /// <summary>
        /// Returns sample skewness of an array of values
        /// </summary>
        /// <param name="x">Array of samples</param>
        /// <returns></returns>
        public static double Skewness(this IEnumerable<double> x)
        {
            var xAvg = x.Average();

            var n = x.Count();
            double m2 = 0;
            double m3 = 0;

            foreach (var xi in x)
            {
                var v1 = xi - xAvg;
                var v2 = v1 * v1;
                m2 += v2;
                m3 += v2 * v1;
            }
            m2 /= n;
            m3 /= n;
            return m3 / Pow(m2, 3 / 2);
        }

        /// <summary>
        /// Returs the moment of Nth order of an array of values
        /// Order 2==Standard Deviation, 3==Skewness, 4==Kurtosis etc.
        /// </summary>
        /// <param name="x">Array of samples</param>
        /// <param name="order">Order of moment</param>
        /// <returns></returns>
        public static double StandardizedMoment(double[] x, int order)
        {
            var xAvg = x.Average();

            var n = x.Length;
            var m2 = 0.0;
            var mO = 0.0;

            for (var i = 0; i < n; i++)
            {
                var v1 = x[i] - xAvg;
                var v2 = v1 * v1;
                m2 += v2;
                mO += Pow(v1, order);
            }
            m2 /= n;
            mO /= n;
            return mO / Pow(m2, order / 2);
        }

        /// <summary>
        /// Compute the sample variance of an array of values with the average already computed
        /// </summary>
        /// <param name="x">Array of samples</param>
        /// <param name="average">The average of the array x</param>
        /// <returns></returns>
        public static double VarianceWithAverage(double[] x, double average)
        {
            var xAvg = average;
            var n = x.Length;
            var v = 0.0;

            for (var i = 0; i < n; i++)
            {
                var v1 = x[i] - xAvg;
                v += (v1 * v1);
            }
            return v / (n - 1);
        }

        public static double StdDev(this IEnumerable<double> x) => Sqrt(Variance(x));
        public static double StdDevWithAverage(this double[] x, double average) => Sqrt(VarianceWithAverage(x, average));

        public static double NormSDist(double d) => FiNormSDist(d);

        public static double FiNormSDist(double z)
        {
            
            var RTWO = 1.4142135623731;

            var a0 = 0.6101430819232;
            var a1 = -0.434841272712578;
            var a2 = 0.176351193643605;
            var a3 = -6.07107956092494E-02;
            var a4 = 1.77120689956941E-02;
            var a5 = -4.32111938556729E-03;
            var a6 = 8.54216676887099E-04;
            var a7 = -1.27155090609163E-04;
            var a8 = 1.12481672436712E-05;
            var a9 = 3.13063885421821E-07;
            var a10 = -2.70988068537762E-07;
            var a11 = 3.07376227014077E-08;
            var a12 = 2.51562038481762E-09;
            var a13 = -1.02892992132032E-09;
            var a14 = 2.99440521199499E-11;
            var a15 = 2.60517896872669E-11;
            var a16 = -2.63483992417197E-12;
            var a17 = -6.43404509890636E-13;
            var a18 = 1.12457401801663E-13;
            var a19 = 1.72815333899861E-14;
            var a20 = -4.26410169494238E-15;
            var a21 = -5.45371977880191E-16;
            var a22 = 1.58697607761671E-16;
            var a23 = 2.0899837844334E-17;
            var a24 = -5.900526869409E-18;

            var xa = Abs(z) / RTWO;
            var p = 0.0;
            if (xa <= 100.0)
            { 
                var BP = 0.0;
                var T = (8.0 * xa - 30.0) / (4 * xa + 15.0);
                var bm = 0.0;
                var b = 0.0;

                BP = b;
                b = bm;
                bm = T * b - BP + a24;
                BP = b;
                b = bm;
                bm = T * b - BP + a23;
                BP = b;
                b = bm;
                bm = T * b - BP + a22;
                BP = b;
                b = bm;
                bm = T * b - BP + a21;
                BP = b;
                b = bm;
                bm = T * b - BP + a20;
                BP = b;
                b = bm;
                bm = T * b - BP + a19;
                BP = b;
                b = bm;
                bm = T * b - BP + a18;
                BP = b;
                b = bm;
                bm = T * b - BP + a17;
                BP = b;
                b = bm;
                bm = T * b - BP + a16;
                BP = b;
                b = bm;
                bm = T * b - BP + a15;
                BP = b;
                b = bm;
                bm = T * b - BP + a14;
                BP = b;
                b = bm;
                bm = T * b - BP + a13;
                BP = b;
                b = bm;
                bm = T * b - BP + a12;
                BP = b;
                b = bm;
                bm = T * b - BP + a11;
                BP = b;
                b = bm;
                bm = T * b - BP + a10;
                BP = b;
                b = bm;
                bm = T * b - BP + a9;
                BP = b;
                b = bm;
                bm = T * b - BP + a8;
                BP = b;
                b = bm;
                bm = T * b - BP + a7;
                BP = b;
                b = bm;
                bm = T * b - BP + a6;
                BP = b;
                b = bm;
                bm = T * b - BP + a5;
                BP = b;
                b = bm;
                bm = T * b - BP + a4;
                BP = b;
                b = bm;
                bm = T * b - BP + a3;
                BP = b;
                b = bm;
                bm = T * b - BP + a2;
                BP = b;
                b = bm;
                bm = T * b - BP + a1;
                BP = b;
                b = bm;
                bm = T * b - BP + a0;

                p = Exp(-xa * xa) * (bm - BP) / 4.0;
            }


            if (z > 0.0) { p = 1.0 - p; }

            return p;
        }

        public static double Erf(double z)
        {
            var erfValue = z;
            var currentCoefficient = 1.0;
            double X;

            if (z < 5)
            {
                var termCount = 50 * (int)Ceiling(Abs(z));
                for (var n = 1; n < termCount; n++)
                {
                    currentCoefficient *= -1.0 * (2.0 * (double)n - 1.0) / ((double)n * (2.0 * (double)n + 1.0));
                    X = currentCoefficient * Pow(z, (2 * n + 1));
                    if (double.IsNaN(X)) { X = 0; }
                    erfValue += X;
                }

                return erfValue * (2.0 / Sqrt(PI));
            }
            else
            {
                return 1.0;
            }
        }

        public static double Phi(double x) => (Exp((-x * x) / 2.0) / Sqrt(6.28318530717958));

        public static double AcklamInvCND(double P)
        {
            const double a1 = -39.6968302866538;
            const double a2 = 220.946098424521;
            const double a3 = -275.928510446969;
            const double a4 = 138.357751867269;
            const double a5 = -30.6647980661472;
            const double a6 = 2.50662827745924;
            const double b1 = -54.4760987982241;
            const double b2 = 161.585836858041;
            const double b3 = -155.698979859887;
            const double b4 = 66.8013118877197;
            const double b5 = -13.2806815528857;
            const double c1 = -7.78489400243029E-03;
            const double c2 = -0.322396458041136;
            const double c3 = -2.40075827716184;
            const double c4 = -2.54973253934373;
            const double c5 = 4.37466414146497;
            const double c6 = 2.93816398269878;
            const double d1 = 7.78469570904146E-03;
            const double d2 = 0.32246712907004;
            const double d3 = 2.445134137143;
            const double d4 = 3.75440866190742;
            const double low = 0.02425;
            const double high = 1.0 - low;
            double z, R;

            if (P <= 0 || P >= 1.0f)
                return (double)0x7FFFFFFF;

            if (P < low)
            {
                z = Sqrt(-2.0 * Log(P));
                z = (((((c1 * z + c2) * z + c3) * z + c4) * z + c5) * z + c6) /
                    ((((d1 * z + d2) * z + d3) * z + d4) * z + 1.0);
            }
            else
            {
                if (P > high)
                {
                    z = Sqrt(-2.0 * Log(1.0 - P));
                    z = -(((((c1 * z + c2) * z + c3) * z + c4) * z + c5) * z + c6) /
                         ((((d1 * z + d2) * z + d3) * z + d4) * z + 1.0);
                }
                else
                {
                    z = P - 0.5;
                    R = z * z;
                    z = (((((a1 * R + a2) * R + a3) * R + a4) * R + a5) * R + a6) * z /
                        (((((b1 * R + b2) * R + b3) * R + b4) * R + b5) * R + 1.0);
                }
            }

            return z;
        }

        /// <summary>
        /// Double-precision normal CDF a'la West's paper
        /// http://www.codeplanet.eu/files/download/accuratecumnorm.pdf
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double CumnormWest(double x)
        {
            var cumNorm = 0.0;

            var XAbs = Abs(x);
            if (XAbs > 37)
                cumNorm = 0.0;
            else
            {
                var Exponential = Exp(-XAbs * XAbs / 2.0);
                if (XAbs < 7.07106781186547)
                {
                    var build = 3.52624965998911E-02 * XAbs + 0.700383064443688;
                    build = build * XAbs + 6.37396220353165;
                    build = build * XAbs + 33.912866078383;
                    build = build * XAbs + 112.079291497871;
                    build = build * XAbs + 221.213596169931;
                    build = build * XAbs + 220.206867912376;
                    cumNorm = Exponential * build;
                    build = 8.83883476483184E-02 * XAbs + 1.75566716318264;
                    build = build * XAbs + 16.064177579207;
                    build = build * XAbs + 86.7807322029461;
                    build = build * XAbs + 296.564248779674;
                    build = build * XAbs + 637.333633378831;
                    build = build * XAbs + 793.826512519948;
                    build = build * XAbs + 440.413735824752;
                    cumNorm /= build;
                }
                else
                {
                    var build = XAbs + 0.65;
                    build = XAbs + 4 / build;
                    build = XAbs + 3 / build;
                    build = XAbs + 2 / build;
                    build = XAbs + 1 / build;
                    cumNorm = Exponential / build / 2.506628274631;
                }
            }
            return x > 0 ? 1 - cumNorm : cumNorm;
        }

        public static double Median(this IEnumerable<double> X)
        {
            var n = X.Count();
            if (n % 2 == 0)
            {
                var IXlo = n / 2 - 1;
                var IXhi = IXlo + 1;
                return 0.5 * (X.OrderBy(x => x).ElementAt(IXlo) + X.OrderBy(x => x).ElementAt(IXhi));
            }
            else
            {
                var IX = (n + 1) / 2 - 1;
                return X.OrderBy(x => x).ElementAt(IX);
            }
        }

        public static double Mode(this IEnumerable<double> X)
        {
            var D = X.Distinct();
            var maxN = int.MinValue;
            var maxNix = 0;
            var cIX = 0;

            var Ns = new List<int>();
            foreach (var d in D)
            {
                var nd = X.Count(y => y == d);
                Ns.Add(nd);

                maxN = Max(maxN, nd);
                if (maxN == nd)
                    maxNix = cIX;
                //put early exit condition here where maxN is more than remaining elements
                cIX++;
            }

            return D.ElementAt(maxNix);
        }


        public static (double Correlation, double Error) Correlation(this double[] x, double[] y)
        {
            var xAvg = x.Average();
            var yAvg = y.Average();
            var n = x.Length;

            if (n == y.Length)
            {
                double c = 0, vX = 0, vY = 0;
                for (var i = 0; i < n; i++)
                {
                    var v1 = x[i] - xAvg;
                    vX += (v1 * v1);
                    var v2 = y[i] - yAvg;
                    vY += (v2 * v2);
                    c += (v1 * v2);
                }

                var denom = Sqrt(vX * vY);
                if (denom != 0)
                {
                    var rho = c / denom;
                    var err = Sqrt((1.0 - rho * rho) * vY / (double)n);
                    return (rho, err);
                }
                else if ((vX == 0 && vY != 0) || (vX != 0 && vY == 0))
                    return (0, 0);
                else
                    return (1, 0);
            }
            else
            {
                return (double.NaN, double.NaN);
            }
        }

        public static double Covariance(this double[] x, double[] y)
        {
            var xAvg = x.Average();
            var yAvg = y.Average();
            var n = x.Length;

            if (n == y.Length)
            {
                double c = 0;
                for (var i = 0; i < n; i++)
                {
                    c += (x[i] - xAvg) * (y[i] - yAvg);
                }
                return c / n;
            }
            else
            {
                return 0;
            }
        }

        public static double[] Returns(this IEnumerable<double> x, bool logReturns)
        {
            var output = new double[x.Count() - 1];
            var count = 0;
            var lastPrice = x.First();
            foreach (var p in x)
            {
                if (count == 0)
                {
                    count++;
                    continue;
                }

                output[count - 1] = logReturns ?
                   Log(p / lastPrice) :
                   p / lastPrice - 1.0;

                lastPrice = p;
                count++;
            }

            return output;
        }

        public static double MaxDD(this IEnumerable<double> x)
        {
            var maxDD = 0.0;
            var peakPrice = double.MinValue;
            foreach(var p in x)
            {
                peakPrice = Max(peakPrice, p);
                maxDD = Min(maxDD, p / peakPrice - 1.0);
            }
            return maxDD;
        }

        public static decimal MaxDD(this IEnumerable<decimal> x)
        {
            var maxDD = 0.0M;
            var peakPrice = decimal.MinValue;
            foreach (var p in x)
            {
                peakPrice = Max(peakPrice, p);
                maxDD = Min(maxDD, p / peakPrice - 1.0M);
            }
            return maxDD;
        }

        public static double MaxDD(this IEnumerable<double> x, int indexSince)
        {
            var maxDD = 0.0;
            var peakPrice = double.MinValue;
            var ix = 0;
            foreach (var p in x)
            {
                peakPrice = Max(peakPrice, p);
                ix++;
                if (ix > indexSince)
                    maxDD = Min(maxDD, p / peakPrice - 1.0);
            }
            return maxDD;
        }

        public static decimal MaxDD(this IEnumerable<decimal> x, int indexSince)
        {
            var maxDD = 0.0M;
            var peakPrice = decimal.MinValue;
            var ix = 0;
            foreach (var p in x)
            {
                peakPrice = Max(peakPrice, p);
                ix++;
                if(ix>indexSince)
                    maxDD = Min(maxDD, p / peakPrice - 1.0M);
            }
            return maxDD;
        }

        public static double FisherTransform(double correl, double conf, double sampleSize, bool isBid)
        {
            var mean = 0.5 * Log((1 + correl) / (1 - correl));
            var sigma = Sqrt(1 / (sampleSize - 3));
            return isBid ? Tanh(NormInv((1 - conf) / 2, mean, sigma))
                : Tanh(NormInv(1 - (1 - conf) / 2, mean, sigma));
        }
    }
}
