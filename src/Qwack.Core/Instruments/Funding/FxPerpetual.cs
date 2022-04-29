using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Funding;
using static System.Math;

namespace Qwack.Core.Instruments.Funding
{
    public class FxPerpetual : IFundingInstrument, IAssetInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public double Strike { get; set; }
        public double DomesticQuantity { get; set; }
        public string PortfolioName { get; set; }
        public Currency DomesticCCY { get; set; }
        public Currency ForeignCCY { get; set; }
        public Currency Currency => ForeignCCY;

        public string ForeignDiscountCurve { get; set; }

        public string SolveCurve { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public DateTime PillarDate { get; set; } = DateTime.Today;
        public double FundingRateHourly { get; set; }

        public string Pair => $"{DomesticCCY.Ccy}/{ForeignCCY.Ccy}";

        public DateTime LastSensitivityDate => DateTime.Today;

        public string[] AssetIds => new[] { Pair };

        public Currency PaymentCurrency => DomesticCCY;

        public string HedgingSet { get; set; }

        public double ForeignNotional => DomesticQuantity * Strike;

        public FxPerpetual() { }

        public List<string> Dependencies(IFxMatrix matrix)
        {
            var curves = new[] { ForeignDiscountCurve, matrix.DiscountCurveMap[DomesticCCY], matrix.DiscountCurveMap[ForeignCCY] };
            return curves.Distinct().Where(x => x != SolveCurve).ToList();
        }

        public double Pv(IFundingModel Model, bool updateState) => Pv(Model, updateState, false);
        public double Pv(IFundingModel Model, bool updateState, bool ignoreTodayFlows)
        {
            var fwdRate = Model.GetFxRate(Model.BuildDate, DomesticCCY, ForeignCCY);
            var FV = (fwdRate - Strike) * DomesticQuantity;
            return FV;
        }

        public double CalculateParRate(IFundingModel Model) => Model.GetFxRate(Model.BuildDate, DomesticCCY, ForeignCCY);

        public double FlowsT0(IFundingModel Model)
        {
            var fwdRate = Model.GetFxRate(Model.BuildDate, DomesticCCY, ForeignCCY);
            var FV = (fwdRate - Strike) * DomesticQuantity;
            return FV;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model) => new();

        public virtual IAssetInstrument Clone() => (IAssetInstrument)(FxForward)((IFundingInstrument)this).Clone();

        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();

        public override bool Equals(object obj) => obj is FxForward forward &&
                   Strike == forward.Strike &&
                   DomesticQuantity == forward.DomesticQuantity &&
                   EqualityComparer<Currency>.Default.Equals(DomesticCCY, forward.DomesticCCY) &&
                   EqualityComparer<Currency>.Default.Equals(ForeignCCY, forward.ForeignCCY) &&
                   ForeignDiscountCurve == forward.ForeignDiscountCurve &&
                   TradeId == forward.TradeId;

        public override int GetHashCode() => Strike.GetHashCode() ^ DomesticQuantity.GetHashCode() ^ DomesticCCY.GetHashCode()
            ^ ForeignCCY.GetHashCode() ^ ForeignDiscountCurve.GetHashCode() ^ TradeId.GetHashCode();

        IFundingInstrument IFundingInstrument.Clone() => new FxForward
        {
            Counterparty = Counterparty,
            DomesticCCY = DomesticCCY,
            DomesticQuantity = DomesticQuantity,
            ForeignCCY = ForeignCCY,
            ForeignDiscountCurve = ForeignDiscountCurve,
            PillarDate = PillarDate,
            SolveCurve = SolveCurve,
            Strike = Strike,
            TradeId = TradeId,
            PortfolioName = PortfolioName
        };

        public IFundingInstrument SetParRate(double parRate)
        {
            var newIns = (FxForward)((IFundingInstrument)this).Clone();
            newIns.Strike = parRate;
            return newIns;
        }

        public string[] IrCurves(IAssetFxModel model) =>
            new[] {
                ForeignDiscountCurve,
                model.FundingModel.FxMatrix.DiscountCurveMap[ForeignCCY],
                model.FundingModel.FxMatrix.DiscountCurveMap[DomesticCCY] }
            .Distinct()
            .ToArray();

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => new();

        public FxConversionType FxType(IAssetFxModel model) => FxConversionType.None;

        public string FxPair(IAssetFxModel model) => Pair;

        public virtual double EffectiveNotional(IAssetFxModel model, double? MPOR = null) => SupervisoryDelta(model) * AdjustedNotional(model) * MaturityFactor(model.BuildDate, MPOR);
        public double AdjustedNotional(IAssetFxModel model) => DomesticQuantity * model.FundingModel.GetFxRate(model.BuildDate, DomesticCCY, model.FundingModel.FxMatrix.BaseCurrency);
        public virtual double SupervisoryDelta(IAssetFxModel model) => 1.0;
        public double MaturityFactor(DateTime today, double? MPOR = null) => MPOR.HasValue ? SaCcrUtils.MfMargined(MPOR.Value) : SaCcrUtils.MfUnmargined(T(today));
        private double T(DateTime today) => Max(0, today.CalculateYearFraction(LastSensitivityDate, DayCountBasis.Act365F));

        public virtual List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => new()
        {
            new CashFlow()
            {
                Currency = ForeignCCY,
                SettleDate = model.BuildDate,
                Notional = FlowsT0(model.FundingModel),
                Fv = DomesticQuantity
            },
        };

        public double SuggestPillarValue(IFundingModel model)
        {
            var rate = 0.05;
            return rate;
        }

        public virtual TO_Instrument ToTransportObject() =>
         new()
         {
             FundingInstrumentType = FundingInstrumentType.FxPerpetual,
             FxPerpetual = new TO_FxPerpetual
             {
                 TradeId = TradeId,
                 DomesticQuantity = DomesticQuantity,
                 DomesticCCY = DomesticCCY,
                 ForeignCCY = ForeignCCY,
                 ForeignDiscountCurve = ForeignDiscountCurve,
                 FundingRateHourly = FundingRateHourly,
                 PillarDate = PillarDate,
                 SolveCurve = SolveCurve,
                 Strike = Strike,
                 Counterparty = Counterparty,
                 HedgingSet = HedgingSet,
                 PortfolioName = PortfolioName,
                 MetaData = new(MetaData)
             }
         };
    }
}
