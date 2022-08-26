using System;
using System.Linq;
using System.Numerics;
using Qwack.Transport.Results;
using static System.Math;
using static Qwack.Math.Matrix.DoubleArrayFunctions;

namespace Qwack.Math
{
    public static class LinearRegression
    {
        public static LinearRegressionResult LinearRegressionNoVector(this double[] X0, double[] Y0, bool computeError)
        {
            if (X0.Length != Y0.Length) throw new ArgumentOutOfRangeException(nameof(X0), "X and Y must be the same length");

            var Ybar = Y0.Average();
            var Xbar = X0.Average();

            var sX = 0.0;
            var sY = 0.0;
            var sXY = 0.0;
            for (var i = 0; i < Y0.Length; i++)
            {
                var x1 = X0[i] - Xbar;
                var y1 = Y0[i] - Ybar;
                sY += y1 * y1;
                sX += x1 * x1;
                sXY += x1 * y1;
            }

            var beta = sXY / sX;
            var alpha = Ybar - beta * Xbar;
            var R2 = beta * Sqrt(sX / sY);

            if (computeError)
            {
                var SSE = 0.0;
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
            var X = X0;
            var Y = Y0;

            var vSumX = new Vector<double>(0);
            var vSumY = new Vector<double>(0);
            for (var i = 0; i < X.Length - simdLength + 1; i += simdLength)
            {
                var vaX = new Vector<double>(X, i);
                vSumX = Vector.Add(vaX, vSumX);
                var vaY = new Vector<double>(Y, i);
                vSumY = Vector.Add(vaY, vSumY);
            }
            var xAvg = 0.0;
            var yAvg = 0.0;
            for (var i = X.Length - overFlow; i < X.Length; i++)
            {
                xAvg += X[i];
                yAvg += Y[i];
            }

            for (var i = 0; i < simdLength; ++i)
            {
                xAvg += vSumX[i];
                yAvg += vSumY[i];
            }

            xAvg /= X0.Length;
            yAvg /= Y0.Length;

            var vMeanX = new Vector<double>(xAvg);
            var vMeanY = new Vector<double>(yAvg);
            vSumX = new Vector<double>(0);
            vSumY = new Vector<double>(0);
            var vSumC = new Vector<double>(0);

            for (var i = 0; i < X.Length - simdLength + 1; i += simdLength)
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
            for (var i = X.Length - overFlow; i < X.Length; i++)
            {
                var x1 = (X[i] - xAvg);
                var y1 = (Y[i] - yAvg);
                vX += x1 * x1;
                vY += y1 * y1;
                c += x1 * y1;
            }

            for (var i = 0; i < simdLength; ++i)
            {
                c += vSumC[i];
                vX += vSumX[i];
                vY += vSumY[i];
            }

            var beta = c / vX;
            var alpha = yAvg - beta * xAvg;
            var R2 = beta * Sqrt(vX / vY);

            return new LinearRegressionResult(alpha, beta, R2);
        }

        public static LinearRegressionResult LinearRegressionNoIntercept(this double[] X0, double[] Y0)
        {
            if (X0.Length != Y0.Length) throw new ArgumentOutOfRangeException(nameof(X0), "X and Y must be the same length");

            var rowVec = RowVectorToMatrix(X0);
            var colVec = ColumnVectorToMatrix(X0);
            var xtx = MatrixProduct(rowVec, colVec);
            var inv = InvertMatrix(xtx);
            var a = MatrixProduct(inv, rowVec);
            var result = MatrixProduct(a, Y0);

            return new LinearRegressionResult(0, result[0], 0);
        }
       
    }
}
