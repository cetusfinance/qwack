using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Regression
{
    public class SegmentedLinearRegressionFacts
    {
        [Fact]
        public void LinearFacts()
        {
            var xs = Enumerable.Range(0, 100).Select(x => (double)x).ToArray();
            var ys = xs.Select(x => 7.0 + 3.0 * x).ToArray();
            var slr = SegmentedLinearRegression.Regress(xs, ys, 5);
            for(var i=0;i<xs.Length;i++)
            {
                Assert.Equal(ys[i], slr.Interpolate(xs[i]), 12);
            }
        }

        [Fact]
        public void PiecewizeLinearFacts()
        {
            var xs = Enumerable.Range(0, 100).Select(x => (double)x).ToArray();
            var ys = xs.Select(x => x < 39 ? 7.0 + 3.0 * x : 65.5 + 1.5 * x).ToArray();
            var slr = SegmentedLinearRegression.Regress(xs, ys, 5);
            for (var i = 0; i < xs.Length; i++)
            {
                Assert.Equal(ys[i], slr.Interpolate(xs[i]), 8);
            }
        }
    }
}
