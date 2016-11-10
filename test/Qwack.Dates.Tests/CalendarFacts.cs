using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Dates.Providers;
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
            Assert.True(CalendarProvider.Collection.TryGetCalendar("usd", out calendar));

            Assert.True(calendar.IsHoliday(new DateTime(2015, 07, 04)));
        }

        [Fact]
        public void CheckUSDCalendarHasWeekendAsHolidays()
        {
            Calendar calendar;
            Assert.True(CalendarProvider.Collection.TryGetCalendar("usd", out calendar));

            Assert.True(calendar.IsHoliday(new DateTime(2015, 07, 03)));
        }

        [Theory]
        [MemberData("GetUSExclusiveHolidays")]
        public void CheckCombinedCalendarHasJuly4th(DateTime dateToCheck)
        {
            Calendar us, gb, combined;
            CalendarProvider.Collection.TryGetCalendar("usd", out us);
            CalendarProvider.Collection.TryGetCalendar("gbp", out gb);
            CalendarProvider.Collection.TryGetCalendar("gbp+usd", out combined);

            Assert.True(us.IsHoliday(dateToCheck));
            Assert.False(gb.IsHoliday(dateToCheck));
            Assert.True(combined.IsHoliday(dateToCheck));
        }

        public IEnumerable<DateTime> GetUSExclusiveHolidays()
        {
            List<DateTime> holidays = new List<DateTime>()
            {
                new DateTime(2015,07,04)
            };

            return holidays;
        }
    }
}
