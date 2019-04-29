using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;
using Qwack.Core.Basic;

namespace Qwack.Math.Tests.Options
{
    public class AmericanOptionFacts
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
            var PV = TrinomialTree.AmericanAssetOptionPV(f, k, rf, f, t, vol, cp);
            Assert.Equal(0, PV, 10);
            PV = BinomialTree.AmericanPV(t, f, k, rf, vol, cp, rf, 100);
            Assert.Equal(0, PV, 10);

            //zero strike call is worth fwd with no discounting
            cp = OptionType.C;
            PV = TrinomialTree.AmericanAssetOptionPV(f, k, rf, f, t, vol, cp);
            Assert.Equal(f, PV, 10);
            PV = BinomialTree.AmericanPV(t, f, k, rf, vol, cp, rf, 100);
            Assert.Equal(f, PV, 10);

            //OTM option with zero vol is worthless
            vol = 0.0;
            k = f + 1;
            PV = BinomialTree.AmericanPV(t, f, k, rf, vol, cp, rf, 100);
            Assert.Equal(0, PV, 10);
            PV = TrinomialTree.AmericanAssetOptionPV(f, k, rf, f, t, vol, cp);
            Assert.Equal(0, PV, 10);

            //option worth >= black in all cases for same inputs
            vol = 0.32;
            k = f + 20;
            var PVBlack = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            PV = BinomialTree.AmericanPV(t, f, k, rf, vol, cp, rf, 100);
            Assert.True(PV >= PVBlack);
            PV = TrinomialTree.AmericanAssetOptionPV(f, k, rf, f, t, vol, cp);
            Assert.True(PV >= PVBlack);
        }

        [Fact]
        public void EuropeanOnGridFacts()
        {
            var t = 1.0;
            var k = 120;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;

            var PVBlack = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            var PVbi = BinomialTree.EuropeanPV(t, f, k, rf, vol, cp, rf, 100);
            var PVtri = TrinomialTree.EuropeanPV(t, f, k, rf, vol, cp, rf, 100);

            Assert.Equal(PVBlack, PVbi, 1);
            Assert.Equal(PVBlack, PVtri, 1);

            PVbi = BinomialTree.EuropeanFuturePV(t, f, k, rf, vol, cp, 100);
            PVtri = TrinomialTree.EuropeanFuturePV(t, f, k, rf, vol, cp, 100);

            Assert.Equal(PVBlack, PVbi, 1);
            Assert.Equal(PVBlack, PVtri, 1);
        }
    }
}
