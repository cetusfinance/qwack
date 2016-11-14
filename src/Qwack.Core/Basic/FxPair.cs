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
            var x1 = x as FxPair;
            if (x1 == null)
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
    }
}
