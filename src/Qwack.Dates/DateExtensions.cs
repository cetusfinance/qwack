using System;
using System.Linq;
using System.Collections.Generic;

namespace Qwack.Dates
{
    /// <summary>
    /// Business date extension methods
    /// </summary>
    public static class DateExtensions
    {
        private const double _ticksFraction360 = 1.0 / (TimeSpan.TicksPerDay * 360.0);
        private static readonly double _ticksFraction365 = 1.0 / (TimeSpan.TicksPerDay * 365.0);
        private static readonly string[] _months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
        private static readonly string[] _futureMonths = { "F", "G", "H", "J", "K", "M", "N", "Q", "U", "V", "X", "Z" };
        /// <summary>
        /// Gets the next IMM date for a given input date. Returns 3rd Wednesday in March, June, September or December.  
        /// If an IMM date is given as the input, the following IMM date will be returned.
        /// </summary>
        /// <param name="input">The reference date</param>
        /// <returns></returns>
        public static DateTime GetNextImmDate(this DateTime input)
        {
            var m = input.Month;
            var y = input.Year;

            //handle case of date before 3rd weds in IMM month
            if (m % 3 == 0 && input < ThirdWednesday(input))
            {
                return ThirdWednesday(input);
            }

            m = m - m % 3 + 3; //roll to next IMM month
            if (m <= 12) return ThirdWednesday(new DateTime(y, m, 1));
            m -= 12;
            y++;
            return ThirdWednesday(new DateTime(y, m, 1));
        }

        /// <summary>
        /// Gets the previous IMM date for a given input date. Returns 3rd Wednesday in March, June, September or December.  
        /// If an IMM date is given as the input, the previous IMM date will be returned.
        /// </summary>
        /// <param name="input">The reference date</param>
        /// <returns></returns>
        public static DateTime GetPrevImmDate(this DateTime input)
        {
            var m = input.Month;
            var y = input.Year;

            //handle case of date after 3rd weds in IMM month
            if (m % 3 == 0 && input > ThirdWednesday(input))
                return ThirdWednesday(input);

            m = m - (m % 3 == 0 ? 3 : m % 3); //roll to next IMM month
            if (m >= 1) return ThirdWednesday(new DateTime(y, m, 1));
            m += 12;
            y--;
            return ThirdWednesday(new DateTime(y, m, 1));
        }

        /// <summary>
        /// Gets the next occurence of a specific weekday from the input date
        /// If the input date is the target day of the week, the following occurence is returned
        /// </summary>
        /// <param name="input">The reference date</param>
        /// <returns></returns>
        public static DateTime GetNextWeekday(this DateTime input, DayOfWeek weekDay)
        {
            var d = input.DayOfWeek;
            if(d<weekDay)
            {
                var deltaD = weekDay - d;
                return input.AddDays(deltaD);
            }
            else
            {
                var deltaD = 7 - (int)d + (int)weekDay;
                return input.AddDays(deltaD);
            }
        }

        /// <summary>
        /// Returns a list of business dates according to a specified calendar which are contained within two given dates.  
        /// Start and end dates are treated as inclusive
        /// </summary>
        /// <param name="startDateInc"></param>
        /// <param name="endDateInc"></param>
        /// <param name="calendars"></param>
        /// <returns></returns>
        public static List<DateTime> BusinessDaysInPeriod(this DateTime startDateInc, DateTime endDateInc, Calendar calendars)
        {
            if (endDateInc < startDateInc)
            {
                throw new ArgumentException(nameof(endDateInc), "End date is before the start date");
            }
            var o = new List<DateTime>((int)(endDateInc - startDateInc).TotalDays);
            var date = startDateInc.IfHolidayRollForward(calendars);
            while (date <= endDateInc)
            {
                o.Add(date);
                date = date.AddPeriod(RollType.F, calendars, 1.Bd());
            }
            return o;
        }

