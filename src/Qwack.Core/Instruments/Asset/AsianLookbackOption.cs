using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianLookbackOption : IHasVega, IAssetInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public OptionType CallPut { get; set; }

        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }

        public DateTime ObsStartDate { get; set; }
        public DateTime ObsEndDate { get; set; }
        public DateTime[] FixingDates { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public RollType SpotLagRollType { get; set; } = RollType.F;
        public Frequency PaymentLag { get; set; }
        public RollType PaymentLagRollType { get; set; } = RollType.F;

        public DateTime SettlementDate { get; set; }
        public DateTime[] SettlementFixingDates { get; set; }
        public DateTime DecisionDate { get; set; }

        public string AssetId { get; set; }
        public string AssetFixingId { get; set; }
        public string FxFixingId { get; set; }
        public DateTime[] FxFixingDates { get; set; }
        public Currency PaymentCurrency { get; set; }
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;
        public string DiscountCurve { get; set; }
        public int WindowSize { get; set; } = 1;

        private bool IsFx => AssetId.Length == 7 && AssetId[3] == '/';

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

        public DateTime LastSensitivityDate => SettlementDate.Max(ObsEndDate.AddPeriod(SpotLagRollType, FixingCalendar, SpotLag));

        public string FxPair(IAssetFxModel model) => IsFx ? AssetId : model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{PaymentCurrency}";
        public FxConversionType FxType(IAssetFxModel model) => IsFx ? FxConversionType.None : (model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? FxConversionType.None : FxConversionType);
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => valDate <= FixingDates.First() ?
           new Dictionary<string, List<DateTime>>() :
           new Dictionary<string, List<DateTime>> { { AssetId, FixingDates.Where(d => d < valDate).ToList() } };


        public IAssetInstrument Clone() => new AsianLookbackOption
        {
            TradeId = TradeId,
            Notional = Notional,
            Direction = Direction,
            ObsStartDate = ObsStartDate,
            ObsEndDate = ObsStartDate,
            FixingDates = (DateTime[])FixingDates.Clone(),
            FixingCalendar = FixingCalendar,
            PaymentCalendar = PaymentCalendar,
            SpotLag = SpotLag,
            SpotLagRollType = SpotLagRollType,
            PaymentLag = PaymentLag,
            PaymentLagRollType = PaymentLagRollType,
            SettlementDate = SettlementDate,
            PaymentCurrency = PaymentCurrency,
            AssetFixingId = AssetFixingId,
            AssetId = AssetId,
            DiscountCurve = DiscountCurve,
            FxConversionType = FxConversionType,
            FxFixingDates = FxFixingDates == null ? null : (DateTime[])FxFixingDates.Clone(),
            FxFixingId = FxFixingId,
            WindowSize = WindowSize,
            PortfolioName = PortfolioName,
            CallPut = CallPut,
            DecisionDate = DecisionDate,
            SettlementFixingDates = SettlementFixingDates == null ? null : (DateTime[])SettlementFixingDates.Clone(),
            MetaData = new(MetaData),
        };

        public IAssetInstrument SetStrike(double strike) => throw new InvalidOperationException();

        public TO_Instrument ToTransportObject() =>
                  new(){
                      AsianLookbackOption = new()
                      {
                          TradeId = TradeId,
                          Notional = Notional,
                          Direction = Direction,
                          ObsStartDate = ObsStartDate,
                          ObsEndDate = ObsStartDate,
                          FixingDates = (DateTime[])FixingDates.Clone(),
                          FixingCalendar = FixingCalendar?.Name,
                          PaymentCalendar = PaymentCalendar?.Name,
                          SpotLag = SpotLag.ToString(),
                          SpotLagRollType = SpotLagRollType,
                          PaymentLag = PaymentLag.ToString(),
                          PaymentLagRollType = PaymentLagRollType,
                          SettlementDate = SettlementDate,
                          PaymentCurrency = PaymentCurrency,
                          AssetFixingId = AssetFixingId,
                          AssetId = AssetId,
                          DiscountCurve = DiscountCurve,
                          FxConversionType = FxConversionType,
                          FxFixingDates = FxFixingDates == null ? null : (DateTime[])FxFixingDates.Clone(),
                          FxFixingId = FxFixingId,
                          WindowSize = WindowSize,
                          PortfolioName = PortfolioName,
                          CallPut = CallPut,
                          DecisionDate = DecisionDate,
                          SettlementFixingDates = SettlementFixingDates == null ? null : (DateTime[])SettlementFixingDates.Clone(),
                      }
                  };
    }
}
