using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    /// <summary>
    /// Extension methods for easy creation of frequency objects
    /// </summary>
    public static class FrequencyExtensions
    {
        public static Frequency Months(this int number)
        {
            return new Frequency(number, DatePeriodType.M);
        }
        public static Frequency Years(this int numberOfPeriods)
        {
            return new Frequency(numberOfPeriods, DatePeriodType.Y);
        }
        public static Frequency Bd(this int numberOfPeriods)
        {
            return new Frequency(numberOfPeriods, DatePeriodType.BusinessDay);
        }
        public static Frequency Weeks(this int numberOfPeriods)
        {
            return new Frequency(numberOfPeriods, DatePeriodType.W);
        }
    }
}
