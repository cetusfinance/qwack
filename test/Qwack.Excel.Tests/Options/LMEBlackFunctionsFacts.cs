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
    public class LMEBlackFunctionsFacts
    {

        [Fact]
        public void LMEBlackPV_Facts()
        {
            Assert.Equal("Could not parse call or put flag - blah", LMEFunctions.QOpt_LMEBlackPV(DateTime.Today, DateTime.Today, DateTime.Today, 1, 1, 1.0, 1, "blah"));
            Assert.Equal(0.0, LMEFunctions.QOpt_LMEBlackPV(DateTime.Today, DateTime.Today, DateTime.Today, 1, 1, 1.0, 1, "C"));
        }

        [Fact]
        public void BlackDelta_Facts()
        {
            Assert.Equal("Could not parse call or put flag - blah", LMEFunctions.QOpt_LMEBlackDelta(DateTime.Today, DateTime.Today,1.0, 1, 1, "blah"));
            Assert.Equal(0.0, LMEFunctions.QOpt_LMEBlackDelta(DateTime.Today, DateTime.Today, 1.0, 1, 1, "C"));
        }

        [Fact]
        public void BlackGammaVega_Facts()
        {
            Assert.Equal(0.0, LMEFunctions.QOpt_LMEBlackGamma(DateTime.Today, DateTime.Today.AddDays(1),  1.0, 0.5, 0.001));
            Assert.Equal(0.0, LMEFunctions.QOpt_LMEBlackVega(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(1), 1.0, 0.5, 0.0, 0.001));
        }

        [Fact]
        public void BlackImpliedVol_Facts()
        {
            Assert.Equal("Could not parse call or put flag - blah", LMEFunctions.QOpt_LMEBlackImpliedVol(DateTime.Today, DateTime.Today, DateTime.Today, 1.0, 1.0, 1, 1, "blah"));
            Assert.Equal(1e-9, LMEFunctions.QOpt_LMEBlackImpliedVol(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(1), 1.0, 0.5, 0.0, 0.0, "C"));
        }
    }
}
