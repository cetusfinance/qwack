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
            FalsePositives = new HashSet<DateTime>();
            InheritedCalendarObjects = new List<Calendar>();
        }

        public bool IsMerged { get; set; }
        public List<string> InheritedCalendar { get; set; }
        public List<Calendar> InheritedCalendarObjects{ get; set; }
        public string Name { get; set; }
        public List<DayOfWeek> DaysToAlwaysExclude { get; set; }
        public HashSet<DateTime> DaysToExclude { get; set; }
        public List<MonthEnum> MonthsToExclude { get; set; }

        public CalendarType CalendarType { get; set; }
        public DateTime FixedDate { get; set; }
        public int ValidFromYear { get; set; }
        public int ValidToYear { get; set; }
        public HashSet<DateTime> FalsePositives { get; set; }


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

            foreach(var ic in InheritedCalendarObjects)
            {
                if (ic.IsHoliday(date))
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
                    return dateThisyear == date && !FalsePositives.Contains(date);
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
            if (otherCalendar.CalendarType != CalendarType)
                throw new Exception("Cannot merge calendars of differing types");
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

        public override bool Equals(object obj) => obj is Calendar calendar && GetHashCode()==calendar.GetHashCode();

        public override int GetHashCode()
        {
            var hashCode = 471772613;
            hashCode = hashCode * -1521134295 + IsMerged.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<List<string>>.Default.GetHashCode(InheritedCalendar);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<DayOfWeek>>.Default.GetHashCode(DaysToAlwaysExclude);
            hashCode = hashCode * -1521134295 + EqualityComparer<HashSet<DateTime>>.Default.GetHashCode(DaysToExclude);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<MonthEnum>>.Default.GetHashCode(MonthsToExclude);
            hashCode = hashCode * -1521134295 + CalendarType.GetHashCode();
            hashCode = hashCode * -1521134295 + FixedDate.GetHashCode();
            hashCode = hashCode * -1521134295 + ValidFromYear.GetHashCode();
            hashCode = hashCode * -1521134295 + ValidToYear.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<HashSet<DateTime>>.Default.GetHashCode(FalsePositives);
            return hashCode;
        }
    }
}
