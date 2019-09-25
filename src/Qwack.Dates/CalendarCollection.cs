using System;
using System.Collections.Generic;
using System.Linq;

namespace Qwack.Dates
{
    public class CalendarCollection
    {
        private readonly Dictionary<string, Calendar> _mergedCalendars = new Dictionary<string, Calendar>(StringComparer.OrdinalIgnoreCase);

        public CalendarCollection(IEnumerable<Calendar> startingCalendars)
        {
            var calendarsToCheck = new Queue<Calendar>(startingCalendars);
            var numberOfCalendersInLastCheck = calendarsToCheck;
            while (calendarsToCheck.Count > 0)
            {
                var currentCalendar = calendarsToCheck.Dequeue();
                if (!currentCalendar.InheritedCalendar.All(inher => _mergedCalendars.ContainsKey(inher)))
                {
                    if (!startingCalendars.Any(c => c.Name == currentCalendar.Name))
                    {
                        //log calendar referencing a non-existing parent
                    }
                    else
                    {
                        calendarsToCheck.Enqueue(currentCalendar);
                    }
                    continue;
                }

                foreach (var dc in currentCalendar.InheritedCalendar)
                {
                    MergeCalendar(_mergedCalendars[dc], currentCalendar);
                    currentCalendar.InheritedCalendarObjects.Add(_mergedCalendars[dc]);
                }
                _mergedCalendars.Add(currentCalendar.Name, currentCalendar);
            }
        }

        public Calendar this[string calendarName]
        {
            get
            {
                if (TryGetCalendar(calendarName, out var returnCalendar))
                {
                    return returnCalendar;
                }
                throw new ArgumentOutOfRangeException(nameof(calendarName),$"Could not find or make a calendar called {calendarName}");
            }
        }

        public string[] ListCalendarNames() => _mergedCalendars.Keys.ToArray();


        public bool TryGetCalendar(string calendarName, out Calendar calendar)
        {
            if (calendarName.Contains("+"))
            {
                if (_mergedCalendars.TryGetValue(calendarName, out calendar))
                {
                    return true;
                }
                return TryGetCalendar(calendarName.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries), out calendar);
            }
            return _mergedCalendars.TryGetValue(calendarName, out calendar);

        }

        public bool TryGetCalendar(string[] calendarsNames, out Calendar calendar)
        {
            var key = string.Join("+", calendarsNames);
            if (_mergedCalendars.TryGetValue(key, out calendar))
            {
                return true;
            }
            //We couldn't find it so lets generate it
            calendar = new Calendar {Name = key};

            for (var i = 0; i < calendarsNames.Length; i++)
            {
                MergeCalendar(_mergedCalendars[calendarsNames[i]], calendar);
            }
            _mergedCalendars[key] = calendar;
            return true;

        }

        private static void MergeCalendar(Calendar calendar, Calendar newCal)
        {
            foreach (var mtoEx in calendar.MonthsToExclude)
            {
                if (!newCal.MonthsToExclude.Contains(mtoEx))
                {
                    newCal.MonthsToExclude.Add(mtoEx);
                }
            }
            foreach (var diw in calendar.DaysToAlwaysExclude)
            {
                if (!newCal.DaysToAlwaysExclude.Contains(diw))
                {
                    newCal.DaysToAlwaysExclude.Add(diw);
                }
            }
            foreach (var dte in calendar.DaysToExclude)
            {
                newCal.DaysToExclude.Add(dte);
            }
        }
    }
}
