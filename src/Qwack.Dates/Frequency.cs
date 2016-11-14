using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public struct Frequency
    {
        public Frequency(string frequency)
        {
            PeriodType = DatePeriodType.B;
            PeriodCount = 0;
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
            var periodType = period[period.Length - 1];

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

        public static bool operator ==(Frequency x, Frequency y)
        {
            return x.PeriodCount == y.PeriodCount && y.PeriodType == x.PeriodType;
        }

        public static bool operator !=(Frequency x, Frequency y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = PeriodCount;
                result = (result * 397) ^ (int)PeriodType;
                return result;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Frequency))
            {
                return false;
            }
            return (Frequency)obj == this;
        }
    }
}