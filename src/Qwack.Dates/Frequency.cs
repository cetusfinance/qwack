using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public class Frequency
    {
        public Frequency() { }

        public Frequency(string frequency)
        {
            SplitPeriod(frequency);
        }

        public Frequency(int periodCount, DatePeriodType periodType)
        {
            PeriodCount = periodCount;
            PeriodType = periodType;
        }

        public DatePeriodType PeriodType { get; set; }
        public int PeriodCount { get; set; }

        public override string ToString()
        {
            return PeriodCount.ToString() + PeriodType.ToString();
        }

        private void SplitPeriod(string period)
        {
            var periodType = period[period.Length-1];

            switch (periodType)
            {
                case 'D':
                case 'd':
                    PeriodType = DatePeriodType.D;
                    break;
                case 'Y':
                case 'y':
                    PeriodType = DatePeriodType.Y;
                    break;
                case 'M':
                case 'm':
                    PeriodType = DatePeriodType.M;
                    break;
                case 'B':
                case 'b':
                    PeriodType = DatePeriodType.B;
                    break;
                case 'w':
                case 'W':
                    PeriodType = DatePeriodType.W;
                    break;
                default:
                    throw new ArgumentException(nameof(period), $"Unknown period type {periodType}");
            }
            PeriodCount = int.Parse(period.Substring(0, period.Length - 1));
        }

        public static Frequency ZeroBd { get { return new Frequency(0, DatePeriodType.B); } }
        public static Frequency OneBd { get { return new Frequency(1, DatePeriodType.B); } }
        public static Frequency TwoBd { get { return new Frequency(2, DatePeriodType.B); } }
        public static Frequency OneWeek { get { return new Frequency(1, DatePeriodType.W); } }
        public static Frequency TwoWeeks { get { return new Frequency(2, DatePeriodType.W); } }
        public static Frequency OneMonth { get { return new Frequency(1, DatePeriodType.M); } }
        public static Frequency ThreeMonths { get { return new Frequency(3, DatePeriodType.M); } }
        public static Frequency SixMonths { get { return new Frequency(6, DatePeriodType.M); } }
        public static Frequency OneYear { get { return new Frequency(1, DatePeriodType.Y); } }
    }
}