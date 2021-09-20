using System;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Models;

namespace Qwack.Core.Basic
{
    public class FxPair
    {
        public Currency Foreign { get; set; }
        public Currency Domestic { get; set; }
        public Frequency SpotLag { get; set; }
        public Calendar PrimaryCalendar { get; set; }
        public Calendar SecondaryCalendar { get; set; }

        public FxPair() 
        {
            PrimaryCalendar = new Calendar() { Name = "Empty" };
            SecondaryCalendar = new Calendar() { Name = "Empty" };
        }

        public FxPair(TO_FxPair transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider):base()
        {
            Foreign = currencyProvider.GetCurrency(transportObject.Foreign);
            Domestic = currencyProvider.GetCurrency(transportObject.Domestic);
            SpotLag = new Frequency(transportObject.SpotLag);
            PrimaryCalendar = calendarProvider.GetCalendarSafe(transportObject.PrimaryCalendar);
            SecondaryCalendar = calendarProvider.GetCalendarSafe(transportObject.SecondaryCalendar);
        }

        public override bool Equals(object x)
        {
            if (x is not FxPair x1)
            {
                return false;
            }
            return (x1.Foreign == Foreign && x1.Domestic == Domestic && x1.PrimaryCalendar == PrimaryCalendar && x1.SecondaryCalendar == SecondaryCalendar && x1.SpotLag == SpotLag);
        }

        public override int GetHashCode() => HashCode.Combine(Foreign, Domestic, PrimaryCalendar, SecondaryCalendar, SpotLag);

        public override string ToString() => $"{Domestic}/{Foreign}";

        public TO_FxPair GetTransportObject() =>
            new()
            {
                Domestic = Domestic.Ccy,
                Foreign = Foreign.Ccy,
                PrimaryCalendar = PrimaryCalendar.Name,
                SecondaryCalendar = SecondaryCalendar.Name,
                SpotLag = SpotLag.ToString()
            };
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

        public static FxPair FxPairFromString(this string pair, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider) => new()
        {
            Domestic = currencyProvider.GetCurrency(pair.Substring(0, 3)),
            Foreign = currencyProvider.GetCurrency(pair.Substring(pair.Length - 3, 3)),
            PrimaryCalendar = calendarProvider.Collection[pair.Substring(0, 3)],
            SecondaryCalendar = calendarProvider.Collection[pair.Substring(pair.Length - 3, 3)],
            SpotLag = 2.Bd()
        };

        public static DateTime OptionExpiry(this FxPair pair, DateTime valDate, Frequency tenor)
        {
            var spot = pair.SpotDate(valDate);
            var roll = tenor.PeriodType == DatePeriodType.M || tenor.PeriodType == DatePeriodType.Y ? RollType.MF : RollType.F;
            var delivery = spot.AddPeriod(roll, pair.PrimaryCalendar, tenor);
            delivery = delivery.IfHolidayRoll(roll, pair.SecondaryCalendar);
            var expiry = delivery.SubtractPeriod(RollType.P, pair.PrimaryCalendar, pair.SpotLag);
            return expiry;
        }
    }

}
