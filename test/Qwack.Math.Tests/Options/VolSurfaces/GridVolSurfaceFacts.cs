using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;

namespace Qwack.Math.Tests.Options.VolSurfaces
{
    public class GridVolSurfaceFacts
    {
        [Fact]
        public void GridVolSurfaceAbsolute()
        {
            //flat surface
            var origin = new DateTime(2017, 02, 07);
            var strikes = new double[] { 1, 2 };
            var maturities = new DateTime[] { new DateTime(2017, 04, 06), new DateTime(2017, 06, 07) };
            var vols = new double[][]
                {
                    new double[] { 0.32, 0.32 },
                    new double[] { 0.32, 0.32 }
                };
            var surface = new Qwack.Options.VolSurfaces.GridVolSurface(
                origin, strikes, maturities, vols,
                Core.Basic.StrikeType.Absolute,
                Math.Interpolation.Interpolator1DType.Linear,
                Math.Interpolation.Interpolator1DType.Linear,
                Dates.DayCountBasis.Act_365F);

            var fwd = 1.5;
            Func<double, double> fwdCurve = (t => { return fwd; });
          
            Assert.Equal(vols[0][0], surface.GetVolForAbsoluteStrike(999, origin.AddDays(33), fwd), 12);
            Assert.Equal(vols[0][0], surface.GetVolForDeltaStrike(-0.3, origin.AddDays(303), fwd), 12);
            Assert.Equal(vols[0][0], surface.GetVolForAbsoluteStrike(4, 0.777, fwd), 12);
            Assert.Equal(vols[0][0], surface.GetVolForDeltaStrike(0.9, 0.123, fwd), 12);

            //with some shape
            vols = new double[][]
                {
                    new double[] { 0.16, 0.32 },
                    new double[] { 0.32, 0.64 }
                };

            surface = new Qwack.Options.VolSurfaces.GridVolSurface(
                origin, strikes, maturities, vols,
                Core.Basic.StrikeType.Absolute,
                Math.Interpolation.Interpolator1DType.Linear,
                Math.Interpolation.Interpolator1DType.Linear,
                Dates.DayCountBasis.Act_365F);

            Assert.Equal(vols[0].Average(), surface.GetVolForAbsoluteStrike(1.5, maturities[0], fwd), 12);
            DateTime midPoint = maturities[0].AddDays(((maturities[1] - maturities[0]).TotalDays / 2.0));
            Assert.Equal(vols.Select(x => x.Average()).Average(), surface.GetVolForAbsoluteStrike(1.5, midPoint, fwd), 12);

            //test delta-space function
            var vol = surface.GetVolForAbsoluteStrike(1.75, maturities[0].AddDays(15), fwd);
            var deltaK = BlackFunctions.BlackDelta(1.5, 1.75, 0, surface.ExpiriesDouble[0] + 15.0 / 365.0, vol, OptionType.C);
            var volForDeltaK = surface.GetVolForDeltaStrike(deltaK, maturities[0].AddDays(15), fwd);
            Assert.Equal(vol, volForDeltaK, 8);
        }

        [Fact]
        public void GridVolSurfaceDelta()
        {
            //flat surface
            var origin = new DateTime(2017, 02, 07);
            var strikes = new double[] { 0.25, 0.75 };
            var maturities = new DateTime[] { new DateTime(2017, 04, 06), new DateTime(2017, 06, 07) };
            var vols = new double[][]
                {
                    new double[] { 0.32, 0.32 },
                    new double[] { 0.32, 0.32 }
                };
            var surface = new Qwack.Options.VolSurfaces.GridVolSurface(
                origin, strikes, maturities, vols,
                Core.Basic.StrikeType.ForwardDelta,
                Math.Interpolation.Interpolator1DType.Linear,
                Math.Interpolation.Interpolator1DType.Linear,
                Dates.DayCountBasis.Act_365F);

            var fwd = 1.5;
            Func<double, double> fwdCurve = (t => { return fwd; });
        
            Assert.Equal(vols[0][0], surface.GetVolForAbsoluteStrike(3, origin.AddDays(33),fwd), 12);
            Assert.Equal(vols[0][0], surface.GetVolForDeltaStrike(-0.3, origin.AddDays(303), fwd), 12);
            Assert.Equal(vols[0][0], surface.GetVolForAbsoluteStrike(0.5, 0.777, fwd), 12);
            Assert.Equal(vols[0][0], surface.GetVolForDeltaStrike(0.9, 0.123, fwd), 12);

        }

    }
}