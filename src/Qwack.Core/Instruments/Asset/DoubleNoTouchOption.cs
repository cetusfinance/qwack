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
    public class DoubleNoTouchOption : IAssetInstrument
    {
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public Frequency PaymentLag { get; set; }
        public DateTime PaymentDate { get; set; }
        public string AssetId { get; set; }
        public Currency PaymentCurrency { get; set; }
        public string FxFixingId { get; set; }
        public string DiscountCurve { get; set; }
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;

        public Currency Currency => PaymentCurrency;

        private bool IsFx => AssetId.Length == 7 && AssetId[3] == '/';

        public double BarrierUp { get; set; }
        public double BarrierDown { get; set; }
        public BarrierType BarrierType { get; set; }
        public BarrierObservationType BarrierObservationType { get; set; }

        public DateTime BarrierObservationStartDate { get; set; }
        public DateTime BarrierObservationEndDate { get; set; }

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

        

        public IAssetInstrument Clone() => new DoubleNoTouchOption
        {
            TradeId = TradeId,
            Notional = Notional,
            Direction = Direction,
            FixingCalendar = FixingCalendar,
            PaymentCalendar = PaymentCalendar,
            SpotLag = SpotLag,
            PaymentLag = PaymentLag,
            AssetId = AssetId,
            PaymentCurrency = PaymentCurrency,
            FxFixingId = FxFixingId,
            DiscountCurve = DiscountCurve,
            PaymentDate = PaymentDate,
            Counterparty = Counterparty,
            FxConversionType = FxConversionType,
            PortfolioName = PortfolioName,
            BarrierUp = BarrierUp,
            BarrierDown = BarrierDown,
            BarrierObservationStartDate = BarrierObservationStartDate,
            BarrierObservationEndDate = BarrierObservationEndDate,
            BarrierObservationType = BarrierObservationType,
            BarrierType = BarrierType
        };

        public IAssetInstrument SetStrike(double strike) => this;

        public string[] AssetIds => new[] { AssetId };

        public DateTime LastSensitivityDate => PaymentDate;

        public FxConversionType FxType(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? FxConversionType.None : FxConversionType;
        public string FxPair(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{PaymentCurrency}";

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => valDate <= BarrierObservationStartDate ?
            new Dictionary<string, List<DateTime>>() :
            new Dictionary<string, List<DateTime>> { { AssetId, new List<DateTime> { BarrierObservationStartDate } } };

    }
}
