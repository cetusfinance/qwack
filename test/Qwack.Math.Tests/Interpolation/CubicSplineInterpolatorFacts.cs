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
        public void CanInterpolateMonotone()
        {
            var interp = new CubicHermiteSplineInterpolator(
                new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 }, 
                new double[] { 3.0, 2.9, 2.5, 1.0, 0.9, 0.8, 0.7, 0.3, 0.1 },
                true);

            var lastI = interp.Interpolate(0);
            for(var i=1;i<100;i++)
            {
                var thisI = interp.Interpolate(i / 100.0);
                Assert.True(thisI <= lastI);
                lastI = thisI;
            }
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

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(17)]
        [InlineData(20)]
        public void SecondDerivative(double point)
        {
            var interp = new CubicHermiteSplineInterpolator(new double[] { 0, 10, 20 }, new double[] { 10, 15, 40 });

            var bump = 0.00000001;
            var v1 = interp.FirstDerivative(point - bump / 2);
            var v2 = interp.FirstDerivative(point + bump / 2);
            var slope = (v2 - v1) / bump;

            Assert.Equal(slope, interp.SecondDerivative(point), 2);
        }

        [Theory]
        [InlineData(0,1, 10.219166666666666)]
        [InlineData(5, 15, 163.54166666666669)]
        [InlineData(20, 25, 231.25)] //linear extrap right
        [InlineData(-10, 0, 75)] //lienar extrap left
        public void Integral(double pointA, double pointB, double expected)
        {
            var interp = new CubicHermiteSplineInterpolator(new double[] { 0, 10, 20 }, new double[] { 10, 15, 40 });
            var v1 = interp.DefiniteIntegral(pointA, pointB);
            Assert.Equal(expected, v1, 8);
        }
    }
}
