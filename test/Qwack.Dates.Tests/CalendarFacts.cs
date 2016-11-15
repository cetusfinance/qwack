using System;
using System.Collections.Generic;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Json.Providers;
using Xunit;

namespace Qwack.Dates.Tests
{
    public class CalendarFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "data", "Calendars.json");

        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void LoadsJsonFileAndHasCalendars()
        {
            Assert.True(CalendarProvider.OriginalCalendars.Count > 100);
        }

        [Fact]
        public void CheckUSDCalendarHasHolidayOnJuly4th()
        {
            Calendar calendar;
            Assert.True(CalendarProvider.Collection.TryGetCalendar("nyc", out calendar));

            Assert.True(calendar.IsHoliday(new DateTime(2016, 07, 04)));
        }

        [Fact]
        public void CheckUSDCalendarHasWeekendAsHolidays()
        {
            Calendar calendar;
            Assert.True(CalendarProvider.Collection.TryGetCalendar("nyc", out calendar));

            Assert.True(calendar.IsHoliday(new DateTime(2016, 07, 03)));
        }

        [Theory]
        [MemberData("GetUSExclusiveHolidays")]
        public void CheckCombinedCalendarHasJuly4th(DateTime dateToCheck)
        {
            Calendar us, gb, combined;
            CalendarProvider.Collection.TryGetCalendar("nyc", out us);
            CalendarProvider.Collection.TryGetCalendar("lon", out gb);
            CalendarProvider.Collection.TryGetCalendar("lon+nyc", out combined);

            Assert.True(us.IsHoliday(dateToCheck));
            Assert.False(gb.IsHoliday(dateToCheck));
            Assert.True(combined.IsHoliday(dateToCheck));
        }

        [Fact]
        public void CheckThatClonedCalendarIsEqualButNotTheSame()
        {
            Calendar usd;
            CalendarProvider.Collection.TryGetCalendar("nyc", out usd);
            var clone = usd.Clone();

            Assert.NotSame(usd.DaysToExclude, clone.DaysToExclude);
            Assert.Equal(usd.DaysToExclude, clone.DaysToExclude);
        }

        public static IEnumerable<object> GetUSExclusiveHolidays()
        {
            List<object> holidays = new List<object>()
            {
                new object[] { new DateTime(2016,07,04) }
            };

            return holidays;
        }
    }
}
