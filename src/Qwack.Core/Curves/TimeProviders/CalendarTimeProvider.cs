using System;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Curves;

namespace Qwack.Core.Curves.TimeProviders
{
    public class CalendarTimeProvider(DayCountBasis dayCountBasis = DayCountBasis.Act365F) : ITimeProvider
    {
        public double GetYearFraction(DateTime start, DateTime end) 
            => start.CalculateYearFraction(end, dayCountBasis);

        TO_ITimeProvider ITimeProvider.ToTransportObject() => new()
        {
            CalendarTimeProvider = new TO_CalendarTimeProvider
            {
                DayCountBasis = dayCountBasis
            }
        };
    }
}
