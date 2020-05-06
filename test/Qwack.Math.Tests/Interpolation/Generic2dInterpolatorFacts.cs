using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class Generic2dInterpolatorFacts
    {
        [Fact]
        public void CanInterpolateFact()
        {
            var interp = new Generic2dInterpolator();

            var xs = new[] { 0.0, 1.0 };
            var ys = new[] { 0.0, 1.0 };
            var zs = new[,] { { 3.0, 2.0 }, { 3.0, 2.0 } };

            interp = new Generic2dInterpolator(xs, ys, zs, Interpolator1DType.Linear, Interpolator1DType.Linear);
            Assert.Equal(2.5, interp.Interpolate(0.5, 0.5));

            var xa = new double[][] { new[] { 0.0, 1.0 }, new[] { 0.0, 1.0 } };
            var za = new double[][] { new[] { 3.0, 2.0 }, new[] { 3.0, 2.0 } };
            interp = new Generic2dInterpolator(xa, ys, za, Interpolator1DType.Linear, Interpolator1DType.Linear);
            Assert.Equal(2.5, interp.Interpolate(0.5, 0.5));

        }

        [Fact]
        public void FactoryFacts()
        {
            var xs = new[] { 0.0, 1.0 };
            var ys = new[] { 0.0, 1.0 };
            var zs = new[,] { { 3.0, 2.0 }, { 3.0, 2.0 } };


            var interp = InterpolatorFactory.GetInterpolator(xs, ys, zs, Interpolator2DType.Bilinear);
            Assert.Equal(2.5, interp.Interpolate(0.5, 0.5));
            interp = InterpolatorFactory.GetInterpolator(xs, ys, zs, Interpolator2DType.BiCubic);
            Assert.Equal(2.0, interp.Interpolate(1.0, 1.0));
            interp = InterpolatorFactory.GetInterpolator(xs, ys, zs, Interpolator2DType.DummyPoint);
            Assert.Equal(3.0, interp.Interpolate(1.0, 1.0));

            var xa = new double[][] { new[] { 0.0, 1.0 }, new[] { 0.0, 1.0 } };
            var za = new double[][] { new[] { 3.0, 2.0 }, new[] { 3.0, 2.0 } };
            interp = InterpolatorFactory.GetInterpolator(xa, ys, za, Interpolator2DType.Bilinear);
            Assert.Equal(2.5, interp.Interpolate(0.5, 0.5));
            interp = InterpolatorFactory.GetInterpolator(xa, ys, za, Interpolator2DType.BiCubic);
            Assert.Equal(2.0, interp.Interpolate(1.0, 1.0));
            interp = InterpolatorFactory.GetInterpolator(xa, ys, za, Interpolator2DType.DummyPoint);
            Assert.Equal(3.0, interp.Interpolate(1.0, 1.0));

        }
    }
}
