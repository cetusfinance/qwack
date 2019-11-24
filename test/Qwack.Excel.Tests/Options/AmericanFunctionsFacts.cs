using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Qwack.Dates;
using Qwack.Excel.Options;
using Qwack.Providers.Json;

namespace Qwack.Excel.Tests.Options
{
    public class AmericanFunctionsFacts
    {

        [Fact]
        public void AmericanFutureOptionPV_Facts()
        { 
            Assert.Equal("Could not parse call or put flag - blah", AmericanFunctions.AmericanFutureOptionPV(1.0,1.0,1,1,1,"blah","pwah"));
            Assert.Equal("Could not parse pricing type - pwah", AmericanFunctions.AmericanFutureOptionPV(1.0, 1.0, 1, 1, 1, "C", "pwah"));

            Assert.Equal(0.0, AmericanFunctions.AmericanFutureOptionPV(0.1, 1.0, 0.5, 0.0, 0.0, "C", "Binomial"));
            Assert.Equal(0.0, AmericanFunctions.AmericanFutureOptionPV(0.1, 1.0, 0.5, 0.0, 0.0, "C", "Trinomial"));
        }

        [Fact]
        public void AmericanFutureOptionImpliedVol_Facts()
        {
            Assert.Equal("Could not parse call or put flag - blah", AmericanFunctions.AmericanFutureOptionImpliedVol(1.0, 1.0, 1, 1, 1, "blah", "pwah"));
            Assert.Equal("Could not parse pricing type - pwah", AmericanFunctions.AmericanFutureOptionImpliedVol(1.0, 1.0, 1, 1, 1, "C", "pwah"));

            Assert.Equal(1e-9, AmericanFunctions.AmericanFutureOptionImpliedVol(0.1, 1.0, 0.5, 0.0, 0.0, "C", "Binomial"));
            Assert.Equal(1e-9, AmericanFunctions.AmericanFutureOptionImpliedVol(0.1, 1.0, 0.5, 0.0, 0.0, "C", "Trinomial"));
        }
    }
}
