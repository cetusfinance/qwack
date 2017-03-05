using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class GKInterpolatorFacts
    {
        [Fact]
        public void CanInterpolateFact()
        {
            var xs = new double[] { 0.1, 0.25, 0.5, 0.75, 0.9 };
            var ys = new double[] { 0.32, 0.32, 0.34, 0.36, 0.38 };
            var interp = InterpolatorFactory.GetInterpolator(xs, ys, Interpolator1DType.GaussianKernel);

            //test pillar values are returned
            for (var i = 0; i < xs.Length; i++)
                Assert.Equal(ys[i], interp.Interpolate(xs[i]), 10);
        }
    }
}
