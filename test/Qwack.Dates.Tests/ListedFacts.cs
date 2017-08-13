using System;
using System.Collections.Generic;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Json.Providers;
using Xunit;

namespace Qwack.Dates.Tests
{
    public class ListedFacts
    {
        [Theory]
        [MemberData("GetFutureCodeExamplesSpecificRefDate")]
        public void CheckCodeExamplesSpecificRefDate(string futureCode, DateTime expiryMonth, DateTime refDate)
        {
            var date = ListedUtils.FuturesCodeToDateTime(futureCode, refDate);
            Assert.Equal(expiryMonth, date);
        }

        public static IEnumerable<object> GetFutureCodeExamplesSpecificRefDate()
        {
            var examples = new List<object>()
            {
                new object[] { "CLZ7", new DateTime(2017,12,01), new DateTime(2017,01,17) },
                new object[] { "CLZ17", new DateTime(2017,12,01) , new DateTime(2017,01,17) },
                new object[] { "VeryLongNameZ17", new DateTime(2017,12,01) , new DateTime(2017,01,17) },
                new object[] { "QWACKN6", new DateTime(2026,07,01) , new DateTime(2017,01,17) },
                new object[] { "QWACKF9", new DateTime(2019,01,01) , new DateTime(2017,01,17) },
                new object[] { "CLZ7", new DateTime(2027,12,01), new DateTime(2018,01,17) }, //test single digit date logic
                new object[] { "CLZ87", new DateTime(1987,12,01), new DateTime(1999,01,17) },
                new object[] { "CLZ87", new DateTime(2087,12,01), new DateTime(2000,01,17) },
            };

            return examples;
        }
    }
}
