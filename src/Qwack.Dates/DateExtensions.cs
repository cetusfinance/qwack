using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public static class DateExtensions
    {
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
            return SubtractPeriod(D, RollType.P, calendar, DatePeriod.BusinessDay, 1);
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

        public static DateTime AddPeriod(this DateTime date, RollType rollType, Calendar calendars, Frequency frequency)
        {
            return AddPeriod(date, rollType, calendars, frequency.PeriodType, frequency.PeriodCount);
        }

        public static DateTime AddPeriod(this DateTime date, RollType rollType, Calendar calendar, DatePeriod periodType, int numberOfPeriods)
        {
            date = date.Date;
            if (numberOfPeriods == 0)
            {
                return IfHolidayRoll(date, rollType, calendar);
            }
            if (periodType == DatePeriod.B)
            {
                //Business day jumping so we need to do something different
                DateTime d = date;
                for (int i = 0; i < numberOfPeriods; i++)
                {
                    d = d.AddDays(1);
                    d = IfHolidayRoll(d, rollType, calendar);
                }
                return d;
            }

            DateTime dt;
            switch (periodType)
            {
                case DatePeriod.D:
                    dt = date.AddDays(numberOfPeriods);
                    break;
                case DatePeriod.M:
                    dt = date.AddMonths(numberOfPeriods);
                    break;
                case DatePeriod.W:
                    dt = date.AddDays(numberOfPeriods * 7);
                    break;
                case DatePeriod.Y:
                default:
                    dt = date.AddYears(numberOfPeriods);
                    break;
            }

            if ((rollType == RollType.MF_LIBOR) & (date == date.LastBusinessDayOfMonth(calendar)))
            {
                dt = date.LastBusinessDayOfMonth(calendar);
            }
            if (rollType == RollType.ShortFLongMF)
            {
                if (periodType == DatePeriod.B || periodType == DatePeriod.D || periodType == DatePeriod.W)
                    return IfHolidayRoll(dt, RollType.F, calendar);
                else
                    return IfHolidayRoll(dt, RollType.MF, calendar);
            }
            return IfHolidayRoll(dt, rollType, calendar);
        }

        public static DateTime SubtractPeriod(this DateTime date, RollType rollType, Calendar calendar, Frequency frequency)
        {
            return SubtractPeriod(date, rollType, calendar, frequency.PeriodType, frequency.PeriodCount);
        }

        public static DateTime SubtractPeriod(this DateTime date, RollType rollType, Calendar calendar, DatePeriod periodType, int numberOfPeriods)
        {
            date = date.Date;
            if (numberOfPeriods == 0)
            {
                return IfHolidayRoll(date, rollType, calendar);
            }

            if (periodType == DatePeriod.B)
            {
                //Business day jumping so we need to do something different
                DateTime d = date;
                for (int i = 0; i < numberOfPeriods; i++)
                {
                    d = d.AddDays(-1);
                    d = IfHolidayRoll(d, rollType, calendar);
                }

                return d;
            }
            return AddPeriod(date, rollType, calendar, periodType, 0 - numberOfPeriods);
        }

    }
}
