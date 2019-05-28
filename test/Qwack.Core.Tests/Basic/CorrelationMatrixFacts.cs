using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Xunit;

namespace Qwack.Core.Tests.Basic
{
    public class CorrelationMatrixFacts
    {
        [Fact]
        public void CorrelMatrixFacts()
        {
            var z = new CorrelationMatrix();
            z = new CorrelationMatrix(new[] { "x" }, new[] { "y" }, new[,] { { 0.9999 } });
            Assert.Throws<Exception>(() => z.GetCorrelation("x", "z"));
            Assert.False(z.TryGetCorrelation("x", "z", out var c));
            var zz = z.Clone();
            var bumped = zz.Bump(0.5);
            Assert.True(bumped.GetCorrelation("x", "y") < 1.0);
        }

        [Fact]
        public void CorrelTimeVectorFacts()
        {
            var z = new CorrelationTimeVector();
            z = new CorrelationTimeVector("x", "y", new[] { 0.999, 0.998 }, new[] { 1.0, 2.0 });

            Assert.Throws<Exception>(() => z.GetCorrelation("x", "z"));
            Assert.False(z.TryGetCorrelation("x", "z", out var c));
            var zz = z.Clone();
            var bumped = zz.Bump(0.5);
            Assert.True(bumped.GetCorrelation("x", "y") < 1.0);
        }
    }
}
