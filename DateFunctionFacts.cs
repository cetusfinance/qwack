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
            Assert.Equal(247, startDate.BusinessDaysInPeriod(endDate, EmptyCalendar).Count);

            var noWeekends = startDate.BusinessDaysInPeriod(endDate, WeekendsOnly);
            Assert.Equal(177, noWeekends.Count);
            Assert.Equal(0, noWeekends.Count(d => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday));
        }

        [Fact]
        public void YearFractionSingleYear()
        {
            var startDate = new DateTime(2016, 02, 10);
            var endDate = new DateTime(2016, 10, 13);
            Assert.Equal(246, startDate.CalculateYearFraction(endDate, DayCountBasis.Act360) * 360, 15);
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
        public void NthLastSpecificWeekdaySameDay()
        {
            var dt = new DateTime(2018, 7, 1);
            Assert.Equal(new DateTime(2018, 07, 31), dt.NthLastSpecificWeekDay(DayOfWeek.Tuesday, 1));
        }

        [Fact]
        public void NthLastSpecificWeekdayDayBefore()
        {
            var dt = new DateTime(2018, 7, 1);
            Assert.Equal(new DateTime(2018, 07, 17), dt.NthLastSpecificWeekDay(DayOfWeek.Tuesday, 3));
        }

        [Fact]
        public void NthLastSpecificWeekdayDayAfter()
        {
            var dt = new DateTime(2018, 7, 1);
            Assert.Equal(new DateTime(2018, 7, 20), dt.NthLastSpecificWeekDay(DayOfWeek.Friday, 2));
        }

        [Fact]
        public void ThirdWednesday()
        {
            var dt = new DateTime(2016, 11, 20, 10, 20, 10);
            Assert.Equal(new DateTime(2016, 11, 16), dt.ThirdWednesday());
        }

        [Fact]
        public void MinMax()
        {
            var dtA = new DateTime(2016, 11, 20);
            var dtB = new DateTime(2017, 11, 20);

            Assert.Equal(dtA, dtA.Min(dtB));
            Assert.Equal(dtB, dtB.Max(dtA));
        }

        [Fact]
        public void Average()
        {
            var dtA = new DateTime(2016, 11, 20);
            var dtB = new DateTime(2017, 11, 20);
            var avgTicks = (dtA.Ticks + dtB.Ticks) / 2L;
            Assert.Equal(new DateTime(avgTicks), dtA.Average(dtB));
           
        }

        [Fact]
        public void RollingRules()
        {
            var calendar = new Calendar();
            calendar.DaysToAlwaysExclude.Add(DayOfWeek.Saturday);
            calendar.DaysToAlwaysExclude.Add(DayOfWeek.Sunday);
            calendar.DaysToExclude.Add(new DateTime(2017, 02, 28));

            //this is a saturday
            var date = new DateTime(2017, 02, 18);

            Assert.Equal(new DateTime(2017, 02, 20), date.IfHolidayRoll(RollType.F, calendar));
            Assert.Equal(new DateTime(2017, 02, 20), date.IfHolidayRoll(RollType.MF, calendar));
            Assert.Equal(new DateTime(2017, 02, 17), date.IfHolidayRoll(RollType.P, calendar));
            Assert.Equal(new DateTime(2017, 02, 17), date.IfHolidayRoll(RollType.NearestFollow, calendar));
            Assert.Equal(new DateTime(2017, 02, 17), date.IfHolidayRoll(RollType.NearestPrev, calendar));
            Assert.Equal(new DateTime(2017, 02, 17), date.IfHolidayRoll(RollType.MP, calendar));
            Assert.Equal(new DateTime(2017, 02, 17), date.IfHolidayRoll(RollType.LME, calendar));

            //now a sunday
            date = new DateTime(2017, 02, 19);

            Assert.Equal(new DateTime(2017, 02, 20), date.IfHolidayRoll(RollType.F, calendar));
            Assert.Equal(new DateTime(2017, 02, 20), date.IfHolidayRoll(RollType.MF, calendar));
            Assert.Equal(new DateTime(2017, 02, 17), date.IfHolidayRoll(RollType.P, calendar));
            Assert.Equal(new DateTime(2017, 02, 20), date.IfHolidayRoll(RollType.NearestFollow, calendar));
            Assert.Equal(new DateTime(2017, 02, 20), date.IfHolidayRoll(RollType.NearestPrev, calendar));
            Assert.Equal(new DateTime(2017, 02, 17), date.IfHolidayRoll(RollType.MP, calendar));
            Assert.Equal(new DateTime(2017, 02, 20), date.IfHolidayRoll(RollType.LME, calendar));

            //now month-end holiday
            date = new DateTime(2017, 02, 28);

            Assert.Equal(new DateTime(2017, 03, 01), date.IfHolidayRoll(RollType.F, calendar));
            Assert.Equal(new DateTime(2017, 02, 27), date.IfHolidayRoll(RollType.MF, calendar));
            Assert.Equal(new DateTime(2017, 02, 27), date.IfHolidayRoll(RollType.P, calendar));
            Assert.Equal(new DateTime(2017, 03, 01), date.IfHolidayRoll(RollType.NearestFollow, calendar));
            Assert.Equal(new DateTime(2017, 02, 27), date.IfHolidayRoll(RollType.NearestPrev, calendar));
            Assert.Equal(new DateTime(2017, 02, 27), date.IfHolidayRoll(RollType.MP, calendar));
            Assert.Equal(new DateTime(2017, 02, 27), date.IfHolidayRoll(RollType.LME, calendar));

            //special case for LME (mod-nearest-follow)
            calendar.DaysToExclude.Add(new DateTime(2017, 02, 20));
            date = new DateTime(2017, 02, 19);
            Assert.Equal(new DateTime(2017, 02, 21), date.IfHolidayRoll(RollType.LME, calendar));
            date = new DateTime(2017, 02, 18);
            Assert.Equal(new DateTime(2017, 02, 17), date.IfHolidayRoll(RollType.LME, calendar));
        }

        [Fact]
        public void LME3mRule()
        {
            var calendar = new Calendar();
            calendar.DaysToAlwaysExclude.Add(DayOfWeek.Saturday);
            calendar.DaysToAlwaysExclude.Add(DayOfWeek.Sunday);

            var date = new DateTime(2017, 01, 30);
            var lme3mDate = date.AddPeriod(RollType.LME, calendar, new Frequency("3m"));
            Assert.Equal(new DateTime(2017, 04, 28), lme3mDate);

            //easter 2017
            calendar.DaysToExclude.Add(new DateTime(2017, 04, 14));
            calendar.DaysToExclude.Add(new DateTime(2017, 04, 17));

            date = new DateTime(2017, 01, 14);
            lme3mDate = date.AddPeriod(RollType.LME, calendar, new Frequency("3m"));
            Assert.Equal(new DateTime(2017, 04, 13), lme3mDate);

            date = new DateTime(2017, 01, 15);
            lme3mDate = date.AddPeriod(RollType.LME, calendar, new Frequency("3m"));
            Assert.Equal(new DateTime(2017, 04, 13), lme3mDate);

            date = new DateTime(2017, 01, 16);
            lme3mDate = date.AddPeriod(RollType.LME, calendar, new Frequency("3m"));
            Assert.Equal(new DateTime(2017, 04, 18), lme3mDate);
        }

    }
}
