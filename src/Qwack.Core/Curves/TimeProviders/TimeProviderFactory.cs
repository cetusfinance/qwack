using System;
using Qwack.Dates;
using Qwack.Transport.TransportObjects.MarketData.Curves;

namespace Qwack.Core.Curves.TimeProviders
{
    public static class TimeProviderFactory
    {
        public static ITimeProvider CreateTimeProvider(this TO_ITimeProvider toTimeProvider, ICalendarProvider calendarProvider)
        {
            if (toTimeProvider.CalendarTimeProvider != null)
            {
                return new CalendarTimeProvider(toTimeProvider.CalendarTimeProvider.DayCountBasis);
            }
            else if (toTimeProvider.BusinessDayTimeProvider != null)
            {
                var calendar = calendarProvider.GetCalendar(toTimeProvider.BusinessDayTimeProvider.Calendar);
                return new BusinessDayTimeProvider(calendar, toTimeProvider.BusinessDayTimeProvider.WeekendWeight, toTimeProvider.BusinessDayTimeProvider.HolidayWeight);
            }
            throw new ArgumentException("Invalid TO_ITimeProvider");
        }
    }
}
