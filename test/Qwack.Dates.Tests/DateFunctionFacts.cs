using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Qwack.Dates.Tests
{
    public class DateFunctionFacts
    {
        private static readonly Calendar EmptyCalendar = new Calendar();

        [Fact]
        public void FirstBusinessDayOfTheMonthIgnoresTime()
        {
            var dt = new DateTime(2016, 10, 10, 11, 54, 30);
            Assert.Equal(new DateTime(2016, 10, 1), dt.FirstBusinessDayOfMonth(EmptyCalendar));
        }

        [Fact]
        public void FirstBusinessDayOfTheMonthRespectsHolidaysAndWeekends()
        {
            var calendar = new Calendar();
            calendar.DaysToAlwaysExclude.Add(DayOfWeek.Saturday);
            calendar.DaysToAlwaysExclude.Add(DayOfWeek.Sunday);
            calendar.DaysToExclude.Add(new DateTime(2016, 07, 01));

            var dt = new DateTime(2016, 07, 20);

            Assert.Equal(new DateTime(2016, 07, 04), dt.FirstBusinessDayOfMonth(calendar));
        }

        [Fact]
        public void LastBusinessDayOfTheMonthIgnoresTime()
        {
            var dt = new DateTime(2016, 10, 10, 11, 54, 30);
            Assert.Equal(new DateTime(2016, 10, 31), dt.LastBusinessDayOfMonth(EmptyCalendar));
        }

        [Fact]
        public void LastBusinessDayOfTheMonthRespectsHolidaysAndWeekends()
        {
            var calendar = new Calendar();
            calendar.DaysToAlwaysExclude.Add(DayOfWeek.Saturday);
            calendar.DaysToAlwaysExclude.Add(DayOfWeek.Sunday);
            calendar.DaysToExclude.Add(new DateTime(2016, 10, 31));

            var dt = new DateTime(2016, 10, 10);
            Assert.Equal(new DateTime(2016, 10, 28), dt.LastBusinessDayOfMonth(calendar));
        }
    }
}
