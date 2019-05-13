using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class NextPreviousInterpolatorFacts
    {
        [Fact]
        public void NextInterpolatorFact()
        {
            var z = new NextInterpolator();
            var interp = new NextInterpolator(new double[] { 0, 10, 20 }, new double[] { 20, 30, 100 });

            Assert.Equal(20, interp.Interpolate(-1));
            Assert.Equal(20, interp.Interpolate(0));
            Assert.Equal(30, interp.Interpolate(1));
            Assert.Equal(30, interp.Interpolate(5));
            Assert.Equal(100, interp.Interpolate(11));
            Assert.Equal(100, interp.Interpolate(25));

            var i2 = interp.UpdateY(1, 25);

            Assert.Equal(20, i2.Interpolate(-1));
            Assert.Equal(20, i2.Interpolate(0));
            Assert.Equal(25, i2.Interpolate(1));
            Assert.Equal(25, i2.Interpolate(5));
            Assert.Equal(100, i2.Interpolate(11));
            Assert.Equal(100, i2.Interpolate(25));

            Assert.Equal(20, interp.Interpolate(-1));
            Assert.Equal(20, interp.Interpolate(0));
            Assert.Equal(30, interp.Interpolate(1));
            Assert.Equal(30, interp.Interpolate(5));
            Assert.Equal(100, interp.Interpolate(11));
            Assert.Equal(100, interp.Interpolate(25));

            i2 = interp.UpdateY(1, 27, true);

            Assert.Equal(20, i2.Interpolate(-1));
            Assert.Equal(20, i2.Interpolate(0));
            Assert.Equal(27, i2.Interpolate(1));
            Assert.Equal(27, i2.Interpolate(5));
            Assert.Equal(100, i2.Interpolate(11));
            Assert.Equal(100, i2.Interpolate(25));

            Assert.Equal(20, interp.Interpolate(-1));
            Assert.Equal(20, interp.Interpolate(0));
            Assert.Equal(27, interp.Interpolate(1));
            Assert.Equal(27, interp.Interpolate(5));
            Assert.Equal(100, interp.Interpolate(11));
            Assert.Equal(100, interp.Interpolate(25));

            Assert.Equal(0, interp.FirstDerivative(25));
            Assert.Equal(0, interp.SecondDerivative(25));
            Assert.Throws<NotImplementedException>(() => interp.Sensitivity(0));
        }

        [Fact]
        public void PreviousInterpolatorFact()
        {
            var z = new PreviousInterpolator();
            var interp = new PreviousInterpolator(new double[] { 0, 10, 20 }, new double[] { 20, 30, 100 });

            Assert.Equal(20, interp.Interpolate(-1));
            Assert.Equal(20, interp.Interpolate(0));
            Assert.Equal(20, interp.Interpolate(1));
            Assert.Equal(20, interp.Interpolate(5));
            Assert.Equal(30, interp.Interpolate(11));
            Assert.Equal(100, interp.Interpolate(25));

            var i2 = interp.UpdateY(1, 25);

            Assert.Equal(20, i2.Interpolate(-1));
            Assert.Equal(20, i2.Interpolate(0));
            Assert.Equal(20, i2.Interpolate(1));
            Assert.Equal(20, i2.Interpolate(5));
            Assert.Equal(25, i2.Interpolate(11));
            Assert.Equal(100, i2.Interpolate(25));

            Assert.Equal(20, interp.Interpolate(-1));
            Assert.Equal(20, interp.Interpolate(0));
            Assert.Equal(20, interp.Interpolate(1));
            Assert.Equal(20, interp.Interpolate(5));
            Assert.Equal(30, interp.Interpolate(11));
            Assert.Equal(100, interp.Interpolate(25));

            i2 = interp.UpdateY(1, 27, true);

            Assert.Equal(20, i2.Interpolate(-1));
            Assert.Equal(20, i2.Interpolate(0));
            Assert.Equal(20, i2.Interpolate(1));
            Assert.Equal(20, i2.Interpolate(5));
            Assert.Equal(27, i2.Interpolate(11));
            Assert.Equal(100, i2.Interpolate(25));

            Assert.Equal(20, interp.Interpolate(-1));
            Assert.Equal(20, interp.Interpolate(0));
            Assert.Equal(20, interp.Interpolate(1));
            Assert.Equal(20, interp.Interpolate(5));
            Assert.Equal(27, interp.Interpolate(11));
            Assert.Equal(100, interp.Interpolate(25));

            Assert.Equal(0, interp.FirstDerivative(25));
            Assert.Equal(0, interp.SecondDerivative(25));
            Assert.Throws<NotImplementedException>(() => interp.Sensitivity(0));
        }
    }
}
