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
            var interp = new DummyPointInterpolator();
            var point = 99.97;
            interp = new DummyPointInterpolator(point);
            Assert.Equal(point,  interp.Interpolate(5,25));
            Assert.Equal(point, interp.Interpolate(-6, 77));
            Assert.Equal(point, interp.Interpolate(0, 0));
            Assert.Equal(point, interp.Interpolate(55, 25));

            Assert.Equal(point, interp.Interpolate(55));
            Assert.Equal(point, interp.Interpolate(-55));
            Assert.Equal(point, interp.Interpolate(0));
            Assert.Equal(point, interp.Interpolate(System.Math.PI));

            Assert.Equal(0, interp.FirstDerivative(System.Math.PI));
            Assert.Equal(0, interp.SecondDerivative(System.Math.PI));

            Assert.Equal(1.0, interp.Sensitivity(System.Math.PI).First());

            var i2 = interp.Bump(66, 3.0);
            Assert.Equal(point+3.0, i2.Interpolate(55));

            var i3 = interp.UpdateY(66, 3.0);
            Assert.Equal(3.0, i3.Interpolate(55));
        }


    }
}
