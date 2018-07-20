using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Dates;
using Qwack.Excel.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Qwack.Excel.Dates
{
    public class BusinessDateFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<BusinessDateFunctions>();

        [ExcelFunction(Description = "Checks if the given date is a holiday according to the specified calendars", Category = "QDates")]
        public static object QDates_IsHoliday(
            [ExcelArgument(Description = "The date to check")] DateTime DateToCheck,
            [ExcelArgument(Description = "The calendar(s) to check against")]string calendar)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(calendar, out var cal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", calendar);
                    return $"Calendar {calendar} not found in cache";
                }
                return cal.IsHoliday(DateToCheck);
            });
        }

        [ExcelFunction(Description = "Adds a specified period to a date, adjusting for holidays", Category = "QDates")]
        public static object QDates_AddPeriod(
            [ExcelArgument(Description = "Starting date")] DateTime StartDate,
            [ExcelArgument(Description = "Period specified as a string e.g. 1w")]string Period,
            [ExcelArgument(Description = "Roll method")]string RollMethod,
            [ExcelArgument(Description = "Calendar(s) to check against")]string Calendar)
        {
            return ExcelHelper.Execute(_logger, () =>
             {
                 if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(Calendar, out var cal))
                     return $"Calendar {Calendar} not found in cache";

                 if (!Enum.TryParse(RollMethod, out RollType rollMethod))
                     return $"Unknown roll method {RollMethod}";

                 var period = new Frequency(Period);

                 return StartDate.AddPeriod(rollMethod, cal, period);

             });
        }

        [ExcelFunction(Description = "Subtracts a specified period from a date, adjusting for holidays", Category = "QDates")]
        public static object QDates_SubtractPeriod(
           [ExcelArgument(Description = "Starting date")] DateTime StartDate,
           [ExcelArgument(Description = "Period specified as a string e.g. 1w")]string Period,
           [ExcelArgument(Description = "Roll method")]string RollMethod,
           [ExcelArgument(Description = "Calendar(s) to check against")]string Calendar)
        {
            return ExcelHelper.Execute(_logger, () =>
             {
                 if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(Calendar, out var cal))
                     return $"Calendar {Calendar} not found in cache";

                 if (!Enum.TryParse(RollMethod, out RollType rollMethod))
                     return $"Unknown roll method {RollMethod}";

                 var period = new Frequency(Period);

                 return StartDate.SubtractPeriod(rollMethod, cal, period);

             });
        }

        [ExcelFunction(Description = "Returns N-th instance of a specific weekday in a given month", Category = "QDates")]
        public static object QDates_SpecificWeekday(
            [ExcelArgument(Description = "Date in month")] DateTime DateInMonth,
            [ExcelArgument(Description = "Day of week")]string DayOfWeek,
            [ExcelArgument(Description = "Nth day to extract - e.g. 3 would give the 3rd instance of the day")]int N)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!Enum.TryParse(DayOfWeek, out DayOfWeek weekDay))
                    return $"Unknown weekday {DayOfWeek}";

                return DateInMonth.NthSpecificWeekDay(weekDay, N);
            });
        }

        [ExcelFunction(Description = "Returns N-th last instance of a specific weekday in a given month", Category = "QDates")]
        public static object QDates_SpecificLastWeekday(
            [ExcelArgument(Description = "Date in month")] DateTime DateInMonth,
            [ExcelArgument(Description = "Day of week")]string DayOfWeek,
            [ExcelArgument(Description = "Nth day to extract - e.g. 3 would give the 3rd instance of the day")]int N)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!Enum.TryParse(DayOfWeek, out DayOfWeek weekDay))
                    return $"Unknown weekday {DayOfWeek}";

                return DateInMonth.NthLastSpecificWeekDay(weekDay, N);
            });
        }

        [ExcelFunction(Description = "Returns 3rd Wednesday in a given month", Category = "QDates")]
        public static object QDates_ThirdWednesday(
            [ExcelArgument(Description = "Date in month")] DateTime DateInMonth)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                return DateInMonth.ThirdWednesday();
            });
        }

        [ExcelFunction(Description = "Returns year faction for two dates and a given day count method", Category = "QDates")]
        public static object QDates_YearFraction(
            [ExcelArgument(Description = "Period start date (inclusive)")] DateTime StartDate,
            [ExcelArgument(Description = "Period end date (exclusive)")] DateTime EndDate,
            [ExcelArgument(Description = "Day count method")] string DayCountMethod)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!Enum.TryParse(DayCountMethod, out DayCountBasis dayCount))
                    return $"Unknown daycount method {DayCountMethod}";

                return dayCount.CalculateYearFraction(StartDate, EndDate);
            });
        }

        [ExcelFunction(Description = "Returns number of business days in a period for a give calendar", Category = "QDates")]
        public static object QDates_NumBusinessDaysInPeriod(
            [ExcelArgument(Description = "Period start date (inclusive)")] DateTime StartDate,
            [ExcelArgument(Description = "Period end date (inclusive)")] DateTime EndDate,
            [ExcelArgument(Description = "Calendar to check")] string Calendar)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(Calendar, out var cal))
                    return $"Calendar {Calendar} not found in cache";

                return StartDate.BusinessDaysInPeriod(EndDate, cal).Count;
            });
        }

        [ExcelFunction(Description = "Returns list of business days in a period for a give calendar", Category = "QDates")]
        public static object QDates_ListBusinessDaysInPeriod(
            [ExcelArgument(Description = "Period start date (inclusive)")] DateTime StartDate,
            [ExcelArgument(Description = "Period end date (inclusive)")] DateTime EndDate,
            [ExcelArgument(Description = "Calendar to check")] string calendar)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(calendar, out var cal))
                {
                    return $"Calendar {calendar} not found in cache";
                }
                return StartDate.BusinessDaysInPeriod(EndDate, cal).Select(x => x.ToOADate()).ToArray();
            });
        }

        [ExcelFunction(Description = "Returns list of Fridays in a period for a give calendar", Category = "QDates")]
        public static object QDates_ListFridaysInPeriod(
           [ExcelArgument(Description = "Period start date (inclusive)")] DateTime StartDate,
           [ExcelArgument(Description = "Period end date (inclusive)")] DateTime EndDate,
           [ExcelArgument(Description = "Calendar to check")] string calendar)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(calendar, out var cal))
                {
                    return $"Calendar {calendar} not found in cache";
                }
                return StartDate.FridaysInPeriod(EndDate, cal).Select(x => x.ToOADate()).ToArray();
            });
        }
    }
}
