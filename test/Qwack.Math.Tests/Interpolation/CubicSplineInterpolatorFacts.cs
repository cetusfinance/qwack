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
        public void CanInterpolate()
        {
            var interp = new CubicHermiteSplineInterpolator(new double[] { 0, 10, 20 }, new double[] { 10, 15, 40 });
            Assert.Equal(10.0,  interp.Interpolate(0));
            Assert.Equal(15.0, interp.Interpolate(10));
            Assert.Equal(40.0, interp.Interpolate(20));

            Assert.True(interp.Interpolate(5) > 10 && interp.Interpolate(5) < 15);
        }

        [Fact]
        public void CanClone()
        {
            var interp = new CubicHermiteSplineInterpolator(new double[] { 0, 10, 20 }, new double[] { 10, 15, 40 });
            
            var i2 = interp.UpdateY(2, 50);

            Assert.Equal(10.0, interp.Interpolate(0));
            Assert.Equal(15.0, interp.Interpolate(10));
            Assert.Equal(40.0, interp.Interpolate(20));

            Assert.Equal(10.0, i2.Interpolate(0));
            Assert.Equal(15.0, i2.Interpolate(10));
            Assert.Equal(50.0, i2.Interpolate(20));

        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(17)]
        [InlineData(20)]
        public void FirstDerivative(double point)
        {
            var interp = new CubicHermiteSplineInterpolator(new double[] { 0, 10, 20 }, new double[] { 10, 15, 40 });

            var bump = 0.0000000001;
            var v1 = interp.Interpolate(point - bump/2);
            var v2 = interp.Interpolate(point + bump/2);
            var slope = (v2 - v1) / bump;

            Assert.Equal(slope, interp.FirstDerivative(point),2);
        }
    }
}
