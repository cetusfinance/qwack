using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;
using Qwack.Dates;
using Qwack.Core.Basic;

namespace Qwack.Math.Tests.Options
{
    public class LMEBlackFacts
    {
        [Fact]
        public void PVFacts()
        {
            var today = DateTime.Parse("2017-02-07");
            var expiry = DateTime.Parse("2017-12-06");
            var delivery = DateTime.Parse("2017-12-20");
            var tExp = DayCountBasis.Act_365F.CalculateYearFraction(today, expiry);
            var tDel = DayCountBasis.Act_365F.CalculateYearFraction(today, delivery);
            var k = 0;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;

            //zero strike put is worthless
            var PV = LMEFunctions.LMEBlackPV(f, k, rf, tExp, tDel, vol, cp);
            Assert.Equal(0, PV, 10);

            //zero strike call is worth discounted fwd
            cp = OptionType.C;
            PV = LMEFunctions.LMEBlackPV(f, k, rf, tExp, tDel, vol, cp);
            Assert.Equal(System.Math.Exp(-rf*tDel)*f, PV, 10);

            //OTM option with zero vol is worthless
            vol = 0.0;
            k = f + 1;
            PV = LMEFunctions.LMEBlackPV(f, k, rf, tExp, tDel, vol, cp);
            Assert.Equal(0, PV, 10);

            //put-call parity at f==k
            k = f;
            vol = 0.32;
            var PVcall = LMEFunctions.LMEBlackPV(f, k, rf, tExp, tDel, vol, OptionType.C);
            var PVput = LMEFunctions.LMEBlackPV(f, k, rf, tExp, tDel, vol, OptionType.P);
            Assert.Equal(PVcall, PVput, 10);
        }

        [Fact]
        public void VegaFacts()
        {
            var today = DateTime.Parse("2017-02-07");
            var expiry = DateTime.Parse("2017-12-06");
            var delivery = DateTime.Parse("2017-12-20");
            var tExp = DayCountBasis.Act_365F.CalculateYearFraction(today, expiry);
            var tDel = DayCountBasis.Act_365F.CalculateYearFraction(today, delivery);
            var k = 100;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;

            //vega closely matches numerical estimate
            var PV1 = LMEFunctions.LMEBlackPV(f, k, rf, tExp, tDel, vol-0.00005, cp);
            var PV2 = LMEFunctions.LMEBlackPV(f, k, rf, tExp, tDel, vol+0.00005, cp);
            var vegaEst = (PV2 - PV1) / 0.0001 * 0.01;
            var vega = LMEFunctions.LMEBlackVega(f, k, rf, tExp, tDel, vol);
            Assert.Equal(vegaEst, vega, 6);


            //cases for zero vega
            vega = LMEFunctions.LMEBlackVega(f, 0, rf, tExp, tDel, vol);
            Assert.Equal(0, vega, 8);

            vega = LMEFunctions.LMEBlackVega(f, 1e6, rf, tExp, tDel, vol);
            Assert.Equal(0, vega, 8);
        }

        [Fact]
        public void DeltaGammaFacts()
        {
            var today = DateTime.Parse("2017-02-07");
            var expiry = DateTime.Parse("2017-12-06");
            var delivery = DateTime.Parse("2017-12-20");
            var tExp = DayCountBasis.Act_365F.CalculateYearFraction(today, expiry);
            var tDel = DayCountBasis.Act_365F.CalculateYearFraction(today, delivery);
            var k = 100;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;

            //delta closely matches numerical estimate
            var PV1 = LMEFunctions.LMEBlackPV(f + 0.000005, k, rf, tExp, tDel, vol, cp);
            var PV2 = LMEFunctions.LMEBlackPV(f - 0.000005, k, rf, tExp, tDel, vol, cp);
            var deltaEst = System.Math.Exp(tDel*rf) * (PV1 - PV2) / 0.00001; //undiscount the delta
            var delta = LMEFunctions.LMEBlackDelta(f, k, tExp, vol, cp);

            Assert.Equal(deltaEst, delta, 6);

            //zero-strike call has fwd delta of 1.0
            k = 0;
            cp = OptionType.C;
            delta = LMEFunctions.LMEBlackDelta(f, k, tExp, vol, cp);
            Assert.Equal(1.0, delta, 10);

            //put-call parity
            k = 100;
            var deltaCall = LMEFunctions.LMEBlackDelta(f, k, tExp, vol, OptionType.C);
            var deltaPut = LMEFunctions.LMEBlackDelta(f, k, tExp, vol, OptionType.P);

            var syntheticFwdDelta = deltaCall - deltaPut;
            Assert.Equal(1.0, syntheticFwdDelta, 10);

            //gamma closely matches numerical estimate
            var delta1 = LMEFunctions.LMEBlackDelta(f + 0.000005, k, tExp, vol, cp);
            var delta2 = LMEFunctions.LMEBlackDelta(f - 0.000005, k, tExp, vol, cp);
            var gammaEst = (delta1 - delta2) / 0.00001;
            var gamma = LMEFunctions.LMEBlackGamma(f, k, tExp, vol);
            Assert.Equal(gammaEst, gamma, 6);
        }


        [Fact]
        public void DeltaStrikeMappingFacts()
        {
            var today = DateTime.Parse("2017-02-07");
            var expiry = DateTime.Parse("2017-12-06");
            var delivery = DateTime.Parse("2017-12-20");
            var tExp = DayCountBasis.Act_365F.CalculateYearFraction(today, expiry);
            var tDel = DayCountBasis.Act_365F.CalculateYearFraction(today, delivery);
            var k = 100;
            var f = 100;
            var vol = 0.32;
            var cp = OptionType.P;

            //black forward delta matches
            var delta = LMEFunctions.LMEBlackDelta(f, k, tExp, vol, cp);
            var absolute = LMEFunctions.AbsoluteStrikefromDeltaKAnalytic(f, delta, tExp, vol);
            Assert.Equal(k, absolute, 10);
        }

        [Fact]
        public void ImpliedVolFacts()
        {
            var today = DateTime.Parse("2017-02-07");
            var expiry = DateTime.Parse("2017-12-06");
            var delivery = DateTime.Parse("2017-12-20");
            var tExp = DayCountBasis.Act_365F.CalculateYearFraction(today, expiry);
            var tDel = DayCountBasis.Act_365F.CalculateYearFraction(today, delivery);
            var k = 120;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;

            var PV = LMEFunctions.LMEBlackPV(f, k, rf, tExp, tDel, vol, cp);
            var impliedVol = LMEFunctions.LMEBlackImpliedVol(f, k, rf, tExp, tDel, PV, cp);

            Assert.Equal(vol, impliedVol, 10);
        }
    }
}
