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
        [Fact(Skip ="Failing")]
        public void CanInterpolateFact()
        {
            var interp = InterpolatorFactory.GetInterpolator(new double[] { 0, 10 }, new double[] { 20, 30 }, new double[,] { { 20, 10 }, { 20, 10 } }, Interpolator2DType.Bilinear);
            Assert.Equal(15.0,  interp.Interpolate(5,25));

            //no extrapolation
            interp = InterpolatorFactory.GetInterpolator(new double[] { 0, 10 }, new double[] { 20, 30 }, new double[,] { { 20, 10 }, { 20, 10 } }, Interpolator2DType.Bilinear);
            Assert.True(double.IsNaN(interp.Interpolate(500, 500)));
        }


    }
}
