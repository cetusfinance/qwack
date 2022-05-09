using Qwack.Core.Basic;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class ETC : CashAsset
    {
        public ETC() : base() { }
        public ETC(double notional, string assetId, Currency ccy, double scalingFactor, Frequency settleLag, Calendar settleCalendar)
            : base(notional, assetId, ccy, scalingFactor, settleLag, settleCalendar)
        {
        }
    }
}
