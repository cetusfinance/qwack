using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Interpolators;

namespace Qwack.Math.Interpolation
{
    public static class Extensions1d
    {
        public static double Average(this IInterpolator1D interp, IEnumerable<double> Xs)
            => Xs.Average(x => interp.Interpolate(x));

        public static double MaxY(this IInterpolator1D interp, IEnumerable<double> Xs)
            => Xs.Max(x => interp.Interpolate(x));

        public static double MinY(this IInterpolator1D interp, IEnumerable<double> Xs)
            => Xs.Min(x => interp.Interpolate(x));

        public static double Sum(this IInterpolator1D interp, IEnumerable<double> Xs)
            => Xs.Sum(x => interp.Interpolate(x));

        public static double[] Many(this IInterpolator1D interp, IEnumerable<double> Xs)
            => Xs.Select(x => interp.Interpolate(x)).ToArray();

        public static TO_Interpolator1d ToTransportObject(this IInterpolator1D interp) => interp switch
        {
            LinearInterpolatorFlatExtrap i1 => new TO_Interpolator1d
            {
                Xs = i1.Xs,
                Ys = i1.Ys,
                Type = Interpolator1DType.LinearFlatExtrap
            },
            LinearInterpolator i2 => new TO_Interpolator1d
            {
                Xs = i2.Xs,
                Ys = i2.Ys,
                Type = Interpolator1DType.Linear
            },
            LinearInVarianceInterpolator i3 => new TO_Interpolator1d
            {
                Xs = i3.Xs,
                Ys = i3.Ys,
                Type = Interpolator1DType.LinearInVariance
            },
            GaussianKernelInterpolator i4 => new TO_Interpolator1d
            {
                Xs = i4.Xs,
                Ys = i4.Ys,
                Type = Interpolator1DType.GaussianKernel
            },
            NextInterpolator i5 => new TO_Interpolator1d
            {
                Xs = i5.Xs,
                Ys = i5.Ys,
                Type = Interpolator1DType.NextValue
            },
            PreviousInterpolator i6 => new TO_Interpolator1d
            {
                Xs = i6.Xs,
                Ys = i6.Ys,
                Type = Interpolator1DType.PreviousValue
            },
            CubicHermiteSplineInterpolator i7 => new TO_Interpolator1d
            {
                Xs = i7.Xs,
                Ys = i7.Ys,
                Type = Interpolator1DType.CubicSpline
            },
            DummyPointInterpolator i8 => new TO_Interpolator1d
            {
                Xs = new[] { i8.Point },
                Ys = new[] { i8.Point },
                Type = Interpolator1DType.DummyPoint
            },
            ConstantHazzardInterpolator i9 => new TO_Interpolator1d
            {
                Xs = new[] { i9.H },
                Ys = new[] { i9.H },
                Type = Interpolator1DType.ConstantHazzard
            },
            _ => throw new InvalidOperationException($"We don't have a way of serializing a {interp.GetType().Name} interpolator"),
        };
    }
}