        /// <summary>
        /// Returns a list of calendar dates which are contained within two given dates.  
        /// Start and end dates are treated as inclusive
        /// </summary>
        /// <param name="startDateInc"></param>
        /// <param name="endDateInc"></param>
        /// <returns></returns>
        public static List<DateTime> CalendarDaysInPeriod(this DateTime startDateInc, DateTime endDateInc)
        {
            if (endDateInc < startDateInc)
            {
                throw new ArgumentException(nameof(endDateInc), "End date is before the start date");
            }
            var o = new List<DateTime>((int)(endDateInc - startDateInc).TotalDays);
            var date = startDateInc;
            while (date <= endDateInc)
            {
                o.Add(date);
                date = date.AddDays(1);
            }
            return o;
        }

        /// <summary>
        /// Returns a list of friday dates according to a specified calendar which are contained within two given dates.  
        /// If a friday is a holiday, the preceeding good business day is returned  
        /// Start and end dates are treated as inclusive
        /// </summary>
        /// <param name="startDateInc"></param>
        /// <param name="endDateInc"></param>
        /// <param name="calendars"></param>
        /// <returns></returns>
        public static List<DateTime> FridaysInPeriod(this DateTime startDateInc, DateTime endDateInc, Calendar calendars)
        {
            if (endDateInc < startDateInc)
            {
                throw new ArgumentException(nameof(endDateInc), "End date is before the start date");
            }
            var o = new List<DateTime>((int)(endDateInc - startDateInc).TotalDays);
            var date = startDateInc;
            while (date <= endDateInc)
            {
                if (date.DayOfWeek == DayOfWeek.Friday)
                {
                    o.Add(date.IfHolidayRoll(RollType.P, calendars));
                }
                date = date.AddPeriod(RollType.None, null, 1.Day());
            }
            return o;
        }

        /// <summary>
        /// Calculates a year fraction from a day count method and two dates
        /// Start date is inclusive, end date exclusive
        /// </summary>
        /// <param name="startDate">Start Date (inclusive)</param>
        /// <param name="endDate">End Date (exclusive)</param>
        /// <param name="basis">DayCountBasis enum</param>
        /// <param name="ignoreTimeComponent">Ignore the time component of the DateTime inputs - defaults to true</param>
        /// <param name="calendar">Optional calendar object, required only for methods involving business days</param>
        /// <returns></returns>
        public static double CalculateYearFraction(this DateTime startDate, DateTime endDate, DayCountBasis basis, bool ignoreTimeComponent = true, Calendar calendar = null)
        {
            if (ignoreTimeComponent)
            {
                startDate = startDate.Date;
                endDate = endDate.Date;
            }

            switch (basis)
            {
                case DayCountBasis.Act_360:
                    return (endDate.Ticks - startDate.Ticks) * _ticksFraction360;
                case DayCountBasis.Act_365F:
                    return (endDate.Ticks - startDate.Ticks) * _ticksFraction365;
                case DayCountBasis.Act_Act_ISDA:
                case DayCountBasis.Act_Act:
                    if (endDate.Year == startDate.Year)
                    {   //simple case
                        var eoY = new DateTime(endDate.Year, 12, 31);
                        return (endDate - startDate).TotalDays / eoY.DayOfYear;
                    }
                    else
                    {
                        double nIntermediateYears = endDate.Year - startDate.Year - 1;

                        var eoYe = new DateTime(endDate.Year, 12, 31);
                        var e = endDate.DayOfYear / (double)eoYe.DayOfYear;

                        var eoYs = new DateTime(startDate.Year, 12, 31);
                        var s = (eoYs - startDate).TotalDays / eoYs.DayOfYear;

                        return s + nIntermediateYears + e;
                    }
                case DayCountBasis._30_360:
                    double ydiff = endDate.Year - startDate.Year;
                    double mdiff = endDate.Month - startDate.Month;
                    double ddiff = endDate.Day - startDate.Day;
                    return (ydiff * 360 + mdiff * 30 + ddiff) / 360;
                case DayCountBasis.ThirtyE360:
                    double d1E = Math.Min(startDate.Day, 30);
                    double d2E = Math.Min(endDate.Day, 30);
                    double ydiffE = endDate.Year - startDate.Year;
                    double mdiffE = endDate.Month - startDate.Month;
                    var ddiffE = d2E - d1E;
                    return (ydiffE * 360 + mdiffE * 30 + ddiffE) / 360;
                case DayCountBasis.Bus252:
                    return startDate.BusinessDaysInPeriod(endDate.AddDays(-1), calendar).Count / 252.0;
                case DayCountBasis.Unity:
                    return 1.0;
            }
            return -1;
        }

