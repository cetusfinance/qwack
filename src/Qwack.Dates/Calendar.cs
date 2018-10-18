using System;
using System.Collections.Generic;
using System.Linq;

namespace Qwack.Dates
{
    public class Calendar
    {
        public Calendar()
        {
            InheritedCalendar = new List<string>();
            DaysToAlwaysExclude = new List<DayOfWeek>();
            DaysToExclude = new HashSet<DateTime>();
            MonthsToExclude = new List<MonthEnum>();
        }

        public bool IsMerged { get; set; }
        public List<string> InheritedCalendar { get; set; }
        public string Name { get; set; }
        public List<DayOfWeek> DaysToAlwaysExclude { get; set; }
        public HashSet<DateTime> DaysToExclude { get; set; }
        public List<MonthEnum> MonthsToExclude { get; set; }

        public bool IsHoliday(DateTime date)
        {
            if (MonthsToExclude.Contains((MonthEnum)date.Month))
            {
                return true;
            }
            if (DaysToAlwaysExclude.Contains(date.DayOfWeek))
            {
                return true;
            }
            else if (DaysToExclude.Contains(date.Date))
            {
                return true;
            }

            return false;
        }
        public Calendar Clone()
        {
            var newCalender = new Calendar()
            {
                DaysToAlwaysExclude = DaysToAlwaysExclude.ToList(),
                InheritedCalendar = InheritedCalendar.ToList(),
                DaysToExclude = new HashSet<DateTime>(DaysToExclude),
                MonthsToExclude = MonthsToExclude.ToList(),
                Name = Name
            };
            return newCalender;
        }
        public Calendar Merge(Calendar otherCalendar)
        {
            var newCalender = new Calendar()
            {
                DaysToAlwaysExclude = DaysToAlwaysExclude.Concat(otherCalendar.DaysToAlwaysExclude).Distinct().ToList(),
                InheritedCalendar = InheritedCalendar.Concat(otherCalendar.InheritedCalendar).Distinct().ToList(),
                DaysToExclude = new HashSet<DateTime>(DaysToExclude.Concat(otherCalendar.DaysToExclude).Distinct()),
                MonthsToExclude = MonthsToExclude.Concat(otherCalendar.MonthsToExclude).Distinct().ToList(),
                Name = Name
            };
            return newCalender;
        }
    }
}
