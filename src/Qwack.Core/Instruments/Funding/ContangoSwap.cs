using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class ContangoSwap : IFundingInstrument
    {
        public double ContangoRate { get; set; }
        public double MetalQuantity { get; set; }

        public DateTime SpotDate { get; set; }
        public DateTime DeliveryDate { get; set; }

        public Currency MetalCCY { get; set; }
        public Currency CashCCY { get; set; }

        public string CashDiscountCurve { get; set; }

        public string SolveCurve { get; set; }

        public DateTime PillarDate { get; set; }

        public DayCountBasis Basis { get; set; } = DayCountBasis.ACT360;

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

        public CashFlowSchedule ExpectedCashFlows(IFundingModel model) => throw new NotImplementedException();

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
                var foreignDiscDict = new Dictionary<DateTime, double>() { { DeliveryDate, (fwdRate-strike) * MetalQuantity * df * -t } };

                return new Dictionary<string, Dictionary<DateTime, double>>()
                {
                    {foreignCurve, foreignDict },
                    {domesticCurve, domesticDict },
                    {CashDiscountCurve, foreignDiscDict },
                };
            }
        }
        
    }
}
