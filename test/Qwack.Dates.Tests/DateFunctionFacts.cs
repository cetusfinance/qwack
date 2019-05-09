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

            Assert.Throws<ArgumentException>(() => endDate.BusinessDaysInPeriod(startDate, WeekendsOnly));
        }

        [Fact]
        public void CalendarDaysInPeriod()
        {
            var startDate = new DateTime(2016, 02, 10);
            var endDate = new DateTime(2016, 10, 13);
            Assert.Equal((endDate - startDate).TotalDays + 1, startDate.CalendarDaysInPeriod(endDate).Count);
            Assert.Throws<ArgumentException>(() => endDate.CalendarDaysInPeriod(startDate));
        }

        [Fact]
        public void FridaysInPeriod()
        {
            var startDate = new DateTime(2018, 08, 01);
            var endDate = new DateTime(2018, 08, 31);
            Assert.Equal(5, startDate.FridaysInPeriod(endDate, EmptyCalendar).Count);
            Assert.Throws<ArgumentException>(() => endDate.FridaysInPeriod(startDate, EmptyCalendar));
        }

        [Fact]
        public void YearFractionSingleYear()
        {
            var startDate = new DateTime(2016, 02, 10);
            var endDate = new DateTime(2016, 10, 13);
            Assert.Equal(246, startDate.CalculateYearFraction(endDate, DayCountBasis.Act360) * 360, 15);
        }


        [Fact]
        public void YearFractionAndBack()
        {
            var startDate = new DateTime(2016, 02, 10);
            var endDate = new DateTime(2016, 10, 13);
            var yf = startDate.CalculateYearFraction(endDate, DayCountBasis.Act365F);
            var date = startDate.AddYearFraction(yf, DayCountBasis.Act365F);
            Assert.Equal(endDate, date);
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

        [Fact]
        public void PeriodParser()
        {
            var (startDate, endDate) = "CAL19".ParsePeriod();
            Assert.Equal(new DateTime(2019, 1, 1), startDate);
            Assert.Equal(new DateTime(2019, 12, 31), endDate);

            (startDate, endDate) = "CAL-30".ParsePeriod();
            Assert.Equal(new DateTime(2030, 1, 1), startDate);
            Assert.Equal(new DateTime(2030, 12, 31), endDate);

            (startDate, endDate) = "Q4-22".ParsePeriod();
            Assert.Equal(new DateTime(2022, 10, 1), startDate);
            Assert.Equal(new DateTime(2022, 12, 31), endDate);

            (startDate, endDate) = "Q422".ParsePeriod();
            Assert.Equal(new DateTime(2022, 10, 1), startDate);
            Assert.Equal(new DateTime(2022, 12, 31), endDate);

            (startDate, endDate) = "Q1-22".ParsePeriod();
            Assert.Equal(new DateTime(2022, 1, 1), startDate);
            Assert.Equal(new DateTime(2022, 3, 31), endDate);

            (startDate, endDate) = "H122".ParsePeriod();
            Assert.Equal(new DateTime(2022, 1, 1), startDate);
            Assert.Equal(new DateTime(2022, 6, 30), endDate);

            (startDate, endDate) = "H2-22".ParsePeriod();
            Assert.Equal(new DateTime(2022, 7, 1), startDate);
            Assert.Equal(new DateTime(2022, 12, 31), endDate);

            (startDate, endDate) = "FEB-22".ParsePeriod();
            Assert.Equal(new DateTime(2022, 2, 1), startDate);
            Assert.Equal(new DateTime(2022, 2, 28), endDate);

            (startDate, endDate) = "FEB 22".ParsePeriod();
            Assert.Equal(new DateTime(2022, 2, 1), startDate);
            Assert.Equal(new DateTime(2022, 2, 28), endDate);

            (startDate, endDate) = "FEB22".ParsePeriod();
            Assert.Equal(new DateTime(2022, 2, 1), startDate);
            Assert.Equal(new DateTime(2022, 2, 28), endDate);
        }

        [Fact]
        public void NextWeekday()
        {
            var d = DateTime.Parse("2018-08-24"); //Friday 24th Aug 2018
            Assert.Equal(new DateTime(2018, 08, 29), d.GetNextWeekday(DayOfWeek.Wednesday));
            Assert.Equal(new DateTime(2018, 08, 30), d.GetNextWeekday(DayOfWeek.Thursday));
            Assert.Equal(new DateTime(2018, 08, 31), d.GetNextWeekday(DayOfWeek.Friday));
            Assert.Equal(new DateTime(2018, 08, 27), d.GetNextWeekday(DayOfWeek.Monday));

            d = DateTime.Parse("2018-08-20"); //Monday 20th Aug 2018
            Assert.Equal(new DateTime(2018, 08, 22), d.GetNextWeekday(DayOfWeek.Wednesday));
            Assert.Equal(new DateTime(2018, 08, 23), d.GetNextWeekday(DayOfWeek.Thursday));
        }

        [Fact]
        public void NextPrevIMMDate()
        {
            Assert.Equal(new DateTime(2018, 09, 19), DateTime.Parse("2018-08-24").GetNextImmDate());
            Assert.Equal(new DateTime(2018, 09, 19), DateTime.Parse("2018-09-10").GetNextImmDate());
            Assert.Equal(new DateTime(2018, 12, 19), DateTime.Parse("2018-09-19").GetNextImmDate());
            Assert.Equal(new DateTime(2019, 03, 20), DateTime.Parse("2018-12-20").GetNextImmDate());

            Assert.Equal(new DateTime(2018, 06, 20), DateTime.Parse("2018-08-24").GetPrevImmDate());
            Assert.Equal(new DateTime(2018, 06, 20), DateTime.Parse("2018-09-19").GetPrevImmDate());
            Assert.Equal(new DateTime(2018, 09, 19), DateTime.Parse("2018-09-20").GetPrevImmDate());
            Assert.Equal(new DateTime(2018, 12, 19), DateTime.Parse("2019-03-19").GetPrevImmDate());
        }

        [Theory]
        [MemberData(nameof(GetYFExamples))]
        public void YearFraction(DateTime startDate, DateTime endDate, DayCountBasis basis, double yf) => Assert.Equal(yf, startDate.CalculateYearFraction(endDate, basis));

        public static IEnumerable<object[]> GetYFExamples()
        {
            var holidays = new List<object[]>()
            {
                new object[] { new DateTime(2015,07,04), new DateTime(2015, 08, 04), DayCountBasis.Act_Act, 31.0/365.0 }, //year is not leap
                new object[] { new DateTime(2016,07,04), new DateTime(2016, 08, 04), DayCountBasis.Act_Act, 31.0/366.0 }, //year is leap
                new object[] { new DateTime(2016,07,04), new DateTime(2017, 07, 04), DayCountBasis.Act_Act, 180/366.0 + 185/365.0 },  //cross years

                new object[] { new DateTime(2015,07,04), new DateTime(2015, 08, 04), DayCountBasis._30_360, 1.0/12 },
                new object[] { new DateTime(2016,07,04), new DateTime(2016, 08, 04), DayCountBasis._30_360, 1.0/12 },
                new object[] { new DateTime(2016,07,04), new DateTime(2017, 07, 04), DayCountBasis._30_360, 1.0 },

                new object[] { new DateTime(2016,03,04), new DateTime(2017, 09, 04), DayCountBasis.Unity, 1.0 },

            };

            return holidays;
        }

        [Fact]
        public void xDigitYear()
        {
            Assert.Equal(18, DateExtensions.DoubleDigitYear(2018));
            Assert.Equal(8, DateExtensions.SingleDigitYear(2018));

            Assert.Equal(28, DateExtensions.DoubleDigitYear(2028));
            Assert.Equal(8, DateExtensions.SingleDigitYear(2028));
        }


        [Theory]
        //https://en.wikipedia.org/wiki/List_of_dates_for_Easter
        [InlineData(1999, "1999-04-04")]
        [InlineData(2001, "2001-04-15")]
        [InlineData(2002, "2002-03-31")]
        [InlineData(2005, "2005-03-27")]
        [InlineData(2010, "2010-04-04")]
        [InlineData(2019, "2019-04-21")]
        [InlineData(2020, "2020-04-12")]
        [InlineData(2024, "2024-03-31")]
        public void GaussEaster(int year, string expected)
        {
            var e = DateTime.Parse(expected);
            Assert.Equal(e, DateExtensions.EasterGauss(year));
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
        }
    }
}
