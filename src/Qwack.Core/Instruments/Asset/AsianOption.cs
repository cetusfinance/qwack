using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Qwack.Core.Basic.Capital;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;
using static System.Math;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianOption : AsianSwap, IHasVega, ISaCcrEnabledCommodity
    {
        public OptionType CallPut { get; set; }

        public new IAssetInstrument Clone() => new AsianOption
        {
            TradeId = TradeId,
            Notional = Notional,
            Direction = Direction,
            AverageStartDate = AverageStartDate,
            AverageEndDate = AverageEndDate,
            FixingDates = (DateTime[])FixingDates.Clone(),
            FixingCalendar = FixingCalendar,
            PaymentCalendar = PaymentCalendar,
            SpotLag = SpotLag,
            SpotLagRollType = SpotLagRollType,
            PaymentLag = PaymentLag,
            PaymentLagRollType = PaymentLagRollType,
            PaymentDate = PaymentDate,
            PaymentCurrency = PaymentCurrency,
            AssetFixingId = AssetFixingId,
            AssetId = AssetId,
            DiscountCurve = DiscountCurve,
            FxConversionType = FxConversionType,
            FxFixingDates = FxFixingDates == null ? null : (DateTime[])FxFixingDates.Clone(),
            FxFixingId = FxFixingId,
            Strike = Strike,
            Counterparty = Counterparty,
            HedgingSet = HedgingSet,
            PortfolioName = PortfolioName,
            CallPut = CallPut
        };

        public new IAssetInstrument SetStrike(double strike)
        {
            var o = (AsianOption)Clone();
            o.Strike = strike;
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

        public override double SupervisoryDelta(IAssetFxModel model) => SaCcrUtils.SupervisoryDelta(Fwd(model), Strike, T(model.BuildDate), CallPut, SupervisoryVol, Notional);
        private double SupervisoryVol => SaCcrParameters.SupervisoryOptionVols[AssetClass];

        public override TO_Instrument ToTransportObject() =>
            new TO_Instrument
            {
                AssetInstrumentType = AssetInstrumentType.AsianOption,
                AsianOption = new TO_AsianOption
                {
                    TradeId = TradeId,
                    Notional = Notional,
                    Direction = Direction,
                    AverageStartDate = AverageStartDate,
                    AverageEndDate = AverageEndDate,
                    FixingDates = (DateTime[])FixingDates.Clone(),
                    FixingCalendar = FixingCalendar?.Name,
                    PaymentCalendar = PaymentCalendar?.Name,
                    SpotLag = SpotLag.ToString(),
                    SpotLagRollType = SpotLagRollType,
                    PaymentLag = PaymentLag.ToString(),
                    PaymentLagRollType = PaymentLagRollType,
                    PaymentDate = PaymentDate,
                    PaymentCurrency = PaymentCurrency,
                    AssetFixingId = AssetFixingId,
                    AssetId = AssetId,
                    DiscountCurve = DiscountCurve,
                    FxConversionType = FxConversionType,
                    FxFixingDates = FxFixingDates == null ? null : (DateTime[])FxFixingDates.Clone(),
                    FxFixingId = FxFixingId,
                    Strike = Strike,
                    Counterparty = Counterparty,
                    HedgingSet = HedgingSet,
                    PortfolioName = PortfolioName,
                    CallPut = CallPut
                }
            };

    }
}
