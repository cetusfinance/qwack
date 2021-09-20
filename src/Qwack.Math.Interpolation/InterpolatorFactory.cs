using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Interpolators;

namespace Qwack.Math.Interpolation
{
    public class InterpolatorFactory
    {
        public static IInterpolator1D GetInterpolator(double[] x, double[] y, Interpolator1DType kind, bool noCopy = false, bool isSorted = false)
        {
            if (!noCopy)
            {
                var newx = new double[x.Length];
                var newy = new double[y.Length];
                Buffer.BlockCopy(x, 0, newx, 0, x.Length * 8);
                Buffer.BlockCopy(y, 0, newy, 0, y.Length * 8);
                x = newx;
                y = newy;
            }
            if (!isSorted)
            {
                Array.Sort(x, y);
            }
            switch (kind)
            {
                case Interpolator1DType.LinearFlatExtrap:
                    if(x.Length < 50)
                    { 
                        return new LinearInterpolatorFlatExtrapNoBinSearch(x, y);
                    }
                    else
                    {
                        return new LinearInterpolatorFlatExtrap(x, y);
                    }
                case Interpolator1DType.Linear:
                    return new LinearInterpolator(x, y);
                case Interpolator1DType.LinearInVariance:
                    return new LinearInVarianceInterpolator(x,y);
                case Interpolator1DType.GaussianKernel:
                    return new GaussianKernelInterpolator(x, y);
                case Interpolator1DType.NextValue:
                    return new NextInterpolator(x, y);
                case Interpolator1DType.PreviousValue:
                    return new PreviousInterpolator(x, y);
                case Interpolator1DType.CubicSpline:
                    return new CubicHermiteSplineInterpolator(x, y);
                case Interpolator1DType.MonotoneCubicSpline:
                    return new CubicHermiteSplineInterpolator(x, y, true);
                case Interpolator1DType.DummyPoint:
                    return new DummyPointInterpolator(y.First());
                default:
                    throw new InvalidOperationException($"We don't have a way of making a {kind} interpolator");
            }
        }

        public static IInterpolator1D GetInterpolator(TO_Interpolator1d transportObject) => 
            GetInterpolator(transportObject.Xs, transportObject.Ys, transportObject.Type, transportObject.NoCopy, transportObject.IsSorted);


        public static IInterpolator2D GetInterpolator(double[] x, double[] y, double[,] z, Interpolator2DType kind) => kind switch
        {
            Interpolator2DType.Bilinear => new Generic2dInterpolator(x, y, z, Interpolator1DType.Linear, Interpolator1DType.Linear),
            Interpolator2DType.BiCubic => new Generic2dInterpolator(x, y, z, Interpolator1DType.CubicSpline, Interpolator1DType.CubicSpline),
            Interpolator2DType.DummyPoint => new DummyPointInterpolator(z[0, 0]),
            _ => throw new InvalidOperationException($"We don't have a way of making a {kind} interpolator"),
        };

        public static IInterpolator2D GetInterpolator(TO_Interpolator2d_Square transportObject) =>
            GetInterpolator(transportObject.Xs, transportObject.Ys, transportObject.Zs, transportObject.Type);


        public static IInterpolator2D GetInterpolator(double[][] x, double[] y, double[][] z, Interpolator2DType kind) => kind switch
        {
            Interpolator2DType.Bilinear => new Generic2dInterpolator(x, y, z, Interpolator1DType.Linear, Interpolator1DType.Linear),
            Interpolator2DType.BiCubic => new Generic2dInterpolator(x, y, z, Interpolator1DType.CubicSpline, Interpolator1DType.CubicSpline),
            Interpolator2DType.DummyPoint => new DummyPointInterpolator(z[0][0]),
            _ => throw new InvalidOperationException($"We don't have a way of making a {kind} interpolator"),
        };

        public static IInterpolator2D GetInterpolator(TO_Interpolator2d_Jagged transportObject) =>
            GetInterpolator(transportObject.Xs, transportObject.Ys, transportObject.Zs, transportObject.Type);

        public static IInterpolator2D GetInterpolator(TO_Interpolator2d transportObject) => 
            transportObject.IsJagged ? GetInterpolator(transportObject.Jagged) : GetInterpolator(transportObject.Square);
    }
}
