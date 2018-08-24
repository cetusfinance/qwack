using System;
using System.Collections.Generic;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Futures;
using Qwack.Providers.Json;
using Xunit;

namespace Qwack.Dates.Tests
{
    public class ListedFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly string JsonFuturesPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "futuresettings.json");

        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        public static readonly IFutureSettingsProvider futureSettingsProvider = new FutureSettingsFromJson(CalendarProvider, JsonFuturesPath);

        [Theory]
        [MemberData(nameof(GetFutureCodeExamplesSpecificRefDate))]
        public void CheckCodeExamplesSpecificRefDate(string futureCode, DateTime expiryMonth, DateTime refDate)
        {
            var date = ListedUtils.FuturesCodeToDateTime(futureCode, refDate);
            Assert.Equal(expiryMonth, date);
        }

        public static IEnumerable<object[]> GetFutureCodeExamplesSpecificRefDate()
        {
            var examples = new List<object[]>()
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

        [Theory]
        [MemberData(nameof(GetNextFutureCodeExamples))]
        public void CheckNextCodeExamples(string futureCode, string nextCode)
        {
            var code = new FutureCode(futureCode, 2016, futureSettingsProvider);

            Assert.Equal(nextCode, code.GetNextCode(false));
        }

        [Theory]
        [MemberData(nameof(GetNextFutureCodeExamples))]
        public void CheckPreviousCodeExamples(string previousCode, string futureCode)
        {
            var code = new FutureCode(futureCode, 2016, futureSettingsProvider);

            Assert.Equal(previousCode, code.GetPreviousCode());
        }

        [Theory]
        [MemberData(nameof(ExpiryRollForFutureCodeExamples))]
        public void CheckExpiryRollForCodeExamples(string futureCode, DateTime expiry, DateTime roll)
        {
            var code = new FutureCode(futureCode, 2016, futureSettingsProvider);

            Assert.Equal(expiry, code.GetExpiry());
            Assert.Equal(roll, code.GetRollDate());
        }

        public static IEnumerable<object[]> GetNextFutureCodeExamples()
        {
            var examples = new List<object[]>()
            {
                new object[] { "CLZ7", "CLF8" },
                new object[] { "EDU8", "EDZ8"},
                new object[] { "C N9","C U9" },
                new object[] { "W Z9","W H0" },
            };

            return examples;
        }

        public static IEnumerable<object[]> ExpiryRollForFutureCodeExamples()
        {
            var examples = new List<object[]>()
            {
                new object[] { "CLV8", new DateTime(2018,09,20),new DateTime(2018,09,20) },
                new object[] { "EDU8", new DateTime(2018,09,17),new DateTime(2018,09,14) },
                new object[] { "C Z8", new DateTime(2018,12,14),new DateTime(2018,11,27) },
            };

            return examples;
        }

    }
}
