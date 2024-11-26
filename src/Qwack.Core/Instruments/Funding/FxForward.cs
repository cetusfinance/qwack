using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Funding;
using static System.Math;

namespace Qwack.Core.Instruments.Funding
{
    public class FxForward : IFundingInstrument, IAssetInstrument, ISaccrEnabledFx
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public double Strike { get; set; }
        public double DomesticQuantity { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string PortfolioName { get; set; }
        public Currency DomesticCCY { get; set; }
        public Currency ForeignCCY { get; set; }
        public Currency Currency => ForeignCCY;

        public string ForeignDiscountCurve { get; set; }

        public string SolveCurve { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public DateTime PillarDate { get; set; }

        public string Pair => $"{DomesticCCY.Ccy}/{ForeignCCY.Ccy}";

        public DateTime LastSensitivityDate => DeliveryDate;

        public string[] AssetIds => new[] { Pair };

        public Currency PaymentCurrency => ForeignCCY;

        public string HedgingSet { get; set; }

        public double ForeignNotional => DomesticQuantity * Strike;

        public List<string> Dependencies(IFxMatrix matrix)
        {
            var curves = new[] { ForeignDiscountCurve, matrix.DiscountCurveMap[DomesticCCY], matrix.DiscountCurveMap[ForeignCCY] };
            return curves.Distinct().Where(x => x != SolveCurve).ToList();
        }

        public double Pv(IFundingModel Model, bool updateState) => Pv(Model, updateState, false);
        public virtual double Pv(IFundingModel Model, bool updateState, bool ignoreTodayFlows)
        {
            if (Model.BuildDate > DeliveryDate || (ignoreTodayFlows && Model.BuildDate == DeliveryDate))
                return 0.0;

            var discountCurve = Model.Curves[ForeignDiscountCurve];
            var fwdRate = Model.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY);
            var FV = (fwdRate - Strike) * DomesticQuantity;
            var PV = FV * discountCurve.GetDf(Model.BuildDate, DeliveryDate);

            return PV;
        }

        public double CalculateParRate(IFundingModel Model) => Model.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY);

        public double FlowsT0(IFundingModel Model)
        {
            if (DeliveryDate != Model.BuildDate)
                return 0.0;

            var fwdRate = Model.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY);
            var FV = (fwdRate - Strike) * DomesticQuantity;
            return FV;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            var foreignCurve = model.FxMatrix.DiscountCurveMap[ForeignCCY];
            var domesticCurve = model.FxMatrix.DiscountCurveMap[DomesticCCY];
            var discountCurve = model.Curves[ForeignDiscountCurve];
            var df = discountCurve.GetDf(model.BuildDate, DeliveryDate);
            var t = ((discountCurve as IrCurve)?.Basis ?? DayCountBasis.Act_365F).CalculateYearFraction(discountCurve.BuildDate, DeliveryDate);
            var fwdRate = model.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY);

            var domesticDict = new Dictionary<DateTime, double>() { { DeliveryDate, fwdRate * DomesticQuantity * df * t } };

            Dictionary<DateTime, double> foreignDict;

            if (foreignCurve == ForeignDiscountCurve)
            {
                foreignDict = new Dictionary<DateTime, double>() { { DeliveryDate, DomesticQuantity * df * (fwdRate * -2 * t + Strike * t) } };

                return new Dictionary<string, Dictionary<DateTime, double>>()
                {
                    {foreignCurve, foreignDict },
                    {domesticCurve, domesticDict },
                };
            }
            else
            {
                foreignDict = new Dictionary<DateTime, double>() { { DeliveryDate, fwdRate * DomesticQuantity * df * -t } };
                var foreignDiscDict = new Dictionary<DateTime, double>() { { DeliveryDate, (fwdRate - Strike) * DomesticQuantity * df * -t } };

                return new Dictionary<string, Dictionary<DateTime, double>>()
                {
                    {foreignCurve, foreignDict },
                    {domesticCurve, domesticDict },
                    {ForeignDiscountCurve, foreignDiscDict },
                };
            }
        }

        public virtual IAssetInstrument Clone() => (IAssetInstrument)(FxForward)((IFundingInstrument)this).Clone();

        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();

        public override bool Equals(object obj) => obj is FxForward forward &&
                   Strike == forward.Strike &&
                   DomesticQuantity == forward.DomesticQuantity &&
                   DeliveryDate == forward.DeliveryDate &&
                   EqualityComparer<Currency>.Default.Equals(DomesticCCY, forward.DomesticCCY) &&
                   EqualityComparer<Currency>.Default.Equals(ForeignCCY, forward.ForeignCCY) &&
                   ForeignDiscountCurve == forward.ForeignDiscountCurve &&
                   TradeId == forward.TradeId;

        public override int GetHashCode() => Strike.GetHashCode() ^ DomesticQuantity.GetHashCode() ^ DeliveryDate.GetHashCode()
            ^ DomesticCCY.GetHashCode() ^ ForeignCCY.GetHashCode() ^ ForeignDiscountCurve.GetHashCode() ^ TradeId.GetHashCode();

        IFundingInstrument IFundingInstrument.Clone() => new FxForward
        {
            Counterparty = Counterparty,
            DeliveryDate = DeliveryDate,
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

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => [];
        public Dictionary<string, List<DateTime>> PastFixingDatesFx(IAssetFxModel model, DateTime valDate) => [];
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
                    Currency = DomesticCCY,
                    SettleDate = DeliveryDate,
                    Notional = DomesticQuantity,
                    Fv = DomesticQuantity
                },
                new CashFlow()
                {
                    Currency = ForeignCCY,
                    SettleDate = DeliveryDate,
                    Notional = DomesticQuantity * Strike,
                    Fv = DomesticQuantity * Strike
                }
        };

        public double SuggestPillarValue(IFundingModel model)
        {
            var pair = model.FxMatrix.GetFxPair(Pair);
            var spotDate = pair.SpotDate(model.BuildDate);
            var fxr = Strike / model.GetFxRate(spotDate, Pair);
            var df1 = model.GetCurve(model.FxMatrix.GetDiscountCurve(DomesticCCY.Ccy)).GetDf(spotDate, PillarDate);
            var df2 = df1 / fxr;
            var discountCurve = model.Curves[SolveCurve];
            var t = ((discountCurve as IrCurve)?.Basis ?? DayCountBasis.ACT365F).CalculateYearFraction(spotDate, PillarDate);
            var rate = -Log(df2) / t;
            if (double.IsNaN(rate) || double.IsInfinity(rate))
                rate = 0.05;
            return rate;
        }

        public virtual TO_Instrument ToTransportObject() =>
         new()
         {
             FundingInstrumentType = FundingInstrumentType.FxForward,
             FxForward = new TO_FxForward
             {
                 TradeId = TradeId,
                 DomesticQuantity = DomesticQuantity,
                 DomesticCCY = DomesticCCY,
                 ForeignCCY = ForeignCCY,
                 ForeignDiscountCurve = ForeignDiscountCurve,
                 DeliveryDate = DeliveryDate,
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
