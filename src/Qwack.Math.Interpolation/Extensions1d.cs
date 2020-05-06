using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Qwack.Math;
using Qwack.Transport.TransportObjects.Interpolators;
using System.Runtime.CompilerServices;
using Qwack.Transport.BasicTypes;

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

        public static TO_Interpolator1d ToTransportObject(this IInterpolator1D interp)
        {
            switch (interp)
            {
                case LinearInterpolatorFlatExtrap i1:
                    return new TO_Interpolator1d
                    {
                        Xs = i1.Xs,
                        Ys = i1.Ys,
                        Type = Interpolator1DType.LinearFlatExtrap
                    };
                case LinearInterpolator i2:
                    return new TO_Interpolator1d
                    {
                        Xs = i2.Xs,
                        Ys = i2.Ys,
                        Type = Interpolator1DType.Linear
                    };
                case LinearInVarianceInterpolator i3:
                    return new TO_Interpolator1d
                    {
                        Xs = i3.Xs,
                        Ys = i3.Ys,
                        Type = Interpolator1DType.LinearInVariance
                    };
                case GaussianKernelInterpolator i4:
                    return new TO_Interpolator1d
                    {
                        Xs = i4.Xs,
                        Ys = i4.Ys,
                        Type = Interpolator1DType.GaussianKernel
                    };
                case NextInterpolator i5:
                    return new TO_Interpolator1d
                    {
                        Xs = i5.Xs,
                        Ys = i5.Ys,
                        Type = Interpolator1DType.NextValue
                    };
                case PreviousInterpolator i6:
                    return new TO_Interpolator1d
                    {
                        Xs = i6.Xs,
                        Ys = i6.Ys,
                        Type = Interpolator1DType.PreviousValue
                    };
                case CubicHermiteSplineInterpolator i7:
                    return new TO_Interpolator1d
                    {
                        Xs = i7.Xs,
                        Ys = i7.Ys,
                        Type = Interpolator1DType.CubicSpline
                    };
                case DummyPointInterpolator i8:
                    return new TO_Interpolator1d
                    {
                        Xs = new[] { i8.Point },
                        Ys = new[] { i8.Point },
                        Type = Interpolator1DType.DummyPoint
                    };
                case ConstantHazzardInterpolator i9:
                    return new TO_Interpolator1d
                    {
                        Xs = new[] { i9.H },
                        Ys = new[] { i9.H },
                        Type = Interpolator1DType.ConstantHazzard
                    };
                default:
                    throw new InvalidOperationException($"We don't have a way of serializing a {interp.GetType().Name} interpolator");
            }
        }
    }
}
