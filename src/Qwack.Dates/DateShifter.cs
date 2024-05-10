using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Basic;

namespace Qwack.Dates
{
    public class DateShifter
    {
        public Frequency Period { get; set; }
        public RollType RollType { get; set; }
        public Calendar Calendar { get; set; }

        public DateShifter() { }
        public DateShifter(TO_DateShifter to, ICalendarProvider calendarProvider)
        {
            Period = new Frequency(to.Period);
            RollType = to.RollType;
            Calendar = calendarProvider.GetCalendarSafe(to.Calendar);
        }

        public TO_DateShifter GetTransportObject() => new()
        {
            Calendar = Calendar?.Name,
            RollType = RollType,
            Period = Period.ToString()
        };
            
    }
}
