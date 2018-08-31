using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public static class ListedUtils
    {
        private static readonly string[] s_futureMonths = new string[] { "F", "G", "H", "J", "K", "M", "N", "Q", "U", "V", "X", "Z" };

        public static DateTime FuturesCodeToDateTime(string futuresCode, DateTime? refDate = null)
        {
            if (!refDate.HasValue)
                refDate = DateTime.Today;

            var offset = futuresCode.Length - 1;
            while (int.TryParse(futuresCode.Substring(offset, 1), out var dummy))
            {
                offset--;
                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(futuresCode), "Reached the start of the string and did not find the end of numeric data");
            }

            if (!Enum.TryParse(futuresCode.Substring(offset, 1), out MonthEnum month))
                throw new InvalidOperationException($"Month code {futuresCode.Substring(offset, 1)} not recognised");

            var year = int.Parse(futuresCode.Substring(offset + 1, futuresCode.Length - offset - 1));

            if (year < 10) //single digit year
            {
                var currentYear = refDate.Value.Year % 10;
                var baseYear = (refDate.Value.Year / 10) * 10;

                if (year < currentYear) // case of year 5 evaluated in 2017 indicating 2025
                    return new DateTime(baseYear + 10 + year, (int)month, 1);
                else // case of year 8 evaluated in 2017 indicating 2018
                    return new DateTime(baseYear + year, (int)month, 1);
            }
            else //double digit year
            {
                var baseYear = (refDate.Value.Year / 100) * 100;
                return new DateTime(baseYear + year, (int)month, 1);
            }
        }

        public static string DateTimeToFuturesCode(string futuresCodeRoot, DateTime targetMonthDate, int numYearDigits)
        {
            var validNumYearDigits = new[] { 1, 2, 4 };
            if (!validNumYearDigits.Contains(numYearDigits))
                throw new Exception($"Only 1, 2 and 4 year digits can be returned, not {numYearDigits}");

            var m = targetMonthDate.Month;
            var y = targetMonthDate.Year;
            var mStr = s_futureMonths[m - 1];
            var yStr = y.ToString().Substring(4 - numYearDigits);
            return $"{futuresCodeRoot}{mStr}{yStr}";
        }
    }
}
