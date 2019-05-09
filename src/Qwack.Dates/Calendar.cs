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
            CalendarType = CalendarType.Regular;
            ValidFromYear = int.MinValue;
            ValidToYear = int.MaxValue;
        }

        public bool IsMerged { get; set; }
        public List<string> InheritedCalendar { get; set; }
        public string Name { get; set; }
        public List<DayOfWeek> DaysToAlwaysExclude { get; set; }
        public HashSet<DateTime> DaysToExclude { get; set; }
        public List<MonthEnum> MonthsToExclude { get; set; }

        public CalendarType CalendarType { get; set; }
        public DateTime FixedDate { get; set; }
        public int ValidFromYear { get; set; }
        public int ValidToYear { get; set; }

        public bool IsHoliday(DateTime date)
        {
            if (CalendarType != CalendarType.Regular)
                return IsHolidayFromRules(date);

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

        private bool IsHolidayFromRules(DateTime date)
        {
            
            switch(CalendarType)
            {
                case CalendarType.EasterGoodFriday:
                    return DateExtensions.EasterGauss(date.Year).AddDays(-2) == date;
                case CalendarType.EasterMonday:
                    return DateExtensions.EasterGauss(date.Year).AddDays(1) == date;
                case CalendarType.FixedDateZARule:
                    if (date.Year > ValidToYear || date.Year < ValidFromYear)
                        return false;
                    var dateThisyear = new DateTime(date.Year, FixedDate.Month, FixedDate.Day);
                    if (dateThisyear.DayOfWeek == DayOfWeek.Sunday)
                        dateThisyear = dateThisyear.AddDays(1);
                    return dateThisyear == date;
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

        public override bool Equals(object obj) => obj is Calendar calendar && Name == calendar.Name;
    }
}
