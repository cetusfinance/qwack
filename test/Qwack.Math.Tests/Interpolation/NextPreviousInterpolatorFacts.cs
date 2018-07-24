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
            var interp = new NextInterpolator(new double[] { 0, 10, 20 }, new double[] { 20, 30, 100 });

            Assert.Equal(20, interp.Interpolate(-1));
            Assert.Equal(20, interp.Interpolate(0));
            Assert.Equal(30, interp.Interpolate(1));
            Assert.Equal(30, interp.Interpolate(5));
            Assert.Equal(100, interp.Interpolate(11));
            Assert.Equal(100, interp.Interpolate(25));
        }

        [Fact]
        public void PreviousInterpolatorFact()
        {
            var interp = new PreviousInterpolator(new double[] { 0, 10, 20 }, new double[] { 20, 30, 100 });

            Assert.Equal(20, interp.Interpolate(-1));
            Assert.Equal(20, interp.Interpolate(0));
            Assert.Equal(20, interp.Interpolate(1));
            Assert.Equal(20, interp.Interpolate(5));
            Assert.Equal(30, interp.Interpolate(11));
            Assert.Equal(100, interp.Interpolate(25));
        }
    }
}
