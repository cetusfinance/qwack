using System;
using Qwack.Dates;

namespace Qwack.Core.Basic
{
    public class FxPair
    {
        public Currency Foreign { get; set; }
        public Currency Domestic { get; set; }
        public Frequency SpotLag { get; set; }
        public Calendar SettlementCalendar { get; set; }

        public override bool Equals(object x)
        {
            if (!(x is FxPair x1))
            {
                return false;
            }
            return (x1.Foreign == Foreign && x1.Domestic == Domestic && x1.SettlementCalendar == SettlementCalendar && x1.SpotLag == SpotLag);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = Foreign.GetHashCode();
                result = (result * 397) ^ Domestic.GetHashCode();
                result = (result * 397) ^ SettlementCalendar.GetHashCode();
                result = (result * 397) ^ SpotLag.GetHashCode();
                return result;
            }
        }

        public new string ToString => $"{Domestic}/{Foreign}";
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
            var d = valDate.AddPeriod(RollType.F, fxPair.SettlementCalendar, fxPair.SpotLag);
            d = d.IfHolidayRollForward(fxPair.SettlementCalendar);
            return d;
        }

        public static FxPair FxPairFromString(this string pair, ICurrencyProvider currencyProvider) => new FxPair
        {
            Domestic = currencyProvider[pair.Substring(0, 3)],
            Foreign = currencyProvider[pair.Substring(pair.Length - 3, 3)],
        };
    }

}
