using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Funding
{
    public class ContangoSwap : IFundingInstrument
    {
        public double ContangoRate { get; set; }
        public double MetalQuantity { get; set; }
        public string PortfolioName { get; set; }
        public DateTime SpotDate { get; set; }
        public DateTime DeliveryDate { get; set; }

        public Currency MetalCCY { get; set; }
        public Currency CashCCY { get; set; }
        public Currency Currency => CashCCY;

        public string CashDiscountCurve { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string SolveCurve { get; set; }

        public DateTime PillarDate { get; set; }

        public DayCountBasis Basis { get; set; } = DayCountBasis.ACT360;

        public DateTime LastSensitivityDate => DeliveryDate;

        public List<string> Dependencies(IFxMatrix matrix)
        {
            var curves = new[] { CashDiscountCurve, matrix.DiscountCurveMap[MetalCCY], matrix.DiscountCurveMap[CashCCY] };
            return curves.Distinct().Where(x => x != SolveCurve).ToList();
        }

        public double Pv(IFundingModel model, bool updateState)
        {
            var discountCurve = model.Curves[CashDiscountCurve];
            var SpotRate = model.GetFxRate(SpotDate, MetalCCY, CashCCY);
            var t = SpotDate.CalculateYearFraction(DeliveryDate, Basis);
            var strike = SpotRate * (1.0 + ContangoRate * t);
            var fwd = model.GetFxRate(DeliveryDate, MetalCCY, CashCCY);
            var FV = (fwd - strike) * MetalQuantity;
            var PV = discountCurve.Pv(FV, DeliveryDate);

            return PV;
        }

        public double CalculateParRate(IFundingModel model)
        {
            var discountCurve = model.Curves[CashDiscountCurve];
            var SpotRate = model.GetFxRate(SpotDate, MetalCCY, CashCCY);
            var t = SpotDate.CalculateYearFraction(DeliveryDate, Basis);
            var fwd = model.GetFxRate(DeliveryDate, MetalCCY, CashCCY);
            var ctgo = (fwd / SpotRate - 1.0) / t;
            return ctgo;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            var foreignCurve = model.FxMatrix.DiscountCurveMap[CashCCY];
            var domesticCurve = model.FxMatrix.DiscountCurveMap[MetalCCY];
            var discountCurve = model.Curves[CashDiscountCurve];
            var df = discountCurve.Pv(1.0, DeliveryDate);
            var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, DeliveryDate);
            var spotRate = model.GetFxRate(SpotDate, MetalCCY, CashCCY);
            var strike = spotRate * (1.0 + ContangoRate * t);
            var fwdRate = model.GetFxRate(DeliveryDate, MetalCCY, CashCCY);

            var domesticDict = new Dictionary<DateTime, double>() { { DeliveryDate, fwdRate * MetalQuantity * df * t } };

            Dictionary<DateTime, double> foreignDict;

            if (foreignCurve == CashDiscountCurve)
            {
                foreignDict = new Dictionary<DateTime, double>() { { DeliveryDate, MetalQuantity * df * (fwdRate * -2 * t + strike * t) } };

                return new Dictionary<string, Dictionary<DateTime, double>>()
                {
                    {foreignCurve, foreignDict },
                    {domesticCurve, domesticDict },
                };
            }
            else
            {
                foreignDict = new Dictionary<DateTime, double>() { { DeliveryDate, fwdRate * MetalQuantity * df * -t } };
                var foreignDiscDict = new Dictionary<DateTime, double>() { { DeliveryDate, (fwdRate - strike) * MetalQuantity * df * -t } };

                return new Dictionary<string, Dictionary<DateTime, double>>()
                {
                    {foreignCurve, foreignDict },
                    {domesticCurve, domesticDict },
                    {CashDiscountCurve, foreignDiscDict },
                };
            }
        }

        public IFundingInstrument Clone() => new ContangoSwap
        {
            Basis = Basis,
            CashCCY = CashCCY,
            CashDiscountCurve = CashDiscountCurve,
            ContangoRate = ContangoRate,
            Counterparty = Counterparty,
            DeliveryDate = DeliveryDate,
            MetalCCY = MetalCCY,
            MetalQuantity = MetalQuantity,
            PillarDate = PillarDate,
            SolveCurve = SolveCurve,
            SpotDate = SpotDate,
            TradeId = TradeId
        };


        public IFundingInstrument SetParRate(double parRate)
        {
            var newIns = (ContangoSwap)Clone();
            newIns.ContangoRate = parRate;
            return newIns;
        }

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model)
        {
            var SpotRate = model.FundingModel.GetFxRate(SpotDate, MetalCCY, CashCCY);
            var t = Basis.CalculateYearFraction(model.BuildDate, DeliveryDate);
            var strike = SpotRate * (1.0 + ContangoRate * t);
            return new List<CashFlow>
            { new CashFlow()
                {
                    Currency = MetalCCY,
                    SettleDate = SpotDate,
                    Notional = MetalQuantity,
                    Fv = MetalQuantity
                },
            new CashFlow()
            {
                    Currency = MetalCCY,
                    SettleDate = DeliveryDate,
                    Notional = -MetalQuantity,
                    Fv = -MetalQuantity
                },
             new CashFlow()
                {
                    Currency = CashCCY,
                    SettleDate = SpotDate,
                    Notional = -MetalQuantity * strike,
                    Fv = -MetalQuantity *strike
                },
            new CashFlow()
            {
                    Currency = MetalCCY,
                    SettleDate = DeliveryDate,
                    Notional = MetalQuantity * strike,
                    Fv = MetalQuantity * strike
                }
            };
        }

        public double SuggestPillarValue(IFundingModel model)
        {
            var discountCurve = model.Curves[CashDiscountCurve];
            var SpotRate = model.GetFxRate(SpotDate, MetalCCY, CashCCY);
            var t = SpotDate.CalculateYearFraction(DeliveryDate, Basis);
            var fwd = SpotRate * (1.0 + ContangoRate * t);
            var fxr = fwd / SpotRate;
            var df1 = discountCurve.GetDf(SpotDate, PillarDate);
            var df2 = df1 / fxr;
            var rate = -System.Math.Log(df2) / t;
            return rate;
        }
    }
}
