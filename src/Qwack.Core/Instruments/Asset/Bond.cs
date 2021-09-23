using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class Bond : CashAsset
    {
        public Bond() : base() { }
        public Bond(double notional, string assetId, Currency ccy, double scalingFactor, Frequency settleLag, Calendar settleCalendar)
            : base(notional, assetId, ccy, scalingFactor, settleLag, settleCalendar)
        {
        }

        public Bond(TO_Bond to, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
            : base(to.Notional, to.AssetId, currencyProvider.GetCurrencySafe(to.Currency), to.ScalingFactor, new Frequency(to.SettleLag), calendarProvider.GetCalendarSafe(to.SettleCalendar))
        {
            TradeId = to.TradeId;
            SettleDate = to.SettleDate;
            Price = to.Price;
        }

        public TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.Bond,
            Bond = new TO_Bond()
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