        /// <summary>
        /// Calculates a year fraction from a day count method and two dates
        /// Start date is inclusive, end date exclusive
        /// </summary>
        /// <param name="startDate">Start Date (inclusive)</param>
        /// <param name="endDate">End Date (exclusive)</param>
        /// <param name="basis">DayCountBasis enum</param>
        /// <returns></returns>
        public static double CalculateYearFraction(this DayCountBasis basis, DateTime startDate, DateTime endDate) => startDate.CalculateYearFraction(endDate, basis);

        /// <summary>
        /// Adds a year fraction to a date to return an end date
        /// </summary>
        /// <param name="startDate">Start Date</param>
        /// <param name="yearFraction">Year fraction in format consistent with basis parameter</param>
        /// <param name="basis">DayCountBasis enum</param>
        /// <returns></returns>
        public static DateTime AddYearFraction(this DateTime startDate, double yearFraction, DayCountBasis basis, bool ignoreTimeComponent=true)
        {
            var o = new DateTime();
            switch (basis)
            {
                case DayCountBasis.Act_360:
                    o = new DateTime((long)(startDate.Ticks +  yearFraction / _ticksFraction360));
                    break;
                case DayCountBasis.Act_365F:
                    o = new DateTime((long)(startDate.Ticks + yearFraction / _ticksFraction365));
                    break;
            }

            if (ignoreTimeComponent)
                o = o.Date;

            return o;
        }


        /// <summary>
        /// Returns first business day (according to specified calendar) of month in which the input date falls
        /// </summary>
        /// <param name="input">Input date</param>
        /// <param name="calendar">Calendar</param>
        /// <returns></returns>
        public static DateTime FirstBusinessDayOfMonth(this DateTime input, Calendar calendar)
        {
            var returnDate = input.FirstDayOfMonth();
            if (calendar != null)
            {
                returnDate = returnDate.IfHolidayRollForward(calendar);
            }
            return returnDate;
        }

        /// <summary>
        /// Returns first calendar day of the months in which the input date falls
        /// </summary>
        /// <param name="input">Input date</param>
        /// <returns></returns>
        public static DateTime FirstDayOfMonth(this DateTime input) => new DateTime(input.Year, input.Month, 1);

        /// <summary>
        /// Returns last business day, according to the specified calendar, of the month in which the input date falls
        /// </summary>
        /// <param name="input">Input date</param>
        /// <param name="calendar">Calendar</param>
        /// <returns></returns>
        public static DateTime LastBusinessDayOfMonth(this DateTime input, Calendar calendar)
        {
            var d = input.Date.AddMonths(1).FirstDayOfMonth();
            return SubtractPeriod(d, RollType.P, calendar, 1.Bd());
        }

        /// <summary>
        /// Returns the third wednesday of the month in which the input date falls
        /// </summary>
        /// <param name="date">Input date</param>
        /// <returns></returns>
        public static DateTime ThirdWednesday(this DateTime date) => date.NthSpecificWeekDay(DayOfWeek.Wednesday, 3);

