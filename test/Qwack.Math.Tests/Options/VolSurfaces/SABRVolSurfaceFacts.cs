using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;

namespace Qwack.Math.Tests.Options.VolSurfaces
{
    public class SABRVolSurfaceFacts
    {
        [Fact]
        public void SABRSurface()
        {
            //flat surface
            var origin = new DateTime(2017, 02, 07);
            var strikes = new double[][] { new [] { 1.0, 2.0 } , new [] { 1.0, 2.0 } };
            var maturities = new DateTime[] { new DateTime(2018, 02, 07), new DateTime(2019, 02, 07) };
            Func<double, double> fwdCurve = (t => { return 1.5; });

            var vols = new double[][]
                {
                    new double[] { 0.32, 0.32 },
                    new double[] { 0.32, 0.32 }
                };
            var surface = new Qwack.Options.VolSurfaces.SABRVolSurface(
                origin, strikes, maturities, vols, fwdCurve,
                Math.Interpolation.Interpolator1DType.Linear,
                Dates.DayCountBasis.Act_365F);


            Assert.Equal(vols[0][0], surface.GetVolForAbsoluteStrike(999, origin.AddDays(33)), 12);
            Assert.Equal(vols[0][0], surface.GetVolForDeltaStrike(-0.3, origin.AddDays(303)), 12);
            Assert.Equal(vols[0][0], surface.GetVolForAbsoluteStrike(4, 0.777), 12);
            Assert.Equal(vols[0][0], surface.GetVolForDeltaStrike(0.9, 0.123), 12);

            vols = new double[][]
                 {
                    new double[] { 0.30, 0.34 },
                    new double[] { 0.30, 0.34 }
                 };

            surface = new Qwack.Options.VolSurfaces.SABRVolSurface(
             origin, strikes, maturities, vols, fwdCurve,
             Math.Interpolation.Interpolator1DType.Linear,
             Dates.DayCountBasis.Act_365F);
        }
    }
}