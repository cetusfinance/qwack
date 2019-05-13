using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class InterpolatorExtensionsFacts
    {
        [Fact]
        public void ExtensionFacts()
        {
            var i = new DummyPointInterpolator(0.5);
            var xs = new double[] { 1.0, 10.0 };
            Assert.Equal(0.5, i.Average(xs));
            Assert.Equal(0.5, i.MaxY(xs));
            Assert.Equal(0.5, i.MinY(xs));
            Assert.Equal(1.0, i.Sum(xs));
            Assert.Equal(0.5, i.Many(xs)[0]);
        }
    }
}
