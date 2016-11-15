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
            switch (kind)
            {
                case Interpolator1DType.LinearFlatExtrap:
                    if(x.Length < 200)
                    { 
                        return new LinearInterpolatorFlatExtrapNoBinSearch(x, y, noCopy, isSorted);
                    }
                    else
                    {
                        return new LinearInterpolatorFlatExtrap(x, y, noCopy, isSorted);
                    }
                default:
                    throw new InvalidOperationException($"We don't have a way of making a {kind} interpolator");
            }
        }
    }
}
