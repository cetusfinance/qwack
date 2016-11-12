using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates.Providers
{
    public interface ICalendarProvider
    {
        CalendarCollection Collection { get; }
        Dictionary<string, Calendar> OriginalCalendars { get; }
    }
}