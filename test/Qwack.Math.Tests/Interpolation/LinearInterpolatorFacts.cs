using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Interpolation
{
    public class LinearInterpolatorFacts
    {
        [Fact]
        public void CanInterpolateFact()
        {
            var interp = new LinearInterpolator();
            interp = new LinearInterpolator(new double[] { 0, 10 }, new double[] { 10, 10 });
            Assert.Equal(10.0, interp.Interpolate(100.0));
            var i2 = interp.Bump(1, 10, false);
            Assert.Equal(110.0, i2.Interpolate(100.0));
            interp.Bump(1, 10, true);
            Assert.Equal(110.0, interp.Interpolate(100.0));

            var interp2 = new LinearInterpolatorFlatExtrap();
            interp2 = new LinearInterpolatorFlatExtrap(new double[] { 0, 10 }, new double[] { 10, 10 });
            Assert.Equal(10.0, interp2.Interpolate(100.0));
            var i4 = interp2.Bump(1, 10, false);
            Assert.Equal(20, i4.Interpolate(100.0));
            interp2.Bump(1, 10, true);
            Assert.Equal(20, interp2.Interpolate(100.0));

            var interp3 = new LinearInterpolatorFlatExtrapNoBinSearch();
            interp3 = new LinearInterpolatorFlatExtrapNoBinSearch(new double[] { 0, 10 }, new double[] { 10, 10 });
            Assert.Equal(10.0, interp3.Interpolate(100.0));
            var i5 = interp3.Bump(1, 10, false);
            Assert.Equal(20, i5.Interpolate(100.0));
            interp3.Bump(1, 10, true);
            Assert.Equal(20, interp3.Interpolate(100.0));
        }

        [Fact]
        public void LinearInterpolatorTests_CanExtrapolate()
        {
            IInterpolator1D interp = new LinearInterpolator(new double[] { 5, 10 }, new double[] { 5, 10 });
            Assert.Equal(0, interp.Interpolate(0.0));
            Assert.Equal(7.5, interp.Interpolate(7.5));
            Assert.Equal(100, interp.Interpolate(100));
            Assert.Equal(1, interp.FirstDerivative(0));
            Assert.Equal(1, interp.FirstDerivative(5));
            Assert.Equal(1, interp.FirstDerivative(100));
            Assert.Equal(0, interp.SecondDerivative(0));
            Assert.Equal(1, interp.SecondDerivative(5));
            Assert.Equal(0, interp.SecondDerivative(100));

            interp = new LinearInterpolatorFlatExtrap(new double[] { 5, 10 }, new double[] { 5, 10 });
            Assert.Equal(5, interp.Interpolate(0.0));
            Assert.Equal(7.5, interp.Interpolate(7.5));
            Assert.Equal(10, interp.Interpolate(100));
            Assert.Equal(0, interp.FirstDerivative(0));
            Assert.Equal(1, interp.FirstDerivative(5));
            Assert.Equal(0, interp.FirstDerivative(100));
            Assert.Equal(0, interp.SecondDerivative(0));
            Assert.Equal(0.5, interp.SecondDerivative(5));
            Assert.Equal(0, interp.SecondDerivative(100));
        }

        [Fact]
        public void LinearInterpolatorTests_CanIntegrate()
        {
            var interp = new LinearInterpolatorFlatExtrap(new double[] { 5, 10 }, new double[] { 5, 10 });
            Assert.Throws<Exception>(() => { interp.DefiniteIntegral(5, 4); });
            Assert.Equal(0, interp.DefiniteIntegral(0, 0));
            Assert.Equal(37.5, interp.DefiniteIntegral(5, 10));
            Assert.Equal(25, interp.DefiniteIntegral(0, 5));
            Assert.Equal(100, interp.DefiniteIntegral(10, 20));
            Assert.Equal(100 + 25 + 37.5, interp.DefiniteIntegral(0, 20));
            Assert.Equal(11.0 / 2, interp.DefiniteIntegral(5, 6));
        }

        [Fact]
        public void LinearInterpolatorTests_CanDifferentiate()
        {
            var interp = new LinearInterpolatorFlatExtrap(new double[] { 5, 10 }, new double[] { 5, 10 });

            Assert.Equal(0, interp.FirstDerivative(0));
            Assert.Equal(0, interp.FirstDerivative(20));
            Assert.Equal(1.0, interp.FirstDerivative(6));

            Assert.Equal(0, interp.SecondDerivative(0));
            Assert.Equal(0, interp.SecondDerivative(20));
            Assert.Equal(0.5, interp.SecondDerivative(5));

            var interp2 = new LinearInterpolator(new double[] { 5, 10 }, new double[] { 5, 10 });

            Assert.Equal(1.0, interp2.FirstDerivative(0));
            Assert.Equal(1.0, interp2.FirstDerivative(20));
            Assert.Equal(1.0, interp2.FirstDerivative(6));

            Assert.Equal(0.0, interp2.SecondDerivative(0));
            Assert.Equal(0.0, interp2.SecondDerivative(20));
            Assert.Equal(1.0, interp2.SecondDerivative(5));

            var interp3 = new LinearInterpolatorFlatExtrapNoBinSearch(new double[] { 5, 10 }, new double[] { 5, 10 });

            Assert.Equal(0, interp3.FirstDerivative(0));
            Assert.Equal(0, interp3.FirstDerivative(20));
            Assert.Equal(1.0, interp3.FirstDerivative(6),6);

            Assert.Equal(0, interp3.SecondDerivative(0));
            Assert.Equal(0, interp3.SecondDerivative(20));
            Assert.Equal(0.0, interp3.SecondDerivative(5),6);

        }

        [Fact]
        public void SensitivityFact()
        {
            var interp = new LinearInterpolator(new double[] { 0, 10 }, new double[] { 10, 10 });
            var r = interp.Sensitivity(5);
            Assert.Equal(0.5, r[0]);
            Assert.Equal(0.5, r[1]);

            r = interp.Sensitivity(0);
            Assert.Equal(1.0, r[0]);
            Assert.Equal(0.0, r[1]);

            r = interp.Sensitivity(60);
            Assert.Equal(0.0, r[0]);
            Assert.Equal(1.0, r[1]);


            var interp2 = new LinearInterpolatorFlatExtrap(new double[] { 0, 10 }, new double[] { 10, 10 });
            var r2 = interp2.Sensitivity(5);
            Assert.Equal(0.5, r2[0]);
            Assert.Equal(0.5, r2[1]);

            r2 = interp2.Sensitivity(0);
            Assert.Equal(1.0, r2[0]);
            Assert.Equal(0.0, r2[1]);

            r2 = interp2.Sensitivity(60);
            Assert.Equal(0.0, r2[0]);
            Assert.Equal(1.0, r2[1]);


            var interp3 = new LinearInterpolatorFlatExtrapNoBinSearch(new double[] { 0, 10 }, new double[] { 10, 10 });
            var r3 = interp3.Sensitivity(5);
            Assert.Equal(0.5, r3[0]);
            Assert.Equal(0.5, r3[1]);

            r3 = interp3.Sensitivity(0);
            Assert.Equal(1.0, r3[0]);
            Assert.Equal(0.0, r3[1]);

            r3 = interp3.Sensitivity(60);
            Assert.Equal(0.0, r3[0]);
            Assert.Equal(1.0, r3[1]);
        }
    }
}
