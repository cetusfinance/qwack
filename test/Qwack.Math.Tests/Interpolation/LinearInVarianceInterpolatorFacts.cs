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
            var interp = new LinearInVarianceInterpolator();

            interp = new LinearInVarianceInterpolator(new double[] { 0, 10, 20 }, new double[] { 10, 20, 30 });
            Assert.Equal(20.0, interp.Interpolate(10));

            var midPoint = ((20 * 20 * 10) + (30 * 30 * 20)) / 2.0;
            midPoint = System.Math.Sqrt(midPoint / 15);
            Assert.Equal(midPoint, interp.Interpolate(15));

            Assert.Equal(0, interp.Interpolate(0));
            Assert.Equal(20, interp.Interpolate(1));
            Assert.Equal(20, interp.Interpolate(5));
            Assert.Equal(20, interp.Interpolate(10));

            var i2 = interp.Bump(1, 10);
            Assert.Equal(30, i2.Interpolate(10));
            var i3 = interp.Bump(1, 20,true);
            Assert.Equal(40, i3.Interpolate(10));

            Assert.Throws<NotImplementedException>(() => interp.Sensitivity(0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(17)]
        [InlineData(20)]
        public void FirstDerivative(double point)
        {
            var interp = new LinearInVarianceInterpolator(new double[] { 0, 10, 20 }, new double[] { 10, 20, 30 });

            var bump = 0.0000000001;
            var v1 = interp.Interpolate(point);
            var v2 = interp.Interpolate(point + bump);
            var slope = (v2 - v1) / bump;

            Assert.Equal(slope, interp.FirstDerivative(point), 2);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(17)]
        [InlineData(20)]
        public void SecondDerivative(double point)
        {
            var interp = new LinearInVarianceInterpolator(new double[] { 0, 10, 20 }, new double[] { 10, 15, 40 });

            var bump = 0.00000001;
            var v1 = interp.FirstDerivative(point);
            var v2 = interp.FirstDerivative(point + bump);
            var slope = (v2 - v1) / bump;

            Assert.Equal(slope, interp.SecondDerivative(point), 2);
        }
    }
}
