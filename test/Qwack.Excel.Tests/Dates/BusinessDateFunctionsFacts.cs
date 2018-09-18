using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Qwack.Dates;
using Qwack.Excel.Dates;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Providers.Json;

namespace Qwack.Excel.Tests.Dates
{
    public class BusinessDateFunctionsFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
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
        public void QDates_ThirdWednesday_Facts()
        {
            Assert.Equal(DateTime.Parse("2018-09-19"), BusinessDateFunctions.QDates_ThirdWednesday(DateTime.Parse("2018-09-01")));
        }
    }
}
