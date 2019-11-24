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
    public class AsianFunctionsFacts
    {


        [Fact]
        public void TurnbullWakemanPV_Facts()
        {
            Assert.Equal("Could not parse call or put flag - blah", AsianFunctions.TurnbullWakemanPV(0,0,0,0,0,0,0,"blah"));
            Assert.Equal(0.0, AsianFunctions.TurnbullWakemanPV(1.0, 0.5, 0, 1.0, 0.5, 0, 0.0001, "C"));

            Assert.Equal("Could not parse call or put flag - blah", AsianFunctions.TurnbullWakemanPV2(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 0, 0, 0, 0,0, "blah"));
            Assert.Equal(0.0, AsianFunctions.TurnbullWakemanPV2(DateTime.Today, DateTime.Today.AddDays(200), DateTime.Today.AddDays(365), 0, 100.0, 0.0, 0, 0.00001, "C"));
        }

        [Fact]
        public void TurnbullWakemanDelta_Facts()
        {
            Assert.Equal("Could not parse call or put flag - blah", AsianFunctions.TurnbullWakemanDelta(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 0, 0, 0, 0, 0, "blah"));
            Assert.Equal(0.0, AsianFunctions.TurnbullWakemanDelta(DateTime.Today, DateTime.Today.AddDays(200), DateTime.Today.AddDays(365), 0, 100.0, 0.0, 0, 0.00001, "C"));
        }

        [Fact]
        public void ClewlowPV_Facts()
        {
            var missing = ExcelDna.Integration.ExcelMissing.Value;
            Assert.Equal("Calendar pwah not found in cache", AsianFunctions.ClewlowPV(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 0, 0, 0, 0, 0, "blah", "pwah"));
            Assert.Equal("Could not parse call or put flag - blah", AsianFunctions.ClewlowPV(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 0, 0, 0, 0, 0, "blah", missing));
            Assert.Equal("Could not parse call or put flag - blah", AsianFunctions.ClewlowPV(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 0, 0, 0, 0, 0, "blah", "Weekends"));

            Assert.Equal(0.0, AsianFunctions.ClewlowPV(DateTime.Today, DateTime.Today.AddDays(200), DateTime.Today.AddDays(365), 0, 100.0, 0.0, 0, 0.00001, "C", "NYC"));
        }

        [Fact]
        public void ClewlowDelta_Facts()
        {
            var missing = ExcelDna.Integration.ExcelMissing.Value;
            Assert.Equal("Calendar pwah not found in cache", AsianFunctions.ClewlowDelta(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 0, 0, 0, 0, 0, "blah", "pwah"));
            Assert.Equal("Could not parse call or put flag - blah", AsianFunctions.ClewlowDelta(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 0, 0, 0, 0, 0, "blah", missing));
            Assert.Equal("Could not parse call or put flag - blah", AsianFunctions.ClewlowDelta(DateTime.Today, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), 0, 0, 0, 0, 0, "blah", "Weekends"));

            Assert.Equal(0.0, AsianFunctions.ClewlowDelta(DateTime.Today, DateTime.Today.AddDays(200), DateTime.Today.AddDays(365), 0, 100.0, 0.0, 0, 0.00001, "C", "NYC"));
        }
    }
}
