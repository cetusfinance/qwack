using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Qwack.Dates;
using Qwack.Excel.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Providers.Json;

namespace Qwack.Excel.Tests.Options
{
    public class BlackFunctionsFacts
    {

        [Fact]
        public void BlackPV_Facts()
        {
            Assert.Equal("Could not parse call or put flag - blah", BlackFunctions.BlackPV(1.0, 1.0, 1, 1, 1, "blah"));
            Assert.Equal(0.0, BlackFunctions.BlackPV(0.1, 1.0, 0.5, 0.0, 0.0, "C"));
        }

        [Fact]
        public void BlackDelta_Facts()
        {
            Assert.Equal("Could not parse call or put flag - blah", BlackFunctions.BlackDelta(1.0, 1.0, 1, 1, 1, "blah"));
            Assert.Equal(0.0, BlackFunctions.BlackDelta(0.1, 1.0, 0.5, 0.0, 0.0, "C"));
        }

        [Fact]
        public void BlackGammaVega_Facts()
        {
            Assert.Equal(0.0, BlackFunctions.BlackGamma(0.1, 1.0, 0.5, 0.0, 0.001));
            Assert.Equal(0.0, BlackFunctions.BlackVega(0.1, 1.0, 0.5, 0.0, 0.001));
        }

        [Fact]
        public void BlackImpliedVol_Facts()
        {
            Assert.Equal("Could not parse call or put flag - blah", BlackFunctions.BlackImpliedVol(1.0, 1.0, 1, 1, 1, "blah"));
            Assert.Equal(1e-9, BlackFunctions.BlackImpliedVol(0.1, 1.0, 0.5, 0.0, 0.0, "C"));
        }

        [Fact]
        public void AbsKFromDelta_Facts()
        {
            var vol = 0.32;
            var fwd = 100.0;
            var k = 115.0;
            var t = 1.5;
            var deltaK = Qwack.Options.BlackFunctions.BlackDelta(fwd, k, 0, t, vol, Core.Basic.OptionType.C);
            Assert.Equal(k,(double)BlackFunctions.AbsoluteStrikeFromDelta(t,deltaK,fwd,vol,0), 12);
        }
    }
}
