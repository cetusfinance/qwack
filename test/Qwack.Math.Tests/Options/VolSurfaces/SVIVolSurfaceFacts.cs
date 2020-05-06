using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Options;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Math.Tests.Options.VolSurfaces
{
    public class SVIVolSurfaceFacts
    {
        [Fact]
        public void SVISurfaceRRBF()
        {
            var origin = new DateTime(2017, 02, 07);
            var expiry = origin.AddYears(1);
            var t = (expiry - origin).TotalDays / 365.0;
            var fwd = 1.5;
            var vol = 0.32;
            var rr = new[] { new[] { 0.015, 0.02 } };
            var bf = new[] { new[] { 0.005, 0.007 } };

            Func<double, double> fwdCurve = (tt => { return fwd; });

            var surface = new SVIVolSurface(origin, new[] { vol }, new[] { expiry }, new[] { 0.25, 0.1 }, rr, bf, new[] { fwd }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.Linear);
            var gSurface = new RiskyFlySurface(origin, new[] { vol }, new[] { expiry }, new[] { 0.25, 0.1 }, rr, bf, new[] { fwd }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.Linear, Interpolator1DType.Linear);

            var atmK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, 0.5, 0.0, t, vol);
            //Assert.Equal(vol, surface.GetVolForAbsoluteStrike(atmK, expiry, fwd), 2);
            Assert.Equal(vol, surface.GetVolForDeltaStrike(0.5, expiry, fwd), 2);

            var v25c = gSurface.GetVolForDeltaStrike(0.75, expiry, fwd);
            var v25p = gSurface.GetVolForDeltaStrike(0.25, expiry, fwd);
            var k25c = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, 0.25, 0.0, t, v25c);
            var k25p = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.25, 0.0, t, v25p);

            var t25c = surface.GetVolForAbsoluteStrike(k25c, expiry, fwd);
            var t25p = surface.GetVolForAbsoluteStrike(k25p, expiry, fwd);

       //     Assert.Equal(rr[0][0], t25c - t25p, 2);

            var v10c = gSurface.GetVolForDeltaStrike(0.90, expiry, fwd);
            var v10p = gSurface.GetVolForDeltaStrike(0.10, expiry, fwd);
            var k10c = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, 0.10, 0.0, t, v10c);
            var k10p = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.10, 0.0, t, v10p);

            var t10c = surface.GetVolForAbsoluteStrike(k10c, expiry, fwd);
            var t10p = surface.GetVolForAbsoluteStrike(k10p, expiry, fwd);

          //  Assert.Equal(rr[0][1], t10c - t10p, 2);

            
        }
    }
}