        /// <summary>
        /// Returns the Nth instance of a specific week day in the month in which the input date falls
        /// E.g. NthSpecificWeekDay(date,DayOfWeek.Wednesday, 3) would return the third wednesday of the month in which the input date falls
        /// </summary>
        /// <param name="date">Input date</param>
        /// <param name="dayofWeek">DayOfWeek enum</param>
        /// <param name="number">N</param>
        /// <returns></returns>
        public static DateTime NthSpecificWeekDay(this DateTime date, DayOfWeek dayofWeek, int number)
        {
            //Get the first day of the month
            var firstDate = new DateTime(date.Year, date.Month, 1);
            //Get the current day 0=sunday
            var currentDay = (int)firstDate.DayOfWeek;
            var targetDow = (int)dayofWeek;

            int daysToAdd;

            if (currentDay == targetDow)
                return firstDate.AddDays((number - 1) * 7);

            if (currentDay < targetDow)
            {
                daysToAdd = targetDow - currentDay;
            }
            else
            {
                daysToAdd = 7 + targetDow - currentDay;
            }

            return firstDate.AddDays(daysToAdd).AddDays((number - 1) * 7);

        }

        /// <summary>
        /// Returns the Nth instance of a specific week day (from the end of the month) in the month in which the input date falls
        /// E.g. NthSpecificWeekDay(date,DayOfWeek.Wednesday, 2) would return the 2nd last wednesday of the month in which the input date falls
        /// </summary>
        /// <param name="date">Input date</param>
        /// <param name="dayofWeek">DayOfWeek enum</param>
        /// <param name="number">N</param>
        /// <returns></returns>
        public static DateTime NthLastSpecificWeekDay(this DateTime date, DayOfWeek dayofWeek, int number)
        {
            //Get the first day of the month
            var lastDate = LastDayOfMonth(date);
            //Get the current day 0=sunday
            var currentDay = (int)lastDate.DayOfWeek;
            var targetDow = (int)dayofWeek;

            int daysToAdd;

            if (currentDay == targetDow)
                return lastDate.AddDays(-(number - 1) * 7);

            if (currentDay > targetDow)
            {
                daysToAdd = currentDay - targetDow;
            }
            else
            {
                daysToAdd = 7 + currentDay - targetDow;
            }

            return lastDate.AddDays(-daysToAdd).AddDays(-(number - 1) * 7);
        }

        /// <summary>
        /// Returns the last calendar day of the month in which the input date falls
        /// </summary>
        /// <param name="input">Input date</param>
        /// <returns></returns>
        public static DateTime LastDayOfMonth(this DateTime input)
        {
            if (input.Month != 12)
            {
                return new DateTime(input.Year, input.Month + 1, 1).AddDays(-1);
            }
            else
            {
                return new DateTime(input.Year + 1, 1, 1).AddDays(-1);
            }
        }

        /// <summary>
        /// Returns the input date, adjusted by rolling forward if the input date falls on a holiday according to the specified calendar
        /// </summary>
        /// <param name="input">Input date</param>
        /// <param name="calendar">Calendar</param>
        /// <returns></returns>
        public static DateTime IfHolidayRollForward(this DateTime input, Calendar calendar)
        {
            if (calendar == null)
                return input;

            input = input.Date;
            while (calendar.IsHoliday(input))
            {
                input = input.AddDays(1);
            }
            return input;
        }

        /// <summary>
        /// Returns the input date, adjusted by rolling backwards if the input date falls on a holiday according to the specified calendar
        /// </summary>
        /// <param name="input">Input date</param>
        /// <param name="calendar">Calendar</param>
        /// <returns></returns>
        public static DateTime IfHolidayRollBack(this DateTime input, Calendar calendar)
        {
            if (calendar == null)
                return input;

            while (calendar.IsHoliday(input))
            {
                input = input.AddDays(-1);
            }
            return input;
        }

