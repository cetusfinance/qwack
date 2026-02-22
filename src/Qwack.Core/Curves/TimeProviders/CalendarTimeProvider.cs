using System;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves.TimeProviders
{
    public class CalendarTimeProvider(DayCountBasis dayCountBasis = DayCountBasis.Act365F) : ITimeProvider
    {
        public double GetYearFraction(DateTime start, DateTime end)
        {
            return start.CalculateYearFraction(end, dayCountBasis);
        }
    }
}
