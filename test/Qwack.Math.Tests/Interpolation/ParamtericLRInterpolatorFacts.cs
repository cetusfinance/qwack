using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class ParamtericLRInterpolatorFacts
    {
        [Fact]
        public void ParamtericLRInterpolatorFact()
        {
            var alpha = 7.6;
            var beta = 1.3;
            var z = new ParametricLinearInterpolator(alpha, beta);

            var tgtFunc = new Func<double, double>(x => alpha + beta * x);
            
            Assert.Equal(tgtFunc(3), z.Interpolate(3));
            Assert.Equal(tgtFunc(30), z.Interpolate(30));
            Assert.Equal(tgtFunc(0), z.Interpolate(0));
            Assert.Equal(tgtFunc(-30), z.Interpolate(-30));

            Assert.Equal(beta, z.FirstDerivative(0));
            Assert.Equal(beta, z.FirstDerivative(50));
            Assert.Equal(beta, z.FirstDerivative(-70));

            Assert.Equal(0, z.SecondDerivative(0));
            Assert.Equal(0, z.SecondDerivative(100));
            Assert.Equal(0, z.SecondDerivative(-99));
        }


    }
}
