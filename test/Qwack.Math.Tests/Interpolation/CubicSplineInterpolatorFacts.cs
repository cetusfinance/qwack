using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class CubicSplineInterpolatorFacts
    {
        [Fact]
        public void CanInterpolateFact()
        {
            var interp = new CubicHermiteSplineInterpolator(new double[] { 0, 10, 20 }, new double[] { 10, 15, 40 });
            Assert.Equal(10.0,  interp.Interpolate(0));
            Assert.Equal(15.0, interp.Interpolate(10));
            Assert.Equal(40.0, interp.Interpolate(20));

            Assert.True(interp.Interpolate(5) > 10 && interp.Interpolate(5) < 15);
        }

    }
}
