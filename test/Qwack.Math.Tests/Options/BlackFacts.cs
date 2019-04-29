using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;
using Qwack.Core.Basic;
using static System.Math;

namespace Qwack.Math.Tests.Options
{
    public class BlackFacts
    {
        [Fact]
        public void PVFacts()
        {
            var t = 1.0;
            var k = 0;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;

            //zero strike put is worthless
            var PV = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            Assert.Equal(0, PV, 10);

            //zero strike call is worth discounted fwd
            cp = OptionType.C;
            PV = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            Assert.Equal(Exp(-rf * t) * f, PV, 10);

            //OTM option with zero vol is worthless
            vol = 0.0;
            k = f + 1;
            PV = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            Assert.Equal(0, PV, 10);

            //put-call parity at f==k
            k = f;
            vol = 0.32;
            var PVcall = BlackFunctions.BlackPV(f, k, rf, t, vol, OptionType.C);
            var PVput = BlackFunctions.BlackPV(f, k, rf, t, vol, OptionType.P);
            Assert.Equal(PVcall, PVput, 10);

            //forward price constant wrt total variance
            rf = 0;
            var variance = vol * vol * t;
            var t2 = t * 2.0;
            var vol2 = Sqrt(variance / t2);

            var PVnear = BlackFunctions.BlackPV(f, k, rf, t, vol, OptionType.C);
            var PVfar = BlackFunctions.BlackPV(f, k, rf, t2, vol2, OptionType.C);
            Assert.Equal(PVnear, PVfar, 10);
        }

        [Fact]
        public void VegaFacts()
        {
            var t = 1.0;
            var k = 100;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;

            //vega closely matches numerical estimate
            var PV1 = BlackFunctions.BlackPV(f, k, rf, t, vol - 0.00005, cp);
            var PV2 = BlackFunctions.BlackPV(f, k, rf, t, vol + 0.00005, cp);
            var vegaEst = (PV2 - PV1) / 0.0001 * 0.01;
            var vega = BlackFunctions.BlackVega(f, k, rf, t, vol);
            Assert.Equal(vegaEst, vega, 6);

            //all else the same, more time==more vega
            var vegaNear = BlackFunctions.BlackVega(f, k, rf, t, vol);
            var vegaFar = BlackFunctions.BlackVega(f, k, rf, t * 2, vol);
            Assert.True(vegaFar > vegaNear);

            //cases for zero vega
            vega = BlackFunctions.BlackVega(f, 0, rf, t, vol);
            Assert.Equal(0, vega, 8);

            vega = BlackFunctions.BlackVega(f, 1e6, rf, t, vol);
            Assert.Equal(0, vega, 8);
        }

        [Fact]
        public void ThetaFacts()
        {
            var t = 1.0;
            var k = 100;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;


            //theta closely matches numerical estimate
            var bumpT = 1e-10;
            var PV1 = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            var PV2 = BlackFunctions.BlackPV(f, k, rf, t- bumpT, vol, cp);
            var thetaEst = (PV2 - PV1) / bumpT;
            var theta = BlackFunctions.BlackTheta(f, k, rf, t, vol,cp);
            Assert.Equal(thetaEst, theta, 3);
        }

