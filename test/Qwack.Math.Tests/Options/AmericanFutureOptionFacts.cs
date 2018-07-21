using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;
using Qwack.Core.Basic;

namespace Qwack.Math.Tests.Options
{
    public class AmericanFutureOptionFacts
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
            var PV = BinomialTree.AmericanFutureOptionPV(f, k, rf, t, vol, cp);
            Assert.Equal(0, PV, 10);
            PV = TrinomialTree.AmericanFutureOptionPV(f, k, rf, t, vol, cp);
            Assert.Equal(0, PV, 10);

            //zero strike call is worth fwd with no discounting
            cp = OptionType.C;
            PV = BinomialTree.AmericanFutureOptionPV(f, k, rf, t, vol, cp);
            Assert.Equal(f, PV, 10);
            PV = TrinomialTree.AmericanFutureOptionPV(f, k, rf, t, vol, cp);
            Assert.Equal(f, PV, 10);

            //OTM option with zero vol is worthless
            vol = 0.0;
            k = f + 1;
            PV = BinomialTree.AmericanFutureOptionPV(f, k, rf, t, vol, cp);
            Assert.Equal(0, PV, 10);
            PV = TrinomialTree.AmericanFutureOptionPV(f, k, rf, t, vol, cp);
            Assert.Equal(0, PV, 10);

            //option worth >= black in all cases for same inputs
            vol = 0.32;
            k = f + 20;
            var PVBlack = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            PV = BinomialTree.AmericanFutureOptionPV(f, k, rf, t, vol, cp);
            Assert.True(PV >= PVBlack);
            PV = TrinomialTree.AmericanFutureOptionPV(f, k, rf, t, vol, cp);
            Assert.True(PV >= PVBlack);
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

            var PVbi = BinomialTree.AmericanFutureOptionPV(f, k, rf, t, vol, cp);
            var PVtri = TrinomialTree.AmericanFutureOptionPV(f, k, rf, t, vol, cp);

            var impliedVolBi = BinomialTree.AmericanFuturesOptionImpliedVol(f, k, rf, t, PVbi, cp);
            var impliedVolTri = TrinomialTree.AmericanFuturesOptionImpliedVol(f, k, rf, t, PVtri, cp);

            Assert.Equal(vol, impliedVolBi, 10);
            Assert.Equal(vol, impliedVolTri, 10);
        }
    }
}
