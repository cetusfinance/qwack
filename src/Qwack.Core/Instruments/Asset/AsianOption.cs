using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianOption : AsianSwap, IHasVega
    {
        public OptionType CallPut { get; set; }

        public new IAssetInstrument Clone()
        {
            var o = (AsianOption)base.Clone();
            o.CallPut = CallPut;
            return o;
        }

        public new IAssetInstrument SetStrike(double strike)
        {
            var o = (AsianOption)base.SetStrike(strike);
            o.CallPut = CallPut;
            return o;
        }

        public override bool Equals(object obj) => obj is AsianOption option &&
                   CallPut == option.CallPut &&
                   AverageStartDate == option.AverageStartDate &&
                   AverageEndDate == option.AverageEndDate &&
                   AssetId == option.AssetId &&
                   AssetFixingId == option.AssetFixingId &&
                   Currency == option.Currency &&
                   Direction == option.Direction &&
                   DiscountCurve == option.DiscountCurve &&
                   FixingCalendar == option.FixingCalendar &&
                   Enumerable.SequenceEqual(FixingDates, option.FixingDates) &&
                   FxConversionType == option.FxConversionType &&
                   FxFixingId == option.FxFixingId &&
                   Notional == option.Notional &&
                   PaymentCalendar == option.PaymentCalendar &&
                   PaymentCurrency == option.PaymentCurrency &&
                   PaymentDate == option.PaymentDate &&
                   PaymentLag == option.PaymentLag &&
                   PaymentLagRollType == option.PaymentLagRollType &&
                   SpotLag == option.SpotLag &&
                   SpotLagRollType == option.SpotLagRollType &&
                   Strike == option.Strike &&
                   TradeId == option.TradeId;
    }
}
