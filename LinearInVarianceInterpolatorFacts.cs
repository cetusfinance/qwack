using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class LinearInVarianceInterpolatorFacts
    {
        [Fact]
        public void CanInterpolateFact()
        {
            var interp = new LinearInVarianceInterpolator(new double[] { 0, 10, 20 }, new double[] { 10, 20, 30 });
            Assert.Equal(20.0, interp.Interpolate(10));

            var midPoint = ((20 * 20 * 10) + (30 * 30 * 20)) / 2.0;
            midPoint = System.Math.Sqrt(midPoint / 15);
            Assert.Equal(midPoint, interp.Interpolate(15));

            Assert.Equal(0, interp.Interpolate(0));
            Assert.Equal(20, interp.Interpolate(1));
            Assert.Equal(20, interp.Interpolate(5));
            Assert.Equal(20, interp.Interpolate(10));
        }
    }
}
