using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Qwack.Dates;
using Qwack.Excel.Dates;
using Qwack.Providers.Json;

namespace Qwack.Excel.Tests.Dates
{
    public class BusinessDateFunctionsFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void QDates_IsHoliday_Facts()
        { 
            Assert.Equal("Calendar blahblah not found in cache" , BusinessDateFunctions.QDates_IsHoliday(DateTime.Today, "blahblah"));

            Assert.Equal(false, BusinessDateFunctions.QDates_IsHoliday(DateTime.Parse("2018-12-24"), "NYC"));
            Assert.Equal(true, BusinessDateFunctions.QDates_IsHoliday(DateTime.Parse("2018-12-25"), "NYC"));
        }

        [Fact]
        public void QDates_AddPeriod_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_AddPeriod(DateTime.Today, "blahblah", "pwah", "glah") is string);

            Assert.Equal("Calendar glah not found in cache", BusinessDateFunctions.QDates_AddPeriod(DateTime.Today, "blahblah", "pwah", "glah"));
            Assert.Equal("Unknown roll method pwah", BusinessDateFunctions.QDates_AddPeriod(DateTime.Today, "blahblah", "pwah", "NYC"));

            Assert.Equal("Could not parse period blahblah", BusinessDateFunctions.QDates_AddPeriod(DateTime.Today, "blahblah", "F", "NYC"));

            Assert.Equal(DateTime.Parse("2018-10-01"), BusinessDateFunctions.QDates_AddPeriod(DateTime.Parse("2018-08-31"), "1m", "F", "NYC"));
            Assert.Equal(DateTime.Parse("2018-09-28"), BusinessDateFunctions.QDates_AddPeriod(DateTime.Parse("2018-08-31"), "1m", "MF", "NYC"));
        }

        [Fact]
        public void QDates_SubtractPeriod_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_SubtractPeriod(DateTime.Today, "blahblah", "pwah", "glah") is string);

            Assert.Equal("Calendar glah not found in cache", BusinessDateFunctions.QDates_SubtractPeriod(DateTime.Today, "blahblah", "pwah", "glah"));
            Assert.Equal("Unknown roll method pwah", BusinessDateFunctions.QDates_SubtractPeriod(DateTime.Today, "blahblah", "pwah", "NYC"));

            Assert.Equal("Could not parse period blahblah", BusinessDateFunctions.QDates_SubtractPeriod(DateTime.Today, "blahblah", "F", "NYC"));

            Assert.Equal(DateTime.Parse("2018-09-04"), BusinessDateFunctions.QDates_SubtractPeriod(DateTime.Parse("2018-10-01"), "1m", "MP", "NYC"));
            Assert.Equal(DateTime.Parse("2018-08-31"), BusinessDateFunctions.QDates_SubtractPeriod(DateTime.Parse("2018-10-01"), "1m", "P", "NYC"));
        }

        [Fact]
        public void QDates_SpecificWeekday_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_SpecificWeekday(DateTime.Today, "blahblah", 1) is string);
            Assert.Equal("Unknown weekday blahblah", BusinessDateFunctions.QDates_SpecificWeekday(DateTime.Today, "blahblah", 1));
            Assert.Equal(DateTime.Parse("2018-09-06"), BusinessDateFunctions.QDates_SpecificWeekday(DateTime.Parse("2018-09-01"), "Thursday", 1));         
        }

        [Fact]
        public void QDates_SpecificLastWeekday_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_SpecificLastWeekday(DateTime.Today, "blahblah", 1) is string);
            Assert.Equal("Unknown weekday blahblah", BusinessDateFunctions.QDates_SpecificLastWeekday(DateTime.Today, "blahblah", 1));
            Assert.Equal(DateTime.Parse("2018-09-27"), BusinessDateFunctions.QDates_SpecificLastWeekday(DateTime.Parse("2018-09-01"), "Thursday", 1));
        }

        [Fact]
        public void QDates_ThirdWednesday_Facts() => Assert.Equal(DateTime.Parse("2018-09-19"), BusinessDateFunctions.QDates_ThirdWednesday(DateTime.Parse("2018-09-01")));

        [Fact]
        public void QDates_YearFraction_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_YearFraction(DateTime.Today, DateTime.Today, "blahblah") is string);
            Assert.Equal("Unknown daycount method blahblah", BusinessDateFunctions.QDates_YearFraction(DateTime.Today, DateTime.Today, "blahblah"));
            Assert.Equal(1.0, BusinessDateFunctions.QDates_YearFraction(DateTime.Today, DateTime.Today.AddDays(365), "ACT365F"));
        }

        [Fact]
        public void QDates_NumBusinessDaysInPeriod_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_NumBusinessDaysInPeriod(DateTime.Today, DateTime.Today, "blahblah") is string);
            Assert.Equal("Calendar blahblah not found in cache", BusinessDateFunctions.QDates_NumBusinessDaysInPeriod(DateTime.Today, DateTime.Today, "blahblah"));
            Assert.Equal(5, BusinessDateFunctions.QDates_NumBusinessDaysInPeriod(DateTime.Parse("2018-09-18"), DateTime.Parse("2018-09-25"), "JHB"));
        }

        [Fact]
        public void QDates_ListBusinessDaysInPeriod_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_ListBusinessDaysInPeriod(DateTime.Today, DateTime.Today, "blahblah") is string);
            Assert.Equal("Calendar blahblah not found in cache", BusinessDateFunctions.QDates_ListBusinessDaysInPeriod(DateTime.Today, DateTime.Today, "blahblah"));

            var expected = new DateTime[]
            {
                DateTime.Parse("2018-09-18"),
                DateTime.Parse("2018-09-19"),
                DateTime.Parse("2018-09-20"),
                DateTime.Parse("2018-09-21"),
                DateTime.Parse("2018-09-25"),
            };
            Assert.True(
                Enumerable.SequenceEqual(expected, 
                ((double[])BusinessDateFunctions.QDates_ListBusinessDaysInPeriod(DateTime.Parse("2018-09-18"), DateTime.Parse("2018-09-25"), "JHB"))
                .Select(x => DateTime.FromOADate(x)).ToArray()));
        }

        [Fact]
        public void QDates_ListFridaysInPeriod_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_ListFridaysInPeriod(DateTime.Today, DateTime.Today, "blahblah") is string);
            Assert.Equal("Calendar blahblah not found in cache", BusinessDateFunctions.QDates_ListFridaysInPeriod(DateTime.Today, DateTime.Today, "blahblah"));

            var expected = new DateTime[]
            {
                DateTime.Parse("2018-09-7"),
                DateTime.Parse("2018-09-14"),
                DateTime.Parse("2018-09-21"),
                DateTime.Parse("2018-09-28"),
            };
            Assert.True(
                Enumerable.SequenceEqual(expected,
                ((double[])BusinessDateFunctions.QDates_ListFridaysInPeriod(DateTime.Parse("2018-09-01"), DateTime.Parse("2018-09-30"), "JHB"))
                .Select(x => DateTime.FromOADate(x)).ToArray()));
        }

        [Fact]
        public void QDates_LastBusinessDay_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_LastBusinessDay(DateTime.Today, "blahblah") is string);
            Assert.Equal("Calendar blahblah not found in cache", BusinessDateFunctions.QDates_LastBusinessDay(DateTime.Today, "blahblah"));
            Assert.Equal(DateTime.Parse("2018-09-28"), BusinessDateFunctions.QDates_LastBusinessDay(DateTime.Parse("2018-09-30"), "JHB"));
                
        }

        [Fact]
        public void QDates_FirstBusinessDay_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_FirstBusinessDay(DateTime.Today, "blahblah") is string);
            Assert.Equal("Calendar blahblah not found in cache", BusinessDateFunctions.QDates_FirstBusinessDay(DateTime.Today, "blahblah"));
            Assert.Equal(DateTime.Parse("2018-09-03"), BusinessDateFunctions.QDates_FirstBusinessDay(DateTime.Parse("2018-09-30"), "JHB"));
        }

        [Fact]
        public void QDates_LastDay_Facts() => Assert.Equal(DateTime.Parse("2018-09-30"), BusinessDateFunctions.QDates_LastDay(DateTime.Parse("2018-09-30")));

        [Fact]
        public void QDates_FirstDay_Facts() => Assert.Equal(DateTime.Parse("2018-09-01"), BusinessDateFunctions.QDates_FirstDay(DateTime.Parse("2018-09-30")));

        [Fact]
        public void QDates_NextWeekday_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_NextWeekday(DateTime.Today, "blahblah") is string);
            Assert.Equal("Unknown weekday blahblah", BusinessDateFunctions.QDates_NextWeekday(DateTime.Today, "blahblah"));
            Assert.Equal(DateTime.Parse("2018-09-06"), BusinessDateFunctions.QDates_NextWeekday(DateTime.Parse("2018-09-01"), "Thursday"));
        }

        [Fact]
        public void QDates_SpotDate_Facts()
        {
            //returns strings for errors
            Assert.True(BusinessDateFunctions.QDates_SpotDate(DateTime.Today, "blahblah", "whahah", "glaah") is string);
            Assert.Equal("Could not parse lag string blahblah", BusinessDateFunctions.QDates_SpotDate(DateTime.Today, "blahblah", "whahah", "glaah"));
            Assert.Equal("Calendar whahah not found in cache", BusinessDateFunctions.QDates_SpotDate(DateTime.Today, "2b", "whahah", "glaah"));
            Assert.Equal("Calendar glaah not found in cache", BusinessDateFunctions.QDates_SpotDate(DateTime.Today, "2b", "NYC", "glaah"));

            Assert.Equal(DateTime.Parse("2018-09-04"), BusinessDateFunctions.QDates_SpotDate(DateTime.Parse("2018-08-31"), "2b", "LON", "NYC"));
        }

        [Fact]
        public void QDates_Easter_Facts()
        {
            var e = (object[])BusinessDateFunctions.QDates_Easter(DateTime.Parse("2019-12-24"));

            Assert.Equal(DateTime.Parse("2019-04-19"), e[0]);
            Assert.Equal(DateTime.Parse("2019-04-22"), e[1]);
        }
    }
}
