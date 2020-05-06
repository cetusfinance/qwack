using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Math.Tests.Options.VolSurfaces
{
    public class SABRVolSurfaceFacts
    {
        [Fact]
        public void SABRSurfaceFlat()
        {
            //flat surface
            var origin = new DateTime(2017, 02, 07);
            var strikes = new double[][] { new[] { 1.4, 1.6 }, new[] { 1.4, 1.6 } };
            var maturities = new DateTime[] { new DateTime(2018, 02, 07), new DateTime(2019, 02, 07) };
            var fwd = 1.5;
            Func<double, double> fwdCurve = (t => { return fwd; });

            var vols = new double[][]
                {
                    new double[] { 0.32, 0.32 },
                    new double[] { 0.32, 0.32 }
                };
            var surface = new SabrVolSurface(
                origin, strikes, maturities, vols, fwdCurve,
                Interpolator1DType.Linear,
                DayCountBasis.Act_365F);

            Assert.Equal(vols[0][0], surface.GetVolForAbsoluteStrike(1.5, origin.AddDays(33), fwd), 2);
            Assert.Equal(vols[0][0], surface.GetVolForDeltaStrike(-0.3, origin.AddDays(303), fwd), 2);
            Assert.Equal(vols[0][0], surface.GetVolForAbsoluteStrike(3, 0.777, fwd), 2);
            Assert.Equal(vols[0][0], surface.GetVolForDeltaStrike(0.9, 0.123, fwd), 2);
        }

        [Fact]
        public void SABRSurfaceRRBF()
        {
            //flat surface
            var origin = new DateTime(2017, 02, 07);
            var expiry = origin.AddYears(1);
            var t = (expiry - origin).TotalDays / 365.0;
            var fwd = 1.5;
            var vol = 0.32;
            var rr = new[] { new[] { 0.02, 0.03 } };
            var bf = new[] { new[] { 0.005, 0.007 } };

            Func<double, double> fwdCurve = (tt => { return fwd; });

            var surface = new SabrVolSurface(origin, new[] { vol }, new[] { expiry }, new[] { 0.25, 0.1 }, rr, bf, new[] { 100.0 }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.Linear);
            var gSurface = new RiskyFlySurface(origin, new[] { vol }, new[] { expiry }, new[] { 0.25, 0.1 }, rr, bf, new[] { 100.0 }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.Linear, Interpolator1DType.Linear);

            var atmK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, 0.5, 0.0, t, vol);
            Assert.Equal(vol, surface.GetVolForAbsoluteStrike(atmK, expiry, fwd), 2);

            var v25c = gSurface.GetVolForDeltaStrike(0.75, expiry, fwd);
            var v25p = gSurface.GetVolForDeltaStrike(0.25, expiry, fwd);
            var k25c = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, 0.25, 0.0, t, v25c);
            var k25p = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.25, 0.0, t, v25p);

            var t25c = surface.GetVolForAbsoluteStrike(k25c, expiry, fwd);
            var t25p = surface.GetVolForAbsoluteStrike(k25p, expiry, fwd);

            Assert.Equal(rr[0][0], t25c - t25p, 2);
        }
    }
}
