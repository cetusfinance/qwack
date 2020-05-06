using System;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Basic
{
    public class FxPair
    {
        public Currency Foreign { get; set; }
        public Currency Domestic { get; set; }
        public Frequency SpotLag { get; set; }
        public Calendar PrimaryCalendar { get; set; }
        public Calendar SecondaryCalendar { get; set; }

        public override bool Equals(object x)
        {
            if (!(x is FxPair x1))
            {
                return false;
            }
            return (x1.Foreign == Foreign && x1.Domestic == Domestic && x1.PrimaryCalendar == PrimaryCalendar && x1.SecondaryCalendar == SecondaryCalendar && x1.SpotLag == SpotLag);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = Foreign.GetHashCode();
                result = (result * 397) ^ Domestic.GetHashCode();
                result = (result * 397) ^ PrimaryCalendar.GetHashCode();
                result = (result * 397) ^ SecondaryCalendar.GetHashCode();
                result = (result * 397) ^ SpotLag.GetHashCode();
                return result;
            }
        }

        public override string ToString() => $"{Domestic}/{Foreign}";
    }

    public static class FxPairEx
    {
        /// <summary>
        /// Returns spot date for a given val date
        /// e.g. for USD/ZAR, calendar would be for ZAR and otherCal would be for USD
        /// </summary>
        /// <param name="valDate"></param>
        /// <param name="spotLag"></param>
        /// <param name="calendar"></param>
        /// <param name="otherCal"></param>
        /// <returns></returns>
        public static DateTime SpotDate(this FxPair fxPair, DateTime valDate)
        {
            var d = valDate.AddPeriod(RollType.F, fxPair.PrimaryCalendar, fxPair.SpotLag);
            d = d.IfHolidayRollForward(fxPair.SecondaryCalendar);
            return d;
        }

        public static FxPair FxPairFromString(this string pair, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider) => new FxPair
        {
            Domestic = currencyProvider.GetCurrency(pair.Substring(0, 3)),
            Foreign = currencyProvider.GetCurrency(pair.Substring(pair.Length - 3, 3)),
            PrimaryCalendar = calendarProvider.Collection[pair.Substring(0, 3)],
            SecondaryCalendar = calendarProvider.Collection[pair.Substring(pair.Length - 3, 3)],
            SpotLag = 2.Bd()
        };
    }

}
