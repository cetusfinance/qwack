using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class FxForward : IFundingInstrument, IAssetInstrument
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

        public List<string> Dependencies(IFxMatrix matrix)
        {
            var curves = new[] { ForeignDiscountCurve, matrix.DiscountCurveMap[DomesticCCY], matrix.DiscountCurveMap[ForeignCCY] };
            return curves.Distinct().Where(x => x != SolveCurve).ToList();
        }

        public double Pv(IFundingModel Model, bool updateState)
        {
            if (Model.BuildDate > DeliveryDate)
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

        public CashFlowSchedule ExpectedCashFlows(IFundingModel model) => throw new NotImplementedException();

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
    }
}
