using System.Collections.Generic;

namespace Qwack.Dates
{
    public interface ICalendarProvider
    {
        CalendarCollection Collection { get; }
        Dictionary<string, Calendar> OriginalCalendars { get; }
        Calendar GetCalendar(string Name);
    }
}
