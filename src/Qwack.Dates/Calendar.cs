﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public class Calendar
    {
        public Calendar()
        {
            InheritedCalendars = new List<string>();
            DaysToAlwaysExclude = new List<DayOfWeek>();
            DaysToExclude = new HashSet<DateTime>();
            MonthsToExclude = new List<MonthEnum>();
        }

        public bool IsMerged { get; set; }
        public List<string> InheritedCalendars { get; set; }
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
                DaysToAlwaysExclude = this.DaysToAlwaysExclude.ToList(),
                InheritedCalendars = this.InheritedCalendars.ToList(),
                DaysToExclude = new HashSet<DateTime>(this.DaysToExclude),
                MonthsToExclude = this.MonthsToExclude.ToList(),
                Name = this.Name
            };
            return newCalender;
        }
    }
}
