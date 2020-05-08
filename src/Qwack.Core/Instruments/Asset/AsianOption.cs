using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;
using static System.Math;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianOption : AsianSwap, IHasVega
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

        private double SupervisoryVol => HedgingSet == "Electricity" ? 1.5 : 0.7;
        public new double SupervisoryDelta(IAssetFxModel model) =>
          BlackDelta(Fwd(model), Strike, 0.0, model.BuildDate.CalculateYearFraction(FixingDates.Last(), DayCountBasis.Act365F), SupervisoryVol, CallPut);

        private static double BlackDelta(double forward, double strike, double riskFreeRate, double expTime, double volatility, OptionType CP)
        {
            double d1, d2, DF;
            DF = Exp(-riskFreeRate * expTime);
            d1 = (Log(forward / strike) + (expTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expTime));
            d2 = d1 - volatility * Sqrt(expTime);

            return (CP == OptionType.Put) ? DF * (Math.Statistics.NormSDist(d1) - 1) : DF * Math.Statistics.NormSDist(d1);
        }

        public new TO_Instrument ToTransportObject()
        {
            var swapTO = base.ToTransportObject().AsianSwap;
            var aoTO = (TO_AsianOption)swapTO;
            aoTO.CallPut = CallPut;
            return new TO_Instrument
            {
                AssetInstrumentType = AssetInstrumentType.AsianOption,
                AsianOption = aoTO
            };
        }
           
    }
}
