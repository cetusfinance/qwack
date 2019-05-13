using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class SegmentedLinearRegressionFacts
    {
        [Fact]
        public void SegmentedLinearRegressionFact()
        {
            var xs = new[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 };
            var ys = new[] { 7.0, 6.0, 5.0, 13.0, 14.0, 15.0 };

            var r1 = SegmentedLinearRegression.RegressNotContinuous(xs, ys, 2);
            Assert.Equal(6.5, r1.Interpolate(0.5));
            Assert.Equal(13.5, r1.Interpolate(3.5));

        }


    }
}
