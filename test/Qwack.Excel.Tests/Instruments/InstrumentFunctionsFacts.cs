using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Excel.Instruments;
using Xunit;
using static ExcelDna.Integration.ExcelMissing;

namespace Qwack.Excel.Tests.Instruments
{
    public class InstrumentFunctionsFacts
    {
        [Fact]
        public void CreateAsianSwapFact()
        {
            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Could not parse date generation type - sqw",
                InstrumentFunctions.CreateAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", "sqw", "disco"));

            Assert.Equal("swppp¬0",
                InstrumentFunctions.CreateAsianSwap("swppp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("swppq¬0",
                InstrumentFunctions.CreateAsianSwap("swppq", DateTime.Today.ToOADate(), "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("swppz¬0",
                InstrumentFunctions.CreateAsianSwap("swppz", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.AddDays(100).ToOADate() } }, "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", "BusinessDays", "disco"));
        }
    }
}
