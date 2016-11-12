using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public class CalendarCollection
    {
        private Dictionary<string, Calendar> _mergedCalendars = new Dictionary<string, Calendar>(StringComparer.OrdinalIgnoreCase);

        public CalendarCollection(IEnumerable<Calendar> startingCalendars)
        {
            Queue<Calendar> calendarsToCheck = new Queue<Calendar>(startingCalendars);

            while (calendarsToCheck.Count > 0)
            {
                var currentCalendar = calendarsToCheck.Dequeue();
                if (!currentCalendar.InheritedCalendars.All(inher => _mergedCalendars.ContainsKey(inher)))
                {
                    calendarsToCheck.Enqueue(currentCalendar);
                    continue;
                }

                foreach (var dc in currentCalendar.InheritedCalendars)
                {
                    MergeCalendar(_mergedCalendars[dc], currentCalendar);
                }
                _mergedCalendars.Add(currentCalendar.Name, currentCalendar);
            }
        }

        public bool TryGetCalendar(string calendarName, out Calendar calendar)
        {
            if (calendarName.Contains("+"))
            {
                if (_mergedCalendars.TryGetValue(calendarName, out calendar))
                {
                    return true;
                }
                return TryGetCalendar(calendarName.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries), out calendar);
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
            calendar = new Calendar();
            calendar.Name = key;

            for (int i = 0; i < calendarsNames.Length; i++)
            {
                MergeCalendar(_mergedCalendars[calendarsNames[i]], calendar);
            }
            _mergedCalendars[key] = calendar;
            return true;

        }

        private void MergeCalendar(Calendar calendar, Calendar newCal)
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
