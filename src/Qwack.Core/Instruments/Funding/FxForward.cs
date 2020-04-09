using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using static System.Math;

namespace Qwack.Core.Instruments.Funding
{
    public class FxForward : IFundingInstrument, IAssetInstrument, ISaCcrEnabled
    {
        public double Strike { get; set; }
        public double DomesticQuantity { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string PortfolioName { get; set; }
        public Currency DomesticCCY { get; set; }
        public Currency ForeignCCY { get; set; }
        public Currency Currency => DomesticCCY;

        public string ForeignDiscountCurve { get; set; }

        public string SolveCurve { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public DateTime PillarDate { get; set; }

        public string Pair => $"{DomesticCCY.Ccy}/{ForeignCCY.Ccy}";

        public DateTime LastSensitivityDate => DeliveryDate;

        public string[] AssetIds => new[] { Pair };

        public Currency PaymentCurrency => DomesticCCY;

        public string HedgingSet { get; set; }

        public List<string> Dependencies(IFxMatrix matrix)
        {
            var curves = new[] { ForeignDiscountCurve, matrix.DiscountCurveMap[DomesticCCY], matrix.DiscountCurveMap[ForeignCCY] };
            return curves.Distinct().Where(x => x != SolveCurve).ToList();
        }

        public double Pv(IFundingModel Model, bool updateState) => Pv(Model, updateState, false);
        public double Pv(IFundingModel Model, bool updateState, bool ignoreTodayFlows)
        {
            if (Model.BuildDate > DeliveryDate || (ignoreTodayFlows && Model.BuildDate == DeliveryDate))
                return 0.0;

            var discountCurve = Model.Curves[ForeignDiscountCurve];
            var fwdRate = Model.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY);
            var FV = (fwdRate - Strike) * DomesticQuantity;
            var PV = discountCurve.Pv(FV, DeliveryDate);

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
            var df = discountCurve.Pv(1.0, DeliveryDate);
            var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, DeliveryDate);
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
                var foreignDiscDict = new Dictionary<DateTime, double>() { { DeliveryDate, (fwdRate-Strike) * DomesticQuantity * df * -t } };

                return new Dictionary<string, Dictionary<DateTime, double>>()
                {
                    {foreignCurve, foreignDict },
                    {domesticCurve, domesticDict },
                    {ForeignDiscountCurve, foreignDiscDict },
                };
            }
        }

        public IAssetInstrument Clone() => (IAssetInstrument)(FxForward)((IFundingInstrument)this).Clone();

        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();

        public override bool Equals(object obj) => obj is FxForward forward &&
                   Strike == forward.Strike &&
                   DomesticQuantity == forward.DomesticQuantity &&
                   DeliveryDate == forward.DeliveryDate &&
                   EqualityComparer<Currency>.Default.Equals(DomesticCCY, forward.DomesticCCY) &&
                   EqualityComparer<Currency>.Default.Equals(ForeignCCY, forward.ForeignCCY) &&
                   ForeignDiscountCurve == forward.ForeignDiscountCurve &&
                   TradeId == forward.TradeId;

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

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => new Dictionary<string, List<DateTime>>();

        public FxConversionType FxType(IAssetFxModel model) => FxConversionType.None;

        public string FxPair(IAssetFxModel model) => Pair;

        public double EffectiveNotional(IAssetFxModel model) => SupervisoryDelta(model) * AdjustedNotional(model) * MaturityFactor(model.BuildDate);
        public double AdjustedNotional(IAssetFxModel model) => DomesticQuantity * model.FundingModel.GetFxRate(model.BuildDate, DomesticCCY, model.FundingModel.FxMatrix.BaseCurrency);
        public double SupervisoryDelta(IAssetFxModel model) => 1.0;
        public double MaturityFactor(DateTime today) => Sqrt(Min(M(today), 1.0));
        private double M(DateTime today) => Max(0, today.CalculateYearFraction(LastSensitivityDate, DayCountBasis.Act365F));


        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => new List<CashFlow>
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
            var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, PillarDate);
            var rate = -Log(df2) / t;
            return rate;
        }
    }
}
