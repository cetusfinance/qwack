using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Transport.BasicTypes;

namespace Qwack.Dates
{
    /// <summary>
    /// Frequency class which represents a frequency or period as a period type and a number of periods
    /// </summary>
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

        public static bool TryParse(string frequency, out Frequency output)
        {
            var (success, periodType, periodCount) = TrySplitPeriod(frequency);

            if (success)
            {
                output = new Frequency(periodCount, periodType);
                return true;
            }
            output = new Frequency(0, DatePeriodType.D);
            return false;
        }

        public DatePeriodType PeriodType { get; set; }
        public int PeriodCount { get; set; }

        public override string ToString() => PeriodCount.ToString() + PeriodType.ToString();

        private void SplitPeriod(string period)
        {
            var periodType = period[period.Length - 1];

            PeriodType = periodType switch
            {
                'D' or 'd' => DatePeriodType.D,
                'Y' or 'y' => DatePeriodType.Y,
                'M' or 'm' => DatePeriodType.M,
                'B' or 'b' => DatePeriodType.B,
                'w' or 'W' => DatePeriodType.W,
                _ => throw new ArgumentException(nameof(period), $"Unknown period type {periodType}"),
            };
            PeriodCount = int.Parse(period.Substring(0, period.Length - 1));
        }

        public static (bool success, DatePeriodType periodType, int periodCount) TrySplitPeriod(string period)
        {
            var periodTypeStr = period[period.Length - 1];
            var periodType = DatePeriodType.Day;
            var periodCount = 0;
            var success = true;

            switch (periodTypeStr)
            {
                case 'D':
                case 'd':
                    periodType = DatePeriodType.D;
                    break;
                case 'Y':
                case 'y':
                    periodType = DatePeriodType.Y;
                    break;
                case 'M':
                case 'm':
                    periodType = DatePeriodType.M;
                    break;
                case 'B':
                case 'b':
                    periodType = DatePeriodType.B;
                    break;
                case 'w':
                case 'W':
                    periodType = DatePeriodType.W;
                    break;
                default:
                    success = false;
                    break;
            }

            var result = int.TryParse(period.Substring(0, period.Length - 1), out var pc);
            if (result)
                periodCount = pc;
            else
                success = false;

            return (success, periodType, periodCount);
        }

        public static bool operator ==(Frequency x, Frequency y) => x.PeriodCount == y.PeriodCount && y.PeriodType == x.PeriodType;

        public static bool operator !=(Frequency x, Frequency y) => !(x == y);

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
            if (obj is not Frequency)
            {
                return false;
            }
            return (Frequency)obj == this;
        }

        public static Frequency operator +(Frequency x, Frequency y)
        {
            if (x.PeriodType == y.PeriodType)
                return new Frequency(x.PeriodCount + y.PeriodCount, x.PeriodType);

            if (x.PeriodType == DatePeriodType.M && y.PeriodType == DatePeriodType.Y)
            {
                var totalMonths = x.PeriodCount + y.PeriodCount * 12;
                if (totalMonths % 12 == 0)
                    return new Frequency(totalMonths / 12, DatePeriodType.Y);
                else
                    return new Frequency(totalMonths, DatePeriodType.M);
            }
            else if (x.PeriodType == DatePeriodType.Y && y.PeriodType == DatePeriodType.M)
            {
                return y + x;
            }

            throw new NotImplementedException("Can only add months/years and frequencies of same period type");
        }

        public static Frequency operator -(Frequency x, Frequency y) => x + new Frequency(-y.PeriodCount, y.PeriodType);
    }
}
