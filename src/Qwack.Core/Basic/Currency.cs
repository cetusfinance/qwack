using Qwack.Dates;

namespace Qwack.Core.Basic
{
    /// <summary>
    /// The basic currency class that contains the static data needed to describe a currency
    /// </summary>
    public class Currency
    {
        public Currency(string ccy, DayCountBasis dayCount, Calendar settlementCalendar)
        {
            Ccy = ccy;
            DayCount = dayCount;
            SettlementCalendar = settlementCalendar;
        }

        public string Ccy { get; set;}
        public DayCountBasis DayCount { get; set;}
        public Calendar SettlementCalendar { get; }
        public string SettlementCalendarName { get;set;}

        public override bool Equals(object x)
        {
            var x1 = x as Currency;
            return (x1 != null) && (x1.Ccy == Ccy);
        }

        public override int GetHashCode() => Ccy.GetHashCode();

        public static bool operator ==(Currency x, Currency y)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            // If one is null, but not both, return false.
            if ((x is null) || (y is null))
            {
                return false;
            }
            return x.Ccy == y.Ccy;
        }

        public static bool operator !=(Currency x, Currency y) => !(x == y);

        public new string ToString()
        {
            return Ccy;
        }
    }
}
