using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public static class DateExtensions
    {
        public static DateTime GetNextIMMDate(this DateTime input)
        {
            int m = input.Month;
            int d = input.Day;
            int y = input.Year;

            //handle case of date before 3rd weds in IMM month
            if (m % 3 == 0 && input < ThirdWednesday(input))
                return ThirdWednesday(input);

            m = m - m % 3 + 3; //roll to next IMM month
            if (m > 12)
            {
                m -= 12;
                y++;
            }
            return ThirdWednesday(new DateTime(y, m, 1));
        }

        public static DateTime GetPrevIMMDate(this DateTime input)
        {
            int m = input.Month;
            int d = input.Day;
            int y = input.Year;

            //handle case of date after 3rd weds in IMM month
            if (m % 3 == 0 && input > ThirdWednesday(input))
                return ThirdWednesday(input);

            m = m - m % 3; //roll to next IMM month
            if (m < 1)
            {
                m += 12;
                y--;
            }
            return ThirdWednesday(new DateTime(y, m, 1));
        }

        public static List<DateTime> BusinessDaysInPeriod(this DateTime startDateInc, DateTime endDateInc, Calendar calendars)
        {
            var o = new List<DateTime>((int)(startDateInc - endDateInc).TotalDays);
            var date = startDateInc;
            while (date <= endDateInc)
            {
                o.Add(date);
                date = date.AddPeriod(RollType.F, calendars, Frequency.OneBd);
            }
            return o;
        }

        public static double CalculateYearFraction(this DateTime startDate, DateTime endDate, DayCountBasis basis)
        {
            switch (basis)
            {
                case DayCountBasis.Act_360:
                    return (endDate - startDate).TotalDays / 360;
                case DayCountBasis.Act_365F:
                    return (endDate - startDate).TotalDays / 365;
                case DayCountBasis.Act_Act_ISDA:
                case DayCountBasis.Act_Act:
                    if (endDate.Year == startDate.Year)
                    {   //simple case
                        DateTime EoY = new DateTime(endDate.Year, 12, 31);
                        return (endDate - startDate).TotalDays / EoY.DayOfYear;
                    }
                    else
                    {
                        double nIntermediateYears = endDate.Year - startDate.Year - 1;

                        DateTime EoYe = new DateTime(endDate.Year, 12, 31);
                        double E = endDate.DayOfYear / EoYe.DayOfYear;

                        DateTime EoYs = new DateTime(startDate.Year, 12, 31);
                        double S = (EoYs - startDate).TotalDays / EoYs.DayOfYear;

                        return S + nIntermediateYears + E;
                    }
                case DayCountBasis._30_360:
                    double Ydiff = endDate.Year - startDate.Year;
                    double Mdiff = endDate.Month - startDate.Month;
                    double Ddiff = endDate.Day - startDate.Day;
                    return (Ydiff * 360 + Mdiff * 30 + Ddiff) / 360;
                case DayCountBasis.Unity:
                    return 1.0;
            }
            return -1;
        }

        public static DateTime FirstBusinessDayOfMonth(this DateTime input, Calendar calendar)
        {
            var returnDate = input.FirstDayOfMonth();
            if (calendar != null)
            {
                returnDate = returnDate.IfHolidayRollForward(calendar);
            }
            return returnDate;
        }

        public static DateTime FirstDayOfMonth(this DateTime input)
        {
            return new DateTime(input.Year, input.Month, 1);
        }

        public static DateTime LastBusinessDayOfMonth(this DateTime input, Calendar calendar)
        {
            DateTime D = input.Date.AddMonths(1).FirstDayOfMonth();
            return SubtractPeriod(D, RollType.P, calendar, Frequency.OneBd);
        }

        public static DateTime ThirdWednesday(this DateTime date)
        {
            return date.NthSpecificWeekDay(DayOfWeek.Wednesday, 3);
        }

        public static DateTime NthSpecificWeekDay(this DateTime date, DayOfWeek dayofWeek, int number)
        {
            //Get the first day of the month
            DateTime firstDate = new DateTime(date.Year, date.Month, 1);
            //Get the current day 0=sunday
            int currentDay = (int)firstDate.DayOfWeek;
            int targetDOW = (int)dayofWeek;

            int daysToAdd = 0;

            if (currentDay == targetDOW)
                return firstDate.AddDays((number - 1) * 7);

            if (currentDay < targetDOW)
            {
                daysToAdd = targetDOW - currentDay;
            }
            else
            {
                daysToAdd = 7 + targetDOW - currentDay;
            }

            return firstDate.AddDays(daysToAdd).AddDays((number - 1) * 7);

        }

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

        public static DateTime IfHolidayRollForward(this DateTime input, Calendar calendar)
        {
            input = input.Date;
            while (calendar.IsHoliday(input))
            {
                input = input.AddDays(1);
            }
            return input;
        }
        public static DateTime IfHolidayRollBack(this DateTime input, Calendar calendar)
        {
            while (calendar.IsHoliday(input))
            {
                input = input.AddDays(-1);
            }
            return input;
        }
        public static DateTime IfHolidayRoll(this DateTime date, RollType rollType, Calendar calendar)
        {
            date = date.Date;
            DateTime d, d1, d2;
            double distFwd, distBack;

            switch (rollType)
            {
                case RollType.F:
                    return date.IfHolidayRollForward(calendar);
                default:
                case RollType.MF:
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
                case RollType.NearestFolow:
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
            }
        }

        public static DateTime AddPeriod(this DateTime date, RollType rollType, Calendar calendar, Frequency datePeriod)
        {
            date = date.Date;
            if (datePeriod.PeriodCount == 0)
            {
                return IfHolidayRoll(date, rollType, calendar);
            }
            if (datePeriod.PeriodType == DatePeriodType.B)
            {
                //Business day jumping so we need to do something different
                DateTime d = date;
                for (int i = 0; i < datePeriod.PeriodCount; i++)
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
                case DatePeriodType.Y:
                default:
                    dt = date.AddYears(datePeriod.PeriodCount);
                    break;
            }

            if ((rollType == RollType.MF_LIBOR) & (date == date.LastBusinessDayOfMonth(calendar)))
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
                DateTime d = date;
                for (int i = 0; i < datePeriod.PeriodCount; i++)
                {
                    d = d.AddDays(-1);
                    d = IfHolidayRoll(d, rollType, calendar);
                }

                return d;
            }
            datePeriod.PeriodCount = 0 - datePeriod.PeriodCount;
            return AddPeriod(date, rollType, calendar, datePeriod);
        }

    }
}
