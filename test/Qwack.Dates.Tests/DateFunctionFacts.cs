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
        private static readonly Calendar WeekendsOnly = new Calendar() { DaysToAlwaysExclude = new List<DayOfWeek>() { DayOfWeek.Saturday, DayOfWeek.Sunday } };

        [Fact]
        public void BusinessDaysInPeriod()
        {
            var startDate = new DateTime(2016, 02, 10);
            var endDate = new DateTime(2016, 10, 13);
            Assert.Equal(247, startDate.BusinessDaysInPeriod(endDate, EmptyCalendar).Count());

            var noWeekends = startDate.BusinessDaysInPeriod(endDate, WeekendsOnly);
            Assert.Equal(177, noWeekends.Count);
            Assert.Equal(0, noWeekends.Count(d => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday));
        }

        [Fact]
        public void YearFractionSingleYear()
        {
            var startDate = new DateTime(2016, 02, 10);
            var endDate = new DateTime(2016, 10, 13);
            Assert.Equal(246,startDate.CalculateYearFraction(endDate, DayCountBasis.ACT360) * 360,15);
        }

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
        public void FirstDayOfMonth()
        {
            var dt = new DateTime(2016, 10, 10);
            Assert.Equal(new DateTime(2016, 10, 1), dt.FirstDayOfMonth());
        }

        [Fact]
        public void LastDayOfMonthDec()
        {
            var dt = new DateTime(2016, 12, 10, 12, 10, 10);
            Assert.Equal(new DateTime(2016, 12, 31), dt.LastDayOfMonth());
        }

        [Fact]
        public void LastDayOfMonthJan()
        {
            var dt = new DateTime(2016, 01, 10, 12, 10, 10);
            Assert.Equal(new DateTime(2016, 01, 31), dt.LastDayOfMonth());
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

        [Fact]
        public void NthSpecificWeekdaySameDay()
        {
            var dt = new DateTime(2016, 11, 1);
            Assert.Equal(new DateTime(2016, 11, 22), dt.NthSpecificWeekDay(DayOfWeek.Tuesday, 4));
        }

        [Fact]
        public void NthSpecificWeekdayDayBefore()
        {
            var dt = new DateTime(2016, 11, 1);
            Assert.Equal(new DateTime(2016, 11, 21), dt.NthSpecificWeekDay(DayOfWeek.Monday, 3));
        }

        [Fact]
        public void NthSpecificWeekdayDayAfter()
        {
            var dt = new DateTime(2016, 11, 1);
            Assert.Equal(new DateTime(2016, 11, 11), dt.NthSpecificWeekDay(DayOfWeek.Friday, 2));
        }

        [Fact]
        public void ThirdWednesday()
        {
            var dt = new DateTime(2016, 11, 20, 10, 20, 10);
            Assert.Equal(new DateTime(2016, 11, 16), dt.ThirdWednesday());
        }
    }
}
