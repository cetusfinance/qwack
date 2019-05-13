using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class NonContinuousInterpolatorFacts
    {
        [Fact]
        public void CanInterpolateFact()
        {
            var interp = new NonContinuousInterpolator();
            interp = new NonContinuousInterpolator(new[] { 10.0, 100.0 }, new[] { new DummyPointInterpolator(50), new DummyPointInterpolator(75) });

            Assert.Equal(50, interp.Interpolate(5));
            Assert.Equal(50, interp.Interpolate(10));
            Assert.Equal(75, interp.Interpolate(11));
            Assert.Equal(75, interp.Interpolate(110));

            Assert.Equal(0.0, interp.FirstDerivative(11));
            Assert.Equal(0.0, interp.FirstDerivative(110));

            Assert.Equal(0.0, interp.SecondDerivative(11));
            Assert.Equal(0.0, interp.SecondDerivative(110));

            Assert.Throws<NotImplementedException>(() => interp.Sensitivity(0));
            Assert.Throws<NotImplementedException>(() => interp.Bump(0,0));
            Assert.Throws<NotImplementedException>(() => interp.UpdateY(0,0));
        }


    }
}
