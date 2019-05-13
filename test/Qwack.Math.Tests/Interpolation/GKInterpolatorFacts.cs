using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class GKInterpolatorFacts
    {
        [Fact]
        public void CanInterpolateFact()
        {
            var xs = new double[] { 0.1, 0.25, 0.5, 0.75, 0.9 };
            var ys = new double[] { 0.32, 0.32, 0.34, 0.36, 0.38 };
            var interp = InterpolatorFactory.GetInterpolator(xs, ys, Interpolator1DType.GaussianKernel);

            //test pillar values are returned
            for (var i = 0; i < xs.Length; i++)
                Assert.Equal(ys[i], interp.Interpolate(xs[i]), 10);

            var gk = new GaussianKernelInterpolator();
            Assert.Throws<NotImplementedException>(() => interp.Bump(0,0));
            Assert.Throws<NotImplementedException>(() => interp.UpdateY(0, 0));
            Assert.Throws<NotImplementedException>(() => interp.Sensitivity(0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.05)]
        [InlineData(0.1)]
        [InlineData(0.4)]
        [InlineData(0.5)]
        [InlineData(0.99)]
        public void FirstDerivative(double point)
        {
            var interp = new GaussianKernelInterpolator(new double[] { 0.1, 0.25, 0.5, 0.75, 0.9 }, new double[] { 0.32, 0.30, 0.26, 0.29, 0.31 });

            var bump = 0.0000000001;
            var v1 = interp.Interpolate(point - bump / 2);
            var v2 = interp.Interpolate(point + bump / 2);
            var slope = (v2 - v1) / bump;

            Assert.Equal(slope, interp.FirstDerivative(point), 2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.05)]
        [InlineData(0.1)]
        [InlineData(0.4)]
        [InlineData(0.5)]
        [InlineData(0.99)]
        public void SecondDerivative(double point)
        {
            var interp = new GaussianKernelInterpolator(new double[] { 0.1, 0.25, 0.5, 0.75, 0.9 }, new double[] { 0.32, 0.30, 0.26, 0.29, 0.31 });

            var bump = 0.0000001;
            var v1 = interp.FirstDerivative(point - bump / 2);
            var v2 = interp.FirstDerivative(point + bump / 2);
            var slope = (v2 - v1) / bump;

            Assert.Equal(slope, interp.SecondDerivative(point), 3);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.05)]
        [InlineData(0.1)]
        [InlineData(0.4)]
        [InlineData(0.5)]
        [InlineData(0.99)]
        public void SecondDerivativeRaw(double point)
        {
            var interp = new GaussianKernelInterpolator(new double[] { 0.1, 0.25, 0.5, 0.75, 0.9 }, new double[] { 0.32, 0.30, 0.26, 0.29, 0.31 });

            var bump = 0.0000001;
            var v1 = Distributions.Gaussian.GKernDeriv(point - bump / 2, 0.1, 0.25);
            var v2 = Distributions.Gaussian.GKernDeriv(point + bump / 2, 0.1, 0.25);
            var slope = (v2 - v1) / bump;

            Assert.Equal(slope, Distributions.Gaussian.GKernDeriv2(point, 0.1, 0.25), 3);
        }
    }
}
