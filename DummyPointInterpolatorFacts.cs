using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class DummyPointInterpolatorFacts
    {
        [Fact]
        public void CanInterpolateFact()
        {
            var point = 99.97;
            var interp = new DummyPointInterpolator(point);
            Assert.Equal(point,  interp.Interpolate(5,25));
            Assert.Equal(point, interp.Interpolate(-6, 77));
            Assert.Equal(point, interp.Interpolate(0, 0));
            Assert.Equal(point, interp.Interpolate(55, 25));

            Assert.Equal(point, interp.Interpolate(55));
            Assert.Equal(point, interp.Interpolate(-55));
            Assert.Equal(point, interp.Interpolate(0));
            Assert.Equal(point, interp.Interpolate(System.Math.PI));
        }


    }
}
