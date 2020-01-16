using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qwack.Math.Interpolation
{
    public static class SegmentedLinearRegression
    {
        public static IInterpolator1D Regress(double[] Xs, double[] Ys, int nSegments)
        {
            var nSamples = Xs.Length;
            var x = new double[nSegments + 1];
            var y = new double[nSegments + 1];
            var samplesPerSegment = nSamples / nSegments;
            for (var i = 0; i < nSegments; i++)
            {
                var sampleXs = Xs.Skip(i * samplesPerSegment).Take(samplesPerSegment).ToArray();
                var sampleYs = Ys.Skip(i * samplesPerSegment).Take(samplesPerSegment).ToArray();
                var lr = LinearRegression.LinearRegressionVector(sampleXs, sampleYs);
                var xLo = sampleXs.First();
                var xHi = sampleXs.Last();
                var yLo = lr.Alpha + lr.Beta * xLo;
                var yHi = lr.Alpha + lr.Beta * xHi;

                if (i == 0)
                {
                    x[0] = xLo;
                    y[0] = yLo;
                    x[1] = xHi;
                    y[1] = yHi;
                }
                else
                {
                    var xD = sampleXs.Select(q => q - xLo).ToArray();
                    var yD = sampleYs.Select(q => q - yLo).ToArray();
                    var lr2 = LinearRegression.LinearRegressionNoIntercept(xD, yD);
                    var yHiBetter = xD.Last() * lr2.Beta + yLo;
                    x[i + 1] = xHi;
                    y[i + 1] = yHiBetter;
                }
            }
            return InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.Linear);
        }

        public static IInterpolator1D RegressNotContinuous(double[] Xs, double[] Ys, int nSegments)
        {
            var nSamples = Xs.Length;
            //var x = new double[nSegments + 1];
            //var y = new double[nSegments + 1];
            var samplesPerSegment = nSamples / nSegments;
            var interps = new IInterpolator1D[nSegments];
            var uBounds = new double[nSegments];

            for (var i = 0; i < nSegments; i++)
            {
                var sampleXs = Xs.Skip(i * samplesPerSegment).Take(samplesPerSegment).ToArray();
                var sampleYs = Ys.Skip(i * samplesPerSegment).Take(samplesPerSegment).ToArray();
                var lr = LinearRegression.LinearRegressionVector(sampleXs, sampleYs);
                var xLo = sampleXs.First();
                var xHi = sampleXs.Last();
                var yLo = lr.Alpha + lr.Beta * xLo;
                var yHi = lr.Alpha + lr.Beta * xHi;

                interps[i] = InterpolatorFactory.GetInterpolator(new[] { xLo, xHi }, new[] { yLo, yHi }, Interpolator1DType.Linear);
                uBounds[i] = xHi;
            }
            uBounds[uBounds.Length-1] = double.MaxValue;
            return new NonContinuousInterpolator(uBounds, interps);
        }
    }
}
