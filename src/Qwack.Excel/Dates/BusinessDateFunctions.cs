using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Dates;
using Qwack.Excel.Services;
namespace Qwack.Excel.Dates
{
    public static class BusinessDateFunctions
    {
        [ExcelFunction(Description = "Checks if the given date is a holiday according to the specified calendars", Category = "QDates")]
        public static object QDates_IsHoliday(
            [ExcelArgument(Description = "The date to check")] DateTime DateToCheck, 
            [ExcelArgument(Description = "The calendar(s) to check against")]string Calendar)
        {
            return ExcelHelper.Execute(() =>
            {
                Calendar cal;

                if (!StaticDataService.Instance.CalendarProvider.Collection.TryGetCalendar(Calendar, out cal)) 
                    return "Calendar not found in cache";

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
            return ExcelHelper.Execute(() =>
            {
                Calendar cal;
                if (!StaticDataService.Instance.CalendarProvider.Collection.TryGetCalendar(Calendar, out cal))
                    return $"Calendar {Calendar} not found in cache";

                RollType rollMethod;
                if(!Enum.TryParse<RollType>(RollMethod,out rollMethod))
                    return $"Unknown roll method {RollMethod}";

                Frequency period = new Frequency(Period);

                return StartDate.AddPeriod(rollMethod, cal, period);

            });
        }
    }
}
