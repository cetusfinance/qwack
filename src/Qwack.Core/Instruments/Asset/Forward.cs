using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class Forward : IAssetInstrument, ISaCcrEnabled
    {
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }
        public DateTime ExpiryDate { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public Frequency PaymentLag { get; set; }
        public DateTime PaymentDate { get; set; }
        public double Strike { get; set; }
        public string AssetId { get; set; }
        public Currency PaymentCurrency { get; set; }
        public string FxFixingId { get; set; }
        public string DiscountCurve { get; set; }
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;

        public Currency Currency => PaymentCurrency;

        private bool IsFx => AssetId.Length == 7 && AssetId[3] == '/';

        public string[] IrCurves(IAssetFxModel model)
        {
            if (IsFx)
            {
                var ccys = AssetId.Split('/');
                var c1 = model.FundingModel.FxMatrix.GetDiscountCurve(ccys[0]);
                var c2 = model.FundingModel.FxMatrix.GetDiscountCurve(ccys[1]);
                return (new[] { DiscountCurve, c1, c2 }).Distinct().ToArray();
            }
            else
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
        }

        public AsianSwap AsBulletSwap() => new AsianSwap
        {
            TradeId = TradeId,
            Notional = Notional,
            Direction = Direction,
            AverageStartDate = ExpiryDate,
            AverageEndDate = ExpiryDate,
            FixingCalendar = FixingCalendar,
            PaymentCalendar = PaymentCalendar,
            SpotLag = SpotLag,
            PaymentLag = PaymentLag,
            Strike = Strike,
            AssetId = AssetId,
            PaymentCurrency = PaymentCurrency,
            FxFixingId = FxFixingId,
            DiscountCurve = DiscountCurve,
            FixingDates = new[] { ExpiryDate },
            FxFixingDates = new[] { ExpiryDate },
            FxConversionType = FxConversionType.AverageThenConvert,
            PaymentDate = PaymentDate,
            Counterparty = Counterparty,
            HedgingSet = HedgingSet,
            PortfolioName = PortfolioName,
        };

        public IAssetInstrument Clone() => new Forward
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
        };

        public IAssetInstrument SetStrike(double strike)
        {
            var o = (Forward)Clone();
            o.Strike = strike;
            return o;
        }

        public string[] AssetIds => new[] { AssetId };

        public DateTime LastSensitivityDate => PaymentDate.Max(ExpiryDate);

        public FxConversionType FxType(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? FxConversionType.None : FxConversionType;
        public string FxPair(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{PaymentCurrency}";

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => valDate <= ExpiryDate ?
            new Dictionary<string, List<DateTime>>() :
            new Dictionary<string, List<DateTime>> { { AssetId, new List<DateTime> { ExpiryDate } } };

        public override bool Equals(object obj) => obj is Forward forward &&
                   TradeId == forward.TradeId &&
                   Notional == forward.Notional &&
                   Direction == forward.Direction &&
                   ExpiryDate == forward.ExpiryDate &&
                   EqualityComparer<Calendar>.Default.Equals(FixingCalendar, forward.FixingCalendar) &&
                   EqualityComparer<Calendar>.Default.Equals(PaymentCalendar, forward.PaymentCalendar) &&
                   EqualityComparer<Frequency>.Default.Equals(SpotLag, forward.SpotLag) &&
                   EqualityComparer<Frequency>.Default.Equals(PaymentLag, forward.PaymentLag) &&
                   PaymentDate == forward.PaymentDate &&
                   Strike == forward.Strike &&
                   AssetId == forward.AssetId &&
                   EqualityComparer<Currency>.Default.Equals(PaymentCurrency, forward.PaymentCurrency) &&
                   FxFixingId == forward.FxFixingId &&
                   DiscountCurve == forward.DiscountCurve &&
                   FxConversionType == forward.FxConversionType &&
                   EqualityComparer<Currency>.Default.Equals(Currency, forward.Currency);

        public double EffectiveNotional(IAssetFxModel model) => AsBulletSwap().EffectiveNotional(model);
        public double AdjustedNotional(IAssetFxModel model) => AsBulletSwap().AdjustedNotional(model);
        public virtual double SupervisoryDelta(IAssetFxModel model) => 1.0;
        public double MaturityFactor(DateTime today) => AsBulletSwap().MaturityFactor(today);
        public string HedgingSet { get; set; }

        internal double Fwd(IAssetFxModel model)
        {
            var fxRate = model.GetPriceCurve(AssetId).Currency == Currency ?
                1.0 :
                model.FundingModel.GetFxRate(ExpiryDate, model.GetPriceCurve(AssetId).Currency, Currency);
            return model.GetPriceCurve(AssetId).GetPriceForFixingDate(ExpiryDate) * fxRate;
        }

        public TO_Instrument ToTransportObject() =>
           new TO_Instrument
           {
               AssetInstrumentType = AssetInstrumentType.Forward,
               Forward = new TO_Forward
               {
                   TradeId = TradeId,
                   Notional = Notional,
                   Direction = Direction,
                   ExpiryDate = ExpiryDate,
                   FixingCalendar = FixingCalendar.Name,
                   PaymentCalendar = PaymentCalendar.Name,
                   SpotLag = SpotLag.ToString(),
                   PaymentLag = PaymentLag.ToString(),
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
               }
           };
    }
}