        /// <summary>
        /// Returns the input date, adjusted by rolling if the input date falls on a holiday according to the specified calendar.  
        /// The type of roll is specfied in the input.
        /// </summary>
        /// <param name="date">Input date</param>
        /// <param name="rollType">RollType enum</param>
        /// <param name="calendar">Calendar</param>
        /// <returns></returns>
        public static DateTime IfHolidayRoll(this DateTime date, RollType rollType, Calendar calendar)
        {
            if (calendar == null)
                return date;

            date = date.Date;
            DateTime d, d1, d2;
            double distFwd, distBack;

            switch (rollType)
            {
                case RollType.F:
                    return date.IfHolidayRollForward(calendar);
                case RollType.MF:
                default:
                    d = date.IfHolidayRollForward(calendar);
                    if (d.Month == date.Month)
                    {
                        return d;
                    }
                    else
                    {
                        return date.IfHolidayRollBack(calendar);
                    }
                case RollType.P:
                    return date.IfHolidayRollBack(calendar);
                case RollType.MP:
                    d = date.IfHolidayRollBack(calendar);
                    if (d.Month == date.Month)
                    {
                        return d;
                    }
                    else
                    {
                        return date.IfHolidayRollForward(calendar);
                    }
                case RollType.NearestFollow:
                    d1 = date.IfHolidayRollForward(calendar);
                    d2 = date.IfHolidayRollBack(calendar);
                    distFwd = (d1 - date).TotalDays;
                    distBack = (date - d2).TotalDays;
                    if (distBack < distFwd)
                    {
                        return d2;
                    }
                    else
                    {
                        return d1;
                    }
                case RollType.NearestPrev:
                    d1 = date.IfHolidayRollForward(calendar);
                    d2 = date.IfHolidayRollBack(calendar);
                    distFwd = (d1 - date).TotalDays;
                    distBack = (date - d2).TotalDays;
                    if (distFwd < distBack)
                    {
                        return d1;
                    }
                    else
                    {
                        return d2;
                    }
                case RollType.LME:
                    d1 = date.IfHolidayRollForward(calendar);
                    if (d1.Month != date.Month)
                    {
                        return date.IfHolidayRollBack(calendar);
                    }
                    d2 = date.IfHolidayRollBack(calendar);
                    if (d2.Month != date.Month)
                    {
                        return d1;
                    }
                    distFwd = (d1 - date).TotalDays;
                    distBack = (date - d2).TotalDays;
                    if (distBack < distFwd)
                    {
                        return d2;
                    }
                    else
                    {
                        return d1;
                    }
                case RollType.None:
                    return date;
            }
        }

        /// <summary>
        /// Returns a date equal to the input date plus the specified period, adjusted for holidays
        /// </summary>
        /// <param name="date">Input date</param>
        /// <param name="rollType">RollType enum</param>
        /// <param name="calendar">Calendar</param>
        /// <param name="datePeriod">Period to add in the form of a Frequency object</param>
        /// <returns></returns>
        public static DateTime AddPeriod(this DateTime date, RollType rollType, Calendar calendar, Frequency datePeriod)
        {
            if (calendar == null && datePeriod.PeriodType==DatePeriodType.B) return date;

            date = date.Date;
            if (datePeriod.PeriodCount == 0)
            {
                return IfHolidayRoll(date, rollType, calendar);
            }

            if (datePeriod.PeriodType == DatePeriodType.B)
            {
                if (datePeriod.PeriodCount < 0) //actually a subtract
                {
                    return date.SubtractPeriod(rollType, calendar, new Frequency(-datePeriod.PeriodCount, datePeriod.PeriodType));
                }

                //Business day jumping so we need to do something different
                var d = date;
                for (var i = 0; i < datePeriod.PeriodCount; i++)
                {
                    d = d.AddDays(1);
                    d = IfHolidayRoll(d, rollType, calendar);
                }
                return d;
            }

            DateTime dt;
            switch (datePeriod.PeriodType)
            {
                case DatePeriodType.D:
                    dt = date.AddDays(datePeriod.PeriodCount);
                    break;
                case DatePeriodType.M:
                    dt = date.AddMonths(datePeriod.PeriodCount);
                    break;
                case DatePeriodType.W:
                    dt = date.AddDays(datePeriod.PeriodCount * 7);
                    break;
                default:
                    dt = date.AddYears(datePeriod.PeriodCount);
                    break;
            }

            if ((rollType == RollType.MF_LIBOR) && (date == date.LastBusinessDayOfMonth(calendar)))
            {
                dt = date.LastBusinessDayOfMonth(calendar);
            }
            if (rollType == RollType.ShortFLongMF)
            {
                if (datePeriod.PeriodType == DatePeriodType.B || datePeriod.PeriodType == DatePeriodType.D || datePeriod.PeriodType == DatePeriodType.W)
                    return IfHolidayRoll(dt, RollType.F, calendar);
                else
                    return IfHolidayRoll(dt, RollType.MF, calendar);
            }
            return IfHolidayRoll(dt, rollType, calendar);
        }

