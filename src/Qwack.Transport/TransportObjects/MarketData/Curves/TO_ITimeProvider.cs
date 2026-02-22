using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    public class TO_ITimeProvider
    {
        public TO_CalendarTimeProvider CalendarTimeProvider { get; set; }
        public TO_BusinessDayTimeProvider BusinessDayTimeProvider { get; set; }

    }

    public class TO_CalendarTimeProvider
    {
        public DayCountBasis DayCountBasis { get; set; }
    }

    public class TO_BusinessDayTimeProvider
    {
        public string Calendar { get; set; }
        public double WeekendWeight { get; set; }
        public double HolidayWeight { get; set; }
    }
}
