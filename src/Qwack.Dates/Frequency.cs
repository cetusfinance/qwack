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

        public Frequency(int periodCount, DatePeriod periodType)
        {
            PeriodCount = periodCount;
            PeriodType = periodType;
        }

        public DatePeriod PeriodType { get; set; }
        public int PeriodCount { get; set; }

        public override string ToString()
        {
            return PeriodCount.ToString() + PeriodType.ToString();
        }

        private void SplitPeriod(string period)
        {
            string PP = period?.Substring(period.Length - 1, 1);

            switch (PP.ToUpper())
            {
                case "D":
                    PeriodType = DatePeriod.D;
                    break;
                case "Y":
                    PeriodType = DatePeriod.Y;
                    break;
                case "M":
                    PeriodType = DatePeriod.M;
                    break;
                case "B":
                    PeriodType = DatePeriod.B;
                    break;
                case "W":
                    PeriodType = DatePeriod.W;
                    break;
                default:
                    throw new ArgumentException(nameof(period), $"Unknown period type {PP}");

            }

            PeriodCount = int.Parse(period.Substring(0, period.Length - 1));
        }

        public static Frequency ZeroBd { get { return new Frequency(0, DatePeriod.B); } }
        public static Frequency OneBd { get { return new Frequency(1, DatePeriod.B); } }
        public static Frequency TwoBd { get { return new Frequency(2, DatePeriod.B); } }
        public static Frequency OneWeek { get { return new Frequency(1, DatePeriod.W); } }
        public static Frequency TwoWeeks { get { return new Frequency(2, DatePeriod.W); } }
        public static Frequency OneMonth { get { return new Frequency(1, DatePeriod.M); } }
        public static Frequency ThreeMonths { get { return new Frequency(3, DatePeriod.M); } }
        public static Frequency SixMonths { get { return new Frequency(6, DatePeriod.M); } }
        public static Frequency OneYear { get { return new Frequency(1, DatePeriod.Y); } }
    }
}