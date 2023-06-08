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
    public class AsianSwap : IAssetInstrument, ISaCcrEnabledCommodity
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }
        public DateTime AverageStartDate { get; set; }
        public DateTime AverageEndDate { get; set; }
        public DateTime[] FixingDates { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public RollType SpotLagRollType { get; set; } = RollType.F;
        public Frequency PaymentLag { get; set; }
        public RollType PaymentLagRollType { get; set; } = RollType.F;
        public DateTime PaymentDate { get; set; }
        public double Strike { get; set; }
        public string AssetId { get; set; }
        public string AssetFixingId { get; set; }
        public string FxFixingId { get; set; }
        public DateTime[] FxFixingDates { get; set; }
        public Currency PaymentCurrency { get; set; }
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;
        public string DiscountCurve { get; set; }
        public string HedgingSet { get; set; }

        public AsianSwap() { }

        public string[] AssetIds => new[] { AssetId };
        public string[] IrCurves(IAssetFxModel model)
        {
            if (!IsFx && FxConversionType == FxConversionType.None && model.GetPriceCurve(AssetId).Currency == PaymentCurrency)
                return new[] { DiscountCurve };
            else
            {
                if (IsFx)
                {
                    var fxCurve = model.FundingModel.FxMatrix.DiscountCurveMap[PaymentCurrency];
                    var ccy1 = model.FundingModel.GetCurrency(AssetId.Split('/')[0]);
                    var ccy2 = model.FundingModel.GetCurrency(AssetId.Split('/')[1]);
                    var ccy1Curve = model.FundingModel.FxMatrix.DiscountCurveMap[ccy1];
                    var ccy2Curve = model.FundingModel.FxMatrix.DiscountCurveMap[ccy2];
                    var assetCurveCcy = model.GetPriceCurve(AssetId).Currency;
                    var assetCurve = model.FundingModel.FxMatrix.DiscountCurveMap[assetCurveCcy];
                    return (new[] { DiscountCurve, fxCurve, assetCurve, ccy1Curve, ccy2Curve }).Distinct().ToArray();
                }
                else
                {
                    var fxCurve = model.FundingModel.FxMatrix.DiscountCurveMap[PaymentCurrency];
                    var assetCurveCcy = model.GetPriceCurve(AssetId).Currency;
                    var assetCurve = model.FundingModel.FxMatrix.DiscountCurveMap[assetCurveCcy];
                    return (new[] { DiscountCurve, fxCurve, assetCurve }).Distinct().ToArray();
                }
            }
        }
        public Currency Currency => PaymentCurrency;

        private bool IsFx => AssetId.Length == 7 && AssetId[3] == '/';

        public DateTime LastSensitivityDate => PaymentDate.Max(AverageEndDate.AddPeriod(SpotLagRollType, FixingCalendar, SpotLag));

        public IAssetInstrument Clone() => new AsianSwap
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
            MetaData = MetaData,
            CommodityType = CommodityType,
            AssetClass = AssetClass,
        };

        public IAssetInstrument SetStrike(double strike)
        {
            var c = (AsianSwap)Clone();
            c.Strike = strike;
            return c;
        }

        public FxConversionType FxType(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? FxConversionType.None : FxConversionType;

        public string FxPair(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{PaymentCurrency}";

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => valDate <= FixingDates.First() ?
                new Dictionary<string, List<DateTime>>() :
                new Dictionary<string, List<DateTime>> { { AssetId, FixingDates.Where(d => d < valDate).ToList() } };


        public double EffectiveNotional(IAssetFxModel model, double? MPOR) => SupervisoryDelta(model) * AdjustedNotional(model) * MaturityFactor(model.BuildDate, MPOR);
        public double AdjustedNotional(IAssetFxModel model) => Notional * Fwd(model);
        public virtual double SupervisoryDelta(IAssetFxModel model) => 1.0;
        internal double T(DateTime today) => today.CalculateYearFraction(LastSensitivityDate, DayCountBasis.Act365F);
        internal double Fwd(IAssetFxModel model)
        {
            var fxRate = model.GetPriceCurve(AssetId).Currency == Currency ?
                1.0 :
                model.FundingModel.GetFxAverage(FixingDates, model.GetPriceCurve(AssetId).Currency, Currency);
            return model.GetPriceCurve(AssetId).GetAveragePriceForDates(FixingDates) * fxRate;
        }
        public double MaturityFactor(DateTime today, double? MPOR) => MPOR.HasValue ? SaCcrUtils.MfMargined(MPOR.Value) : SaCcrUtils.MfUnmargined(T(today));
        public double TradeNotional(IAssetFxModel model) => System.Math.Abs(Notional) * Fwd(model);
        public SaCcrAssetClass AssetClass { get; set; }
        public string CommodityType { get; set; }
        public override bool Equals(object obj) => obj is AsianSwap swap &&
                   TradeId == swap.TradeId &&
                   Counterparty == swap.Counterparty &&
                   PortfolioName == swap.PortfolioName &&
                   Notional == swap.Notional &&
                   Direction == swap.Direction &&
                   AverageStartDate == swap.AverageStartDate &&
                   AverageEndDate == swap.AverageEndDate &&
                   Enumerable.SequenceEqual(FixingDates ?? Array.Empty<DateTime>(), swap.FixingDates ?? Array.Empty<DateTime>()) &&
                   EqualityComparer<Calendar>.Default.Equals(FixingCalendar, swap.FixingCalendar) &&
                   EqualityComparer<Calendar>.Default.Equals(PaymentCalendar, swap.PaymentCalendar) &&
                   EqualityComparer<Frequency>.Default.Equals(SpotLag, swap.SpotLag) &&
                   SpotLagRollType == swap.SpotLagRollType &&
                   EqualityComparer<Frequency>.Default.Equals(PaymentLag, swap.PaymentLag) &&
                   PaymentLagRollType == swap.PaymentLagRollType &&
                   PaymentDate == swap.PaymentDate &&
                   Strike == swap.Strike &&
                   AssetId == swap.AssetId &&
                   AssetFixingId == swap.AssetFixingId &&
                   FxFixingId == swap.FxFixingId &&
                   Enumerable.SequenceEqual(FxFixingDates ?? Array.Empty<DateTime>(), swap.FxFixingDates ?? Array.Empty<DateTime>()) &&
                   EqualityComparer<Currency>.Default.Equals(PaymentCurrency, swap.PaymentCurrency) &&
                   FxConversionType == swap.FxConversionType &&
                   DiscountCurve == swap.DiscountCurve &&
                   EqualityComparer<Currency>.Default.Equals(Currency, swap.Currency) &&
                   HedgingSet == swap.HedgingSet;

        public virtual TO_Instrument ToTransportObject() =>
            new()
            {
                AssetInstrumentType = AssetInstrumentType.AsianSwap,
                AsianSwap = new TO_AsianSwap
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
                    MetaData = MetaData,
                }
            };
    }
}

