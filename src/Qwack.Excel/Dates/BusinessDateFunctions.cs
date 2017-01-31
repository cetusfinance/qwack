using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Dates;

namespace Qwack.Excel.Dates
{
    public static class BusinessDateFunctions
    {
        [ExcelFunction(Description = "Checks if the given date is a holiday according to the specified calendars", Category = "QDates")]
        public static bool QDates_IsHoliday(
            [ExcelArgument(Description = "The date to check")] DateTime DateToCheck, 
            [ExcelArgument(Description = "The calendar(s) to check against")]string Calendar)
        {

            //return DateToCheck.IfHolidayRoll
            return true;
        }
    }
}
