using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Transport.BasicTypes;

namespace Qwack.Dates
{
    /// <summary>
    /// Extension methods for easy creation of frequency objects
    /// </summary>
    public static class FrequencyExtensions
    {
        public static Frequency Months(this int number) => new Frequency(number, DatePeriodType.M);
        public static Frequency Years(this int numberOfPeriods) => new Frequency(numberOfPeriods, DatePeriodType.Y);
        public static Frequency Bd(this int numberOfPeriods) => new Frequency(numberOfPeriods, DatePeriodType.BusinessDay);
        public static Frequency Day(this int numberOfPeriods) => new Frequency(numberOfPeriods, DatePeriodType.Day);
        public static Frequency Weeks(this int numberOfPeriods) => new Frequency(numberOfPeriods, DatePeriodType.W);
    }
}
