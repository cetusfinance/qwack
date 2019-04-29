using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;
using Qwack.Core.Basic;
using Qwack.Options.VolSurfaces;

namespace Qwack.Math.Tests.Options.VolSurfaces
{
    public class RiskyFlyVolSurfaceFacts
    {
        [Fact]
        public void RiskyFlySimple()
        {
            //flat surface
            var origin = new DateTime(2017, 02, 07);
            var atms = new double[] { 0.3, 0.32, 0.34 };
            var fwds = new double[] { 100, 102, 110 };
            var maturities = new DateTime[] { new DateTime(2017, 04, 06), new DateTime(2017, 06, 07), new DateTime(2017, 08, 07) };
            var wingDeltas = new[] { 0.1, 0.25 };
            var riskies = new[] { new[] { 0.025, 0.015 }, new[] { 0.025, 0.015 }, new[] { 0.025, 0.015 } };
            var flies = new[] { new[] { 0.0025, 0.0015 }, new[] { 0.0025, 0.0015 }, new[] { 0.0025, 0.0015 } };
            var surface = new RiskyFlySurface(
                origin, atms, maturities, wingDeltas, riskies, flies, fwds, WingQuoteType.Simple,
                AtmVolType.ZeroDeltaStraddle, Math.Interpolation.Interpolator1DType.Linear,
                Math.Interpolation.Interpolator1DType.LinearInVariance);

            Assert.Equal(atms[1], surface.GetVolForDeltaStrike(0.5, maturities[1], fwds[1]));

            var cube = surface.ToCube();
            Assert.Equal(fwds[0], cube.GetAllRows().First().Value);

            var recon = RiskyFlySurface.FromCube(cube, origin, Math.Interpolation.Interpolator1DType.Linear,
                Math.Interpolation.Interpolator1DType.LinearInVariance);
            Assert.Equal(surface.GetVolForDeltaStrike(0.5, maturities[1], fwds[1]), recon.GetVolForDeltaStrike(0.5, maturities[1], fwds[1]));

            var quotes = surface.DisplayQuotes();
            Assert.Equal(maturities[0], (DateTime) quotes[1,0]);

        }

        [Fact]
        public void WorksWIthBackwardsStrikes()
        {
            //flat surface
            var origin = new DateTime(2017, 02, 07);
            var atms = new double[] { 0.3, 0.32, 0.34 };
            var fwds = new double[] { 100, 102, 110 };
            var maturities = new DateTime[] { new DateTime(2017, 04, 06), new DateTime(2017, 06, 07), new DateTime(2017, 08, 07) };
            var wingDeltas = new[] { 0.1, 0.25 };
            var riskies = new[] { new[] { 0.025, 0.015 }, new[] { 0.025, 0.015 }, new[] { 0.025, 0.015 } };
            var flies = new[] { new[] { 0.0025, 0.0015 }, new[] { 0.0025, 0.0015 }, new[] { 0.0025, 0.0015 } };
            var surface = new Qwack.Options.VolSurfaces.RiskyFlySurface(
                origin, atms, maturities, wingDeltas, riskies, flies, fwds, WingQuoteType.Simple,
                AtmVolType.ZeroDeltaStraddle, Math.Interpolation.Interpolator1DType.Linear,
                Math.Interpolation.Interpolator1DType.LinearInVariance);

            var wingDeltas2 = new[] { 0.25, 0.1 };
            var riskies2 = new[] { new[] { 0.015, 0.025 }, new[] { 0.015, 0.025 }, new[] { 0.015, 0.025 } };
            var flies2 = new[] { new[] { 0.0015, 0.0025 }, new[] { 0.0015, 0.0025 }, new[] { 0.0015, 0.0025 } };

            var surface2 = new Qwack.Options.VolSurfaces.RiskyFlySurface(
                origin, atms, maturities, wingDeltas2, riskies2, flies2, fwds, WingQuoteType.Simple,
                AtmVolType.ZeroDeltaStraddle, Math.Interpolation.Interpolator1DType.Linear,
                Math.Interpolation.Interpolator1DType.LinearInVariance);

            Assert.Equal(surface.GetVolForDeltaStrike(0.5, maturities[1], fwds[1]), surface2.GetVolForDeltaStrike(0.5, maturities[1], fwds[1]));
            Assert.Equal(surface.GetVolForDeltaStrike(0.2, maturities[0], fwds[0]), surface2.GetVolForDeltaStrike(0.2, maturities[0], fwds[0]));
            Assert.Equal(surface.GetVolForDeltaStrike(0.1, maturities[2], fwds[2]), surface2.GetVolForDeltaStrike(0.1, maturities[2], fwds[2]));
            
        }
    }
}
