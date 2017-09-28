using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Qwack.Math
{
    public static class LinearRegression
    {
        public static LinearRegressionResult LinearRegressionNoVector(double[] X0, double[] Y0, bool computeError)
        {
            if (X0.Length != Y0.Length) throw new ArgumentOutOfRangeException(nameof(X0), "X and Y must be the same length");

            var beta = Statistics.Covariance(X0, Y0) / Statistics.Variance(X0);

            var Ybar = Y0.Average();
            var Xbar = X0.Average();
            var alpha = Ybar - beta * Xbar;

            var sX = 0.0;
            var sY = 0.0;
            for (var i = 0; i < Y0.Length; i++)
            {
                sY += System.Math.Pow(Y0[i] - Ybar, 2.0);
                sX += System.Math.Pow(X0[i] - Xbar, 2.0);
            }
            var R2 = beta * System.Math.Sqrt(sX / sY);

            if (computeError)
            {
                double SSE = 0;
                for (var i = 0; i < Y0.Length; i++)
                {
                    var e2 = Y0[i] - (alpha + beta * X0[i]);
                    e2 *= e2;
                    SSE += e2;
                }
                return new LinearRegressionResult(alpha, beta, R2, SSE);
            }
            return new LinearRegressionResult(alpha, beta, R2);
        }

        public static LinearRegressionResult LinearRegressionVector(this double[] X0, double[] Y0)
        {
            if (X0.Length != Y0.Length) throw new ArgumentOutOfRangeException(nameof(X0), "X and Y must be the same length");

            var simdLength = Vector<double>.Count;
            var overFlow = X0.Length % simdLength;
            Utils.SimdHelpers.PadArrayForSIMD(X0, Y0, out double[] X, out double[] Y, overFlow, 0.0, 0.0);

            var vSumX = new Vector<double>(0);
            var vSumY = new Vector<double>(0);
            for (var i = 0; i < X.Length; i += simdLength)
            {
                var vaX = new Vector<double>(X, i);
                vSumX = Vector.Add(vaX, vSumX);
                var vaY = new Vector<double>(Y, i);
                vSumY = Vector.Add(vaY, vSumY);
            }

            double xAvg = 0, yAvg = 0;
            for (var i = 0; i < simdLength; ++i)
            {
                xAvg += vSumX[i];
                yAvg += vSumY[i];
            }

            xAvg /= X0.Length;
            yAvg /= X0.Length;

            Utils.SimdHelpers.PadArrayForSIMDnoAlloc(X0, Y0, X, Y, overFlow, xAvg, yAvg);
            
            var vMeanX = new Vector<double>(xAvg);
            var vMeanY = new Vector<double>(yAvg);
            vSumX = new Vector<double>(0);
            vSumY = new Vector<double>(0);
            var vSumC = new Vector<double>(0);

            for (var i = 0; i < X.Length; i += simdLength)
            {
                var vaX = new Vector<double>(X, i);
                var vaMX = Vector.Subtract(vaX, vMeanX);
                var va2X = Vector.Multiply(vaMX, vaMX);
                vSumX = Vector.Add(va2X, vSumX);

                var vaY = new Vector<double>(Y, i);
                var vaMY = Vector.Subtract(vaY, vMeanY);
                var va2Y = Vector.Multiply(vaMY, vaMY);
                vSumY = Vector.Add(va2Y, vSumY);

                var va2C = Vector.Multiply(vaMX, vaMY);
                vSumC = Vector.Add(va2C, vSumC);
            }

            double c = 0, vX = 0, vY = 0;
            for (var i = 0; i < simdLength; ++i)
            {
                c += vSumC[i];
                vX += vSumX[i];
                vY += vSumY[i];
            }

            var n = (double)X0.Length;
            var beta = c / vX;
            var alpha = yAvg - beta * xAvg;
            var R2 = beta * System.Math.Sqrt(vX / vY);

            return new LinearRegressionResult(alpha, beta, R2);
        }

        public struct LinearRegressionResult
        {
            private readonly double _alpha;
            private readonly double _beta;
            private readonly double _r2;
            private readonly double _sse;

            public LinearRegressionResult(double Alpha, double Beta, double R2, double SSE = double.NaN)
            {
                _alpha = Alpha;
                _beta = Beta;
                _r2 = R2;
                _sse = SSE;
            }

            public double Alpha => _alpha;
            public double Beta => _beta;
            public double R2 => _r2;
            public double SSE => _sse;
        }
    }
}
