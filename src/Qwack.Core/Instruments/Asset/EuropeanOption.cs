using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class EuropeanOption : Forward, IHasVega
    {
        public OptionType CallPut { get; set; }

        public new IAssetInstrument Clone() => new EuropeanOption
        {
            TradeId = TradeId,
            Notional = Notional,
            Direction = Direction,
            ExpiryDate = ExpiryDate,
            FixingCalendar = FixingCalendar,
            PaymentCalendar = PaymentCalendar,
            SpotLag = SpotLag,
            PaymentLag = PaymentLag,
            Strike = Strike,
            AssetId = AssetId,
            PaymentCurrency = PaymentCurrency,
            FxFixingId = FxFixingId,
            DiscountCurve = DiscountCurve,
            PaymentDate = PaymentDate,
            Counterparty = Counterparty,
            FxConversionType = FxConversionType,
            HedgingSet = HedgingSet,
            PortfolioName = PortfolioName,
            CallPut = CallPut
        };

        public new IAssetInstrument SetStrike(double strike)
        {
            var o = (EuropeanOption)Clone();
            o.Strike = strike;
            return o;
        }

        public override bool Equals(object obj) => obj is EuropeanOption euroOpt &&
                   CallPut == euroOpt.CallPut &&
                   TradeId == euroOpt.TradeId &&
                   Notional == euroOpt.Notional &&
                   Direction == euroOpt.Direction &&
                   ExpiryDate == euroOpt.ExpiryDate &&
                   EqualityComparer<Calendar>.Default.Equals(FixingCalendar, euroOpt.FixingCalendar) &&
                   EqualityComparer<Calendar>.Default.Equals(PaymentCalendar, euroOpt.PaymentCalendar) &&
                   EqualityComparer<Frequency>.Default.Equals(SpotLag, euroOpt.SpotLag) &&
                   EqualityComparer<Frequency>.Default.Equals(PaymentLag, euroOpt.PaymentLag) &&
                   PaymentDate == euroOpt.PaymentDate &&
                   Strike == euroOpt.Strike &&
                   AssetId == euroOpt.AssetId &&
                   EqualityComparer<Currency>.Default.Equals(PaymentCurrency, euroOpt.PaymentCurrency) &&
                   FxFixingId == euroOpt.FxFixingId &&
                   DiscountCurve == euroOpt.DiscountCurve &&
                   FxConversionType == euroOpt.FxConversionType &&
                   EqualityComparer<Currency>.Default.Equals(Currency, euroOpt.Currency);
    }
}
