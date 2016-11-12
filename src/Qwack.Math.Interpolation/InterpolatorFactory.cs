using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Math.Interpolation
{
    public class InterpolatorFactory
    {
        public static IInterpolator1D GetInterpolator(double[] x, double[] y, Interpolator1DType kind, bool noCopy = false, bool isSorted = false)
        {
            throw new NotImplementedException("Not yet implemented");
        }
    }
}
