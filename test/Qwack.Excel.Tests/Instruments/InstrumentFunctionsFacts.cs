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

        [Fact]
        public void CreateAsianCrackDiffSwap()
        {
            Assert.Equal("Calendar xxxP not found in cache",
                InstrumentFunctions.CreateAsianCrackDiffSwap("cswp", "Jan-19", "xxP", "xxR", "ZAR", 0.0, 0.0, 0.0, "xxxP", "xxxR", "xxxPay", "2b", "2b", "2b", "disco"));
            Assert.Equal("Calendar xxxR not found in cache",
                InstrumentFunctions.CreateAsianCrackDiffSwap("cswp", "Jan-19", "xxP", "xxR", "ZAR", 0.0, 0.0, 0.0, "NYC", "xxxR", "xxxPay", "2b", "2b", "2b", "disco"));
            Assert.Equal("Calendar xxxPay not found in cache",
                InstrumentFunctions.CreateAsianCrackDiffSwap("cswp", "Jan-19", "xxP", "xxR", "ZAR", 0.0, 0.0, 0.0, "NYC", "NYC", "xxxPay", "2b", "2b", "2b", "disco"));
            Assert.Equal("cswp¬0",
                InstrumentFunctions.CreateAsianCrackDiffSwap("cswp", "Jan-19", "xxP", "xxR", "ZAR", 0.0, 0.0, 0.0, "NYC", "NYC", "NYC", "2b", "2b", "2b", "disco"));
            Assert.Equal("ccswp¬0",
                InstrumentFunctions.CreateAsianCrackDiffSwap("ccswp", (new DateTime(2019, 1, 1)).ToOADate(), "xxP", "xxR", "ZAR", 0.0, 0.0, 0.0, "NYC", "NYC", "NYC", "2b", "2b", "2b", "disco"));
        }


        [Fact]
        public void CreateFuturesCrackDiffSwap()
        {
            Assert.Equal("fdswap¬0",
               InstrumentFunctions.CreateFutureCrackDiffSwap("fdswap", "COF9", "QSF9", "CO", "QS", "USD", -19, 1000, 7460, "DISCO"));
        }

        [Fact]
        public void CreateFuturesPositionSwap()
        {
            Assert.Equal("futXXX¬0",
               InstrumentFunctions.CreateFuture("futXXX", DateTime.Today, "CO", "USD", 74, 1000, 1000, 1.0));
        }

        [Fact]
        public void CreateFutureOptionPositionSwap()
        {
            Assert.Equal("Could not parse call/put flag xyxy",
                InstrumentFunctions.CreateFutureOption("futOptXXX", DateTime.Today, "CO", "USD", 74, 1000, 1000, "xyxy", "xxxxx", "xxxx"));
            Assert.Equal("Could not parse option style flag xxxxx",
                InstrumentFunctions.CreateFutureOption("futOptXXX", DateTime.Today, "CO", "USD", 74, 1000, 1000, "C", "xxxxx", "xxxx"));
            Assert.Equal("Could not parse margining type flag xxxx",
                InstrumentFunctions.CreateFutureOption("futOptXXX", DateTime.Today, "CO", "USD", 74, 1000, 1000, "C", "European", "xxxx"));
            Assert.Equal("futOptXXX¬0",
                InstrumentFunctions.CreateFutureOption("futOptXXX", DateTime.Today, "CO", "USD", 74, 1000, 1000, "C", "European", "FuturesStyle"));
        }

        [Fact]
        public void CreateMonthlyAsianSwapFact()
        {
            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateMonthlyAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateMonthlyAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Could not parse date generation type - sqw",
                InstrumentFunctions.CreateMonthlyAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", "sqw", "disco"));

            Assert.Equal("swppp¬0",
                InstrumentFunctions.CreateMonthlyAsianSwap("swppp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("swppq¬0",
                InstrumentFunctions.CreateMonthlyAsianSwap("swppq", DateTime.Today.ToOADate(), "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("swppz¬0",
                InstrumentFunctions.CreateMonthlyAsianSwap("swppz", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.AddDays(100).ToOADate() } }, "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", "BusinessDays", "disco"));
        }

        [Fact]
        public void CreateCustomAsianSwapFact()
        {
            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateCustomAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, new[] { 0.0 }, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateCustomAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, new[] { 0.0 }, "NYC", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Could not parse date generation type - sqw",
                InstrumentFunctions.CreateCustomAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, new[] { 0.0 }, "NYC", "NYC", "2b", "2b", "sqw", "disco"));

            Assert.Equal("Expecting a Nx2 array of period dates",
                InstrumentFunctions.CreateCustomAsianSwap("swppp", "Jan-19", "xx", "ZAR", 0.0, new[] { 0.0 }, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("Number of notionals must match number of periods",
                InstrumentFunctions.CreateCustomAsianSwap("swppp", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.ToOADate() } }, "xx", "ZAR", 0.0, new[] { 0.0, 0.0 }, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("swppp¬0",
                InstrumentFunctions.CreateCustomAsianSwap("swppp", new object[,] { {DateTime.Today.ToOADate(), DateTime.Today.ToOADate() } }, "xx", "ZAR", 0.0, new[] { 0.0 }, "NYC", "NYC", "2b", "2b", Value, "disco"));
        }

        [Fact]
        public void CreateAsianOptionFact()
        {
            Assert.Equal("Could not parse put/call flag - px",
                InstrumentFunctions.CreateAsianOption("swp", "Jan-19", "xx", "ZAR", 0.0, "px", 0.0, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateAsianOption("swp", "Jan-19", "xx", "ZAR", 0.0, "P", 0.0, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateAsianOption("swp", "Jan-19", "xx", "ZAR", 0.0, "P", 0.0, "NYC", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Could not parse date generation type - zzzz",
                InstrumentFunctions.CreateAsianOption("swp", "Jan-19", "xx", "ZAR", 0.0, "P", 0.0, "NYC", "NYC", "2b", "2b", "zzzz", "disco"));

            Assert.Equal("opttt¬0",
                InstrumentFunctions.CreateAsianOption("opttt", "Jan-19", "xx", "ZAR", 0.0, "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

        }
    }
}
