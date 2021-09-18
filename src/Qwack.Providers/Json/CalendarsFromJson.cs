using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Qwack.Dates;

namespace Qwack.Providers.Json
{
    public class CalendarsFromJson: ICalendarProvider
    {
        private static readonly JsonSerializerSettings _jsonSettings = new() { DateFormatString = "yyyyMMdd" };
        private Dictionary<string, Calendar> _loadedCalendars;
        private CalendarCollection _calendarCollection;

        private CalendarsFromJson()
        {
        }

        public CalendarCollection Collection => _calendarCollection;
        public Dictionary<string, Calendar> OriginalCalendars => _loadedCalendars;

        public static CalendarsFromJson Parse(string jsonString)
        {
            var returnValue = new CalendarsFromJson()
            {
                _loadedCalendars = JsonConvert.DeserializeObject<Dictionary<string, Calendar>>(jsonString, _jsonSettings)
            };
            returnValue._calendarCollection = new CalendarCollection(returnValue._loadedCalendars.Values);
            return returnValue;
        }

        public static CalendarsFromJson Load(string filename) => Parse(System.IO.File.ReadAllText(filename));

        public Calendar GetCalendar(string Name) => _calendarCollection.TryGetCalendar(Name, out var cal) ? cal : throw new Exception($"Unable to find calendar {Name}");
        public Calendar GetCalendarSafe(string Name) => _calendarCollection.TryGetCalendar(Name, out var cal) ? cal : default;

    }
}
