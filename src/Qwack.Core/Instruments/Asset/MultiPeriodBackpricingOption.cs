using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class MultiPeriodBackpricingOption : IHasVega, IAssetInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public OptionType CallPut { get; set; }

        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }

        public Tuple<DateTime, DateTime>[] PeriodDates { get; set; }
        public DateTime DecisionDate { get; set; }
        public DateTime SettlementDate { get; set; }
        public List<DateTime[]> FixingDates { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public RollType SpotLagRollType { get; set; } = RollType.F;
        public Frequency PaymentLag { get; set; }
        public RollType PaymentLagRollType { get; set; } = RollType.F;
        public DateTime PaymentDate { get; set; }
        public string AssetId { get; set; }
        public string AssetFixingId { get; set; }
        public string FxFixingId { get; set; }
        public List<DateTime[]> FxFixingDates { get; set; }
        public Currency PaymentCurrency { get; set; }
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;
        public string DiscountCurve { get; set; }

        public string[] AssetIds => new[] { AssetId };
        public string[] IrCurves(IAssetFxModel model)
        {
            if (FxConversionType == FxConversionType.None)
                return new[] { DiscountCurve };
            else
            {
                var fxCurve = model.FundingModel.FxMatrix.DiscountCurveMap[PaymentCurrency];
                var assetCurveCcy = model.GetPriceCurve(AssetId).Currency;
                var assetCurve = model.FundingModel.FxMatrix.DiscountCurveMap[assetCurveCcy];
                return (new[] { DiscountCurve, fxCurve, assetCurve }).Distinct().ToArray();
            }
        }
        public Currency Currency => PaymentCurrency;

        public DateTime LastSensitivityDate => PaymentDate.Max(PeriodDates.Max(x => x.Item2).AddPeriod(SpotLagRollType, FixingCalendar, SpotLag));

        public string FxPair(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{PaymentCurrency}";
        public FxConversionType FxType(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? FxConversionType.None : FxConversionType;
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => valDate <= FixingDates.Min(x => x.Min()) ?
           new Dictionary<string, List<DateTime>>() :
           new Dictionary<string, List<DateTime>> { { AssetId, FixingDates.SelectMany(x => x).Where(d => d < valDate).ToList() } };


        public IAssetInstrument Clone() => new MultiPeriodBackpricingOption
        {
            TradeId = TradeId,
            Notional = Notional,
            Direction = Direction,
            PeriodDates = PeriodDates,
            DecisionDate = DecisionDate,
            FixingDates = FixingDates.Select(x => (DateTime[])x.Clone()).ToList(),
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
            FxFixingDates = FxFixingDates?.Select(x => (DateTime[])x.Clone()).ToList(),
            FxFixingId = FxFixingId,
            CallPut = CallPut,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            SettlementDate = SettlementDate
        };

        public IAssetInstrument SetStrike(double strike) => throw new InvalidOperationException();

        public TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.MultiPeriodBackpricingOption,
            BackpricingOption = new TO_MultiPeriodBackpricingOption
            {
                TradeId = TradeId,
                Notional = Notional,
                Direction = Direction,
                PeriodDates = PeriodDates,
                DecisionDate = DecisionDate,
                FixingDates = FixingDates?.Select(x => new DateArray((DateTime[])x.Clone()))?.ToList(),
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
                FxFixingDates = FxFixingDates?.Select(x => new DateArray((DateTime[])x.Clone()))?.ToList(),
                FxFixingId = FxFixingId,
                CallPut = CallPut,
                Counterparty = Counterparty,
                PortfolioName = PortfolioName,
                SettlementDate = SettlementDate
            }
        };
    }
}