        public static DateTime[] AddPeriod(this DateTime[] dates, RollType rollType, Calendar calendar, Frequency datePeriod) => dates.Select(d => d.AddPeriod(rollType, calendar, datePeriod)).ToArray();

        /// <summary>
        /// Returns a date equal to the input date minus the specified period, adjusted for holidays
        /// </summary>
        /// <param name="date">Input date</param>
        /// <param name="rollType">RollType enum</param>
        /// <param name="calendar">Calendar</param>
        /// <param name="datePeriod">Period to add in the form of a Frequency object</param>
        /// <returns></returns>
        public static DateTime SubtractPeriod(this DateTime date, RollType rollType, Calendar calendar, Frequency datePeriod)
        {
            date = date.Date;
            if (datePeriod.PeriodCount == 0)
            {
                return IfHolidayRoll(date, rollType, calendar);
            }

            if (datePeriod.PeriodType == DatePeriodType.B)
            {
                //Business day jumping so we need to do something different
                var d = date;
                for (var i = 0; i < datePeriod.PeriodCount; i++)
                {
                    d = d.AddDays(-1);
                    d = IfHolidayRoll(d, rollType, calendar);
                }

                return d;
            }
            return AddPeriod(date, rollType, calendar, new Frequency(0 - datePeriod.PeriodCount, datePeriod.PeriodType));
        }

        /// <summary>
        /// Returns the lesser of two DateTime objects
        /// </summary>
        /// <param name="dateA"></param>
        /// <param name="dateB"></param>
        /// <returns></returns>
        public static DateTime Min(this DateTime dateA, DateTime dateB) => dateA < dateB ? dateA : dateB;

        /// <summary>
        /// Returns the greater of two DateTime objects
        /// </summary>
        /// <param name="dateA"></param>
        /// <param name="dateB"></param>
        /// <returns></returns>
        public static DateTime Max(this DateTime dateA, DateTime dateB) => dateA > dateB ? dateA : dateB;

        /// <summary>
        /// Returns the average of two DateTime objects
        /// </summary>
        /// <param name="dateA"></param>
        /// <param name="dateB"></param>
        /// <returns></returns>
        public static DateTime Average(this DateTime dateA, DateTime dateB) => new DateTime((dateB.Ticks + dateA.Ticks) / 2);

