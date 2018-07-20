using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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
    }
}
