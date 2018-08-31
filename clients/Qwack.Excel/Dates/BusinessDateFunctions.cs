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
                 if (!ValidateCalendarAndRoll(RollMethod, Calendar, out var rollMethod, out var cal, out var errorMessage))
                     return errorMessage;

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
                 if (!ValidateCalendarAndRoll(RollMethod, Calendar, out var rollMethod, out var cal, out var errorMessage))
                     return errorMessage;

                 var period = new Frequency(Period);

                 return StartDate.SubtractPeriod(rollMethod, cal, period);

             });
        }

        private static bool ValidateCalendarAndRoll(string rollMethod, string calendar, out RollType rollType, out Calendar calendarObject, out string errorMessage)
        {
            rollType = default;
            calendarObject = default;
            errorMessage = default;
            if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(calendar, out calendarObject))
            {
                errorMessage = $"Calendar {calendar} not found in cache";
                return false;
            }

            if (!Enum.TryParse(rollMethod, out rollType))
            {
                errorMessage = $"Unknown roll method {rollMethod}";
                return false;
            }

            return true;
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

        [ExcelFunction(Description = "Returns start and end dates for a period code", Category = "QDates")]
        public static object QDates_DatesForPeriodCode(
           [ExcelArgument(Description = "Period code")] object Code)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (Code is double)
                {
                    Code = DateTime.FromOADate((double)Code).ToString("MMM-yy");
                }
                var (Start, End) = DateExtensions.ParsePeriod((string)Code);
                return (new object[] { Start, End }).ReturnExcelRangeVector();
            });
        }

        [ExcelFunction(Description = "Returns the last business day in a month for a give calendar", Category = "QDates")]
        public static object QDates_LastBusinessDay(
            [ExcelArgument(Description = "Date in month")] DateTime Date,
            [ExcelArgument(Description = "Calendar to check")] string Calendar)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(Calendar, out var cal))
                    return $"Calendar {Calendar} not found in cache";

                return Date.LastBusinessDayOfMonth(cal);
            });
        }

        [ExcelFunction(Description = "Returns the last business day in a month for a give calendar", Category = "QDates")]
        public static object QDates_FirstBusinessDay(
            [ExcelArgument(Description = "Date in month")] DateTime Date,
            [ExcelArgument(Description = "Calendar to check")] string Calendar)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(Calendar, out var cal))
                    return $"Calendar {Calendar} not found in cache";

                return Date.FirstBusinessDayOfMonth(cal);
            });
        }

        [ExcelFunction(Description = "Returns a list of all calendar names", Category = "QDates")]
        public static object QDates_ListCalendars()
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                return ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.ListCalendarNames();
            });
        }

        [ExcelFunction(Description = "Returns next isntance of specific weekday", Category = "QDates")]
        public static object QDates_NextWeekday(
            [ExcelArgument(Description = "Date")] DateTime Date,
            [ExcelArgument(Description = "Weekday")] string Weekday)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!Enum.TryParse(Weekday, out DayOfWeek weekday))
                    return $"Unknown daycount method {Weekday}";

                return Date.GetNextWeekday(weekday);
            });
        }

        [ExcelFunction(Description = "Returns next isntance of specific weekday", Category = "QDates")]
        public static object QDates_SpotDate(
            [ExcelArgument(Description = "Value Date")] DateTime ValDate,
            [ExcelArgument(Description = "Spot Lag")] string SpotLag,
            [ExcelArgument(Description = "Primary calendar")] string PrimaryCalendar,
            [ExcelArgument(Description = "Secondary calendar")] string SecondaryCalendar)
        {

            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(PrimaryCalendar, out var cal1))
                    return $"Calendar {PrimaryCalendar} not found in cache";

                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(SecondaryCalendar, out var cal2))
                    return $"Calendar {SecondaryCalendar} not found in cache";

                return ValDate.SpotDate(new Frequency(SpotLag), cal1, cal2);
            });
        }
    }
}
