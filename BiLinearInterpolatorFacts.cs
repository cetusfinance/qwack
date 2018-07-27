using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class BiLinearInterpolatorFacts
    {
        [Fact]
        public void CanInterpolateFact()
        {
            var interp = new BilinearInterpolator(new double[] { 0, 10 }, new double[] { 20, 30 }, new double[,] { { 20, 10 }, { 20, 10 } });
            Assert.Equal(15.0,  interp.Interpolate(5,25));

            //no extrapolation
            interp = new BilinearInterpolator(new double[] { 0, 10 }, new double[] { 20, 30 }, new double[,] { { 20, 10 }, { 20, 10 } });
            Assert.True(double.IsNaN(interp.Interpolate(500, 500)));
        }


    }
}
