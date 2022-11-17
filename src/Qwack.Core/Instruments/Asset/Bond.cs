using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class Bond : CashAsset
    {
        public double? Coupon { get;set; }
        public CouponFrequency CouponFrequency { get; set; } = CouponFrequency.SemiAnnual;

        public DateTime? IssueDate { get; set; }
        public DateTime? MaturityDate { get; set; }
        public DateTime? FirstCouponDate { get; set; }

        public Dictionary<DateTime, double> CallSchedule { get; set; } = new();
        public Dictionary<DateTime, double> SinkingSchedule { get; set; } = new();


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
                MetaData = new(MetaData),
                Coupon = Coupon,
                CallSchedule = CallSchedule,
                SinkingSchedule = SinkingSchedule,
                CouponFrequency = CouponFrequency,
                FirstCouponDate = FirstCouponDate,
                IssueDate = IssueDate,
                MaturityDate = MaturityDate
            }
        };
    }
}
