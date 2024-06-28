using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class Equity : CashAsset
    {
        public Equity() : base() { }
        public Equity(double notional, string assetId, Currency ccy, double scalingFactor, Frequency settleLag, Calendar settleCalendar)
            : base(notional, assetId, ccy, scalingFactor, settleLag, settleCalendar)
        {
        }

        public Equity(TO_Equity to, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
            : base(to.Notional, to.AssetId, currencyProvider.GetCurrencySafe(to.Currency), to.ScalingFactor, new Frequency(to.SettleLag??"0b"), calendarProvider.GetCalendarSafe(to.SettleCalendar??to.Currency))
        {
            TradeId = to.TradeId;
            SettleDate = to.SettleDate;
            Price = to.Price;
        }

        public TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.Equity,
            Equity = new TO_Equity()
            {
                AssetId = AssetId,
                Notional = Notional,
                Counterparty = Counterparty,
                Currency = Currency?.Ccy,
                Price = Price,
                ScalingFactor = ScalingFactor,
                SettleCalendar = SettleCalendar?.Name,
                SettleDate = SettleDate,
                PortfolioName = PortfolioName,
                TradeId = TradeId,
                SettleLag = SettleLag.ToString(),
                MetaData = new(MetaData)
            }
        };
    }
}
