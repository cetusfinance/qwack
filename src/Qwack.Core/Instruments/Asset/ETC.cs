using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Asset
{
    public class ETC : IAssetInstrument
    {
        public ETC() { }
        public ETC(double notional, string assetId, Currency ccy, double scalingFactor, Frequency settleLag, Calendar settleCalendar) : base()
        {
            Notional = notional;
            AssetId = assetId;
            Currency = ccy;
            ScalingFactor = scalingFactor;
            SettleLag = settleLag;
            SettleCalendar = settleCalendar;
        }

        public string[] AssetIds => new[] { AssetId };

        public Currency Currency { get; }

        public Currency PaymentCurrency => Currency;

        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }

        public DateTime LastSensitivityDate => DateTime.Today.AddPeriod(RollType.F, SettleCalendar, SettleLag);

        public double Notional { get; }
        public string AssetId { get; }
        public double ScalingFactor { get; }
        public Frequency SettleLag { get; }
        public Calendar SettleCalendar { get; }

        public IAssetInstrument Clone() => new ETC(Notional, AssetId, Currency, ScalingFactor, SettleLag, SettleCalendar)
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName
        };

        private bool IsFx => AssetId.Length == 7 && AssetId[3] == '/';

        public string FxPair(IAssetFxModel model)
        {
            if (IsFx)
                return AssetId;
            else if (Currency != model.GetPriceCurve(AssetId).Currency)
                return $"{model.GetPriceCurve(AssetId).Currency}/{PaymentCurrency}";
            else
                return string.Empty;
        }

        public FxConversionType FxType(IAssetFxModel model) => FxConversionType.None;

        public string DiscountCurve { get; set; }

        public string[] IrCurves(IAssetFxModel model)
        {
            if (IsFx)
            {
                var ccys = AssetId.Split('/');
                var c1 = model.FundingModel.FxMatrix.GetDiscountCurve(ccys[0]);
                var c2 = model.FundingModel.FxMatrix.GetDiscountCurve(ccys[1]);
                return (new[] { DiscountCurve, c1, c2 }).Distinct().Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();
            }
            else
            {
                if (string.IsNullOrEmpty(FxPair(model)))
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

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => new Dictionary<string, List<DateTime>>();
        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();

        public double PV(IAssetFxModel model)
        {
            var date = model.BuildDate.AddPeriod(RollType.F, SettleCalendar, SettleLag);
            var curve = model.GetPriceCurve(AssetId, PaymentCurrency);
            var fwd = curve.GetPriceForDate(date);
            var fx = curve.Currency == PaymentCurrency ? 1.0 : model.FundingModel.GetFxRate(LastSensitivityDate, curve.Currency, PaymentCurrency);
            return fwd * Notional * ScalingFactor * fx;
        }

        public override bool Equals(object obj) => obj is ETC eTC &&
                   EqualityComparer<Currency>.Default.Equals(Currency, eTC.Currency) &&
                   TradeId == eTC.TradeId &&
                   Counterparty == eTC.Counterparty &&
                   PortfolioName == eTC.PortfolioName &&
                   Notional == eTC.Notional &&
                   AssetId == eTC.AssetId &&
                   ScalingFactor == eTC.ScalingFactor &&
                   EqualityComparer<Frequency>.Default.Equals(SettleLag, eTC.SettleLag) &&
                   EqualityComparer<Calendar>.Default.Equals(SettleCalendar, eTC.SettleCalendar) &&
                   DiscountCurve == eTC.DiscountCurve;

        public override int GetHashCode() => Currency.GetHashCode() ^ TradeId.GetHashCode() ^ Counterparty.GetHashCode()
            ^ PortfolioName.GetHashCode() ^ Notional.GetHashCode() ^ AssetId.GetHashCode() ^ ScalingFactor.GetHashCode()
            ^ SettleLag.GetHashCode() ^ SettleCalendar.GetHashCode() ^ DiscountCurve.GetHashCode();
    }
}