        [Fact]
        public void DeltaGammaFacts()
        {
            var t = 1.0;
            var k = 100;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;

            //delta closely matches numerical estimate
            var PV1 = BlackFunctions.BlackPV(f + 0.000005, k, rf, t, vol, cp);
            var PV2 = BlackFunctions.BlackPV(f - 0.000005, k, rf, t, vol, cp);
            var deltaEst = (PV1 - PV2) / 0.00001;
            var delta = BlackFunctions.BlackDelta(f, k, rf, t, vol, cp);
            Assert.Equal(deltaEst, delta, 6);

            //all else the same, more time for OTM option == more delta
            k = 150;
            var deltaNear = BlackFunctions.BlackDelta(f, k, rf, t, vol, cp);
            var deltaFar = BlackFunctions.BlackDelta(f, k, rf, t * 2, vol, cp);
            Assert.True(deltaFar > deltaNear);

            //put-call parity
            var deltaCall = BlackFunctions.BlackDelta(f, k, rf, t, vol, OptionType.C);
            var deltaPut = BlackFunctions.BlackDelta(f, k, rf, t, vol, OptionType.P);

            var syntheticFwdDelta = deltaCall - deltaPut;
            Assert.Equal(System.Math.Exp(-rf * t), syntheticFwdDelta, 10);

            //gamma closely matches numerical estimate
            var delta1 = BlackFunctions.BlackDelta(f + 0.000005, k, rf, t, vol, cp);
            var delta2 = BlackFunctions.BlackDelta(f - 0.000005, k, rf, t, vol, cp);
            var gammaEst = (delta1 - delta2) / 0.00001;
            var gamma = BlackFunctions.BlackGamma(f, k, rf, t, vol);
            Assert.Equal(gammaEst, gamma, 6);

            //cases for zero delta / gamma
            delta = BlackFunctions.BlackDelta(f, 0, rf, t, vol, OptionType.P);
            Assert.Equal(0, delta, 8);
            delta = BlackFunctions.BlackDelta(f, 1e6, rf, t, vol, OptionType.C);
            Assert.Equal(0, delta, 8);
            gamma = BlackFunctions.BlackGamma(f, 0, rf, t, vol);
            Assert.Equal(0, gamma, 8);
            gamma = BlackFunctions.BlackGamma(f, 1e6, rf, t, vol);
            Assert.Equal(0, gamma, 8);
        }


        [Fact]
        public void DeltaStrikeMappingFacts()
        {
            var t = 1.0;
            var k = 100;
            var f = 100;
            var vol = 0.32;
            var rf = 0.0;
            var cp = OptionType.P;

            //black forward delta matches
            var delta = BlackFunctions.BlackDelta(f, k, rf, t, vol, cp);
            var absolute = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(f, delta, rf, t, vol);
            Assert.Equal(k, absolute, 10);
        }

        [Fact]
        public void ImpliedVolFacts()
        {
            var t = 1.0;
            var k = 120;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;

            var PV = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            var impliedVol = BlackFunctions.BlackImpliedVol(f, k, rf, t, PV, cp);

            Assert.Equal(vol, impliedVol, 10);
        }

        [Fact]
        public void BarrierFacts()
        {
            var t = 1.0;
            var k = 100;
            var b = 0.0;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.C;

            //zero barrier knock-in down is worthless
            var PV = BlackFunctions.BarrierOptionPV(f, k, rf, t, vol, cp, b, BarrierType.KI, BarrierSide.Down);
            Assert.Equal(0.0, PV, 10);

            //zero barrier knock-in up is worth vanilla
            var vanillaPV = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            PV = BlackFunctions.BarrierOptionPV(f, k, rf, t, vol, cp, b, BarrierType.KI, BarrierSide.Up);
            Assert.Equal(vanillaPV, PV, 10);

            //ki forward is worth same as fwd
            b = 100;
            k = 110;
            var PVc = BlackFunctions.BarrierOptionPV(f, k, rf, t, vol, OptionType.C, b, BarrierType.KI, BarrierSide.Down);
            var PVp = BlackFunctions.BarrierOptionPV(f, k, rf, t, vol, OptionType.P, b, BarrierType.KI, BarrierSide.Down);
            var df = Exp(-rf * t);
            var fwdPV = (f - k) * df;
            Assert.Equal(fwdPV, PVc - PVp, 10);
        }

        [Fact]
        public void DigitalFacts()
        {
            var t = 1.0;
            var f = 100;
            var vol = 0.32;
            var rf = 0.0;
            var cp = OptionType.C;
            var k = 110;

            var digiPV = BlackFunctions.BlackDigitalPV(f, k, rf, t, vol, cp);
            var spread = 0.0001;
            var expected = (BlackFunctions.BlackPV(f, k, rf, t, vol, cp) - BlackFunctions.BlackPV(f, k + spread, rf, t, vol, cp)) / spread;
            Assert.Equal(expected, digiPV, 6);

            var iv = BlackFunctions.BlackDigitalImpliedVol(f, k, rf, t, digiPV, cp);
            Assert.Equal(vol, iv, 6);
        }
    }
}
