using System;
using Qwack.Dates;

namespace Qwack.Core.Curves.TimeProviders
{
    public class BusinessDayTimeProvider(Calendar calendar, double weekendWeight = 0.0, double holidayWeight = 0.0) : ITimeProvider
    {
        public double GetYearFraction(DateTime start, DateTime end)
        {
            var sign = 1.0;
            if (end < start)
            {
                (start, end) = (end, start);
                sign = -1.0;
            }

            var wholeYears = 0;
            var boundary = start;
            while (boundary.AddYears(1) <= end)
            {
                boundary = boundary.AddYears(1);
                wholeYears++;
            }

            if (boundary == end)
                return sign * wholeYears;

            var remainingDays = CountWeightedDays(boundary, end);
            var fullYearDays = CountWeightedDays(boundary, boundary.AddYears(1));

            return sign * (wholeYears + remainingDays / fullYearDays);
        }

        private double CountWeightedDays(DateTime start, DateTime end)
        {
            var total = 0.0;
            for (var date = start; date < end; date = date.AddDays(1))
            {
                if (calendar.DaysToAlwaysExclude.Contains(date.DayOfWeek))
                    total += weekendWeight;
                else if (calendar.IsHoliday(date))
                    total += holidayWeight;
                else
                    total += 1.0;
            }
            return total;
        }
    }
}