        /// <summary>
        /// Returns the start and end dates for a specified period string
        /// e.g. CAL19, Q120, CAL-20, JAN-22, BALMO
        /// </summary>
        /// <param name="dateA"></param>
        /// <param name="dateB"></param>
        /// <returns></returns>
        public static (DateTime Start, DateTime End) ParsePeriod(this string period)
        {
            switch (period.ToUpper())
            {
                case string p when p.StartsWith("BALM"):
                    return (Start: DateTime.Today, End: (DateTime.Today).LastDayOfMonth());
                case string p when p.StartsWith("CAL"):
                    if (!int.TryParse(p.Substring(3).Trim('-',' '), out var y))
                        throw new Exception($"Could not parse year from {period}");
                    return (Start: new DateTime(y + 2000, 1, 1), End: new DateTime(y + 2000, 12, 31));
                case string p when p.Length == 2 && int.TryParse(p.Substring(1,1), out var yr) && _futureMonths.Contains(p.Substring(0,1)): //X8
                    var m1 = Array.IndexOf(_futureMonths, p.Substring(0, 1)) +1;
                    return (Start: new DateTime(2010 + yr, m1, 1), End: (new DateTime(2010 + yr, m1, 1)).LastDayOfMonth()); ;
                case string p when p.Length == 3 && int.TryParse(p.Substring(1, 2), out var yr) && _futureMonths.Contains(p.Substring(0, 1)): //X18
                    var m2 = Array.IndexOf(_futureMonths, p.Substring(0, 1)) + 1;
                    return (Start: new DateTime(2000 + yr, m2, 1), End: (new DateTime(2000 + yr, m2, 1)).LastDayOfMonth()); ;
                case string p when p.StartsWith("Q"):
                    if (!int.TryParse(p.Substring(1, 1), out var q))
                        throw new Exception($"Could not parse quarter from {period}");
                    if (!int.TryParse(p.Substring(2).Trim('-',' '), out var yq))
                        throw new Exception($"Could not parse year from {period}");
                    return (Start: new DateTime(2000 + yq, 3 * (q - 1) + 1, 1), End: (new DateTime(2000 + yq, 3 * q, 1)).LastDayOfMonth());
                case string p when p.StartsWith("H"):
                    if (!int.TryParse(p.Substring(1, 1), out var h))
                        throw new Exception($"Could not parse half-year from {period}");
                    if (!int.TryParse(p.Substring(2).Trim('-',' '), out var yh))
                        throw new Exception($"Could not parse year from {period}");
                    return (Start: new DateTime(2000 + yh, (h-1)*6+1, 1), End: (new DateTime(2000 + yh, h * 6, 1)).LastDayOfMonth());
                case string p when _months.Any(x => x == p.Substring(0, 3)):
                    if (!int.TryParse(p.Substring(3).Trim('-', ' '), out var ym))
                        throw new Exception($"Could not parse year from {period}");
                    var m = _months.ToList().IndexOf(p.Substring(0, 3))+1;
                    return (Start: new DateTime(ym + 2000, m, 1), End: (new DateTime(ym + 2000, m, 1)).LastDayOfMonth());
                default:
                    throw new Exception($"Could not parse period {period}");
            }
        }

        public static int SingleDigitYear(int fullYear)
        {
            var floor = (int)Math.Floor(fullYear / 10.0) * 10;
            return fullYear - floor;
        }

        public static int DoubleDigitYear(int fullYear)
        {
            var floor = (int)Math.Floor(fullYear / 100.0) * 100;
            return fullYear - floor;
        }

        /// <summary>
        /// Returns spot date for a given val date
        /// e.g. for USD/ZAR, calendar would be for ZAR and otherCal would be for USD
        /// </summary>
        /// <param name="valDate"></param>
        /// <param name="spotLag"></param>
        /// <param name="calendar"></param>
        /// <param name="otherCal"></param>
        /// <returns></returns>
        public static DateTime SpotDate(this DateTime valDate, Frequency spotLag, Calendar calendar, Calendar otherCal)
        {
            var d = valDate.AddPeriod(RollType.F, calendar, spotLag);
            d = d.IfHolidayRollForward(otherCal);
            return d;
        }

        //http://www.henk-reints.nl/easter/index.htm?frame=easteralg2.htm
        public static DateTime EasterGauss(int year)
        {
            var P = year / 100;
            var Q = (3 * P + 3) / 4;
            var R = (8 * P + 13) / 25;
            var M = (15 + Q - R) % 30;
            var N = (4 + Q) % 7;

            var a = year % 19;
            var b = year % 4;
            var c = year % 7;
            var d = (19 * a + M) % 30;
            var e = (2 * b + 4 * c + 6 * d + N) % 7;

            var f = 22 + d + e;
            if (f == 57 || (f == 56 && e == 6 && a > 10))
                f -= 7;

            var ge = (new DateTime(year, 3, 1)).AddDays(f-1);
            return ge;
        }

        /// <summary>
        /// Returns the dates of westerns easter for a given year
        /// </summary>
        /// <param name="dateInYear"></param>
        /// <returns>
        /// GoodFriday, EasterMonday
        /// </returns>
        public static (DateTime GoodFriday, DateTime EasterMonday) Easter(this DateTime dateInYear)
        {
            var easterSunday = EasterGauss(dateInYear.Year);
            return (easterSunday.AddDays(-2), easterSunday.AddDays(1));
        }
    }
}
