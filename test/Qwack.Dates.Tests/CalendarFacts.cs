using System;
using System.Collections.Generic;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Json.Providers;
using Xunit;

namespace Qwack.Dates.Tests
{
    public class CalendarFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");

        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void LoadsJsonFileAndHasCalendars()
        {
            Assert.True(CalendarProvider.OriginalCalendars.Count > 100);
        }

        [Fact]
        public void CheckUSDCalendarHasHolidayOnJuly4th()
        {
            Assert.True(CalendarProvider.Collection.TryGetCalendar("nyc", out Calendar calendar));

            Assert.True(calendar.IsHoliday(new DateTime(2016, 07, 04)));
        }

        [Fact]
        public void CheckUSDCalendarHasWeekendAsHolidays()
        {
            Assert.True(CalendarProvider.Collection.TryGetCalendar("nyc", out Calendar calendar));

            Assert.True(calendar.IsHoliday(new DateTime(2016, 07, 03)));
        }

        [Theory]
        [MemberData("GetUSExclusiveHolidays")]
        public void CheckCombinedCalendarHasJuly4th(DateTime dateToCheck)
        {
            CalendarProvider.Collection.TryGetCalendar("nyc", out Calendar us);
            CalendarProvider.Collection.TryGetCalendar("lon", out Calendar gb);
            CalendarProvider.Collection.TryGetCalendar("lon+nyc", out Calendar combined);

            Assert.True(us.IsHoliday(dateToCheck));
            Assert.False(gb.IsHoliday(dateToCheck));
            Assert.True(combined.IsHoliday(dateToCheck));
        }

        [Fact]
        public void CheckThatClonedCalendarIsEqualButNotTheSame()
        {
            CalendarProvider.Collection.TryGetCalendar("nyc", out Calendar usd);
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
