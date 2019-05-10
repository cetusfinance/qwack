using System;
using System.Collections.Generic;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Providers.Json;
using Xunit;

namespace Qwack.Dates.Tests
{
    public class CalendarFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");

        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void LoadsJsonFileAndHasCalendars() => Assert.True(CalendarProvider.OriginalCalendars.Count > 100);

        [Fact]
        public void CheckUSDCalendarHasHolidayOnJuly4th()
        {
            Assert.True(CalendarProvider.Collection.TryGetCalendar("nyc", out var calendar));

            Assert.True(calendar.IsHoliday(new DateTime(2016, 07, 04)));
        }

        [Fact]
        public void CheckUSDCalendarHasWeekendAsHolidays()
        {
            Assert.True(CalendarProvider.Collection.TryGetCalendar("nyc", out var calendar));

            Assert.True(calendar.IsHoliday(new DateTime(2016, 07, 03)));
        }

        [Theory]
        [MemberData(nameof(GetUSExclusiveHolidays))]
        public void CheckCombinedCalendarHasJuly4th(DateTime dateToCheck)
        {
            CalendarProvider.Collection.TryGetCalendar("nyc", out var us);
            CalendarProvider.Collection.TryGetCalendar("lon", out var gb);
            CalendarProvider.Collection.TryGetCalendar("lon+nyc", out var combined);

            Assert.True(us.IsHoliday(dateToCheck));
            Assert.False(gb.IsHoliday(dateToCheck));
            Assert.True(combined.IsHoliday(dateToCheck));
        }

        [Fact]
        public void CheckThatClonedCalendarIsEqualButNotTheSame()
        {
            CalendarProvider.Collection.TryGetCalendar("nyc", out var usd);
            var clone = usd.Clone();

            Assert.NotSame(usd.DaysToExclude, clone.DaysToExclude);
            Assert.Equal(usd.DaysToExclude, clone.DaysToExclude);
        }

        public static IEnumerable<object[]> GetUSExclusiveHolidays()
        {
            var holidays = new List<object[]>()
            {
                new object[] { new DateTime(2016,07,04) }
            };

            return holidays;
        }

        [Fact]
        public void RuleBased_ZARule()
        {
            var calendar = new Calendar
            {
                CalendarType = CalendarType.FixedDateZARule,
                FixedDate = new DateTime(2000, 07, 07),
                ValidFromYear = 1994,
                ValidToYear = 2020
            };

            Assert.False(calendar.IsHoliday(new DateTime(2019, 07, 07)));
            Assert.False(calendar.IsHoliday(new DateTime(2019, 07, 06)));
            Assert.True(calendar.IsHoliday(new DateTime(2019, 07, 08)));

            Assert.True(calendar.IsHoliday(new DateTime(2020, 07, 07)));
            Assert.False(calendar.IsHoliday(new DateTime(2021, 07, 07)));
            Assert.False(calendar.IsHoliday(new DateTime(1993, 07, 07)));

            calendar.FalsePositives.Add(new DateTime(2019, 07, 08));
            Assert.False(calendar.IsHoliday(new DateTime(2019, 07, 08)));

        }

        [Fact]
        public void RuleBased_Easter()
        {
            var calendarGF = new Calendar
            {
                CalendarType = CalendarType.EasterGoodFriday,
            };
            var calendarEM = new Calendar
            {
                CalendarType = CalendarType.EasterMonday,
            };

            Assert.False(calendarGF.IsHoliday(new DateTime(2019, 07, 07)));
            Assert.False(calendarEM.IsHoliday(new DateTime(2019, 07, 07)));

            Assert.True(calendarGF.IsHoliday(new DateTime(2019, 04, 19)));
            Assert.True(calendarEM.IsHoliday(new DateTime(2019, 04, 22)));
        }

        [Fact]
        public void Merged_Calendar()
        {
            var calendarA = new Calendar();
            var calendarB = new Calendar();

            calendarA.DaysToExclude.Add(new DateTime(2019, 07, 07));
            calendarB.DaysToExclude.Add(new DateTime(2019, 07, 08));

            calendarA.MonthsToExclude.Add(MonthEnum.Dec);
            calendarB.MonthsToExclude.Add(MonthEnum.Dec);
            calendarB.MonthsToExclude.Add(MonthEnum.Jan);

            var calendarC = calendarA.Merge(calendarB);

            Assert.False(calendarC.IsHoliday(new DateTime(2019, 07, 06)));
            Assert.True(calendarC.IsHoliday(new DateTime(2019, 07, 07)));
            Assert.True(calendarC.IsHoliday(new DateTime(2019, 07, 08)));

            Assert.True(calendarC.IsHoliday(new DateTime(2019, 12, 07)));
            Assert.True(calendarC.IsHoliday(new DateTime(2019, 01, 07)));

            Assert.False(CalendarA.Equals(CalendarB));
            Assert.True(CalendarA.Equals(CalendarA));
        }
    }
}
