using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class ConstantHazzardInterpolatorFacts
    {
        [Fact]
        public void CanInterpolate()
        {
            var hz = 0.5;
            var sut = new ConstantHazzardInterpolator();
            sut = new ConstantHazzardInterpolator(hz);

            var t = 2.5;
            Assert.Equal(System.Math.Exp(-hz * t), sut.Interpolate(t));
            Assert.Equal(-hz * System.Math.Exp(-hz * t), sut.FirstDerivative(t));
            Assert.Equal(hz * hz * System.Math.Exp(-hz * t), sut.SecondDerivative(t));

            Assert.Throws<NotImplementedException>(() => sut.Sensitivity(0));
            Assert.Throws<NotImplementedException>(() => sut.Bump(0,0));
            Assert.Throws<NotImplementedException>(() => sut.UpdateY(0,0));
        }
    }
}
