using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class FxForward : IFundingInstrument
    {
        public double Strike { get; set; }
        public double DomesticQuantity { get; set; }
        public DateTime DeliveryDate { get; set; }

        public Currency DomesticCCY { get; set; }
        public Currency ForeignCCY { get; set; }

        public string ForeignDiscountCurve { get; set; }

        public string SolveCurve { get; set; }

        public double Pv(FundingModel Model, bool updateState)
        {
            var discountCurve = Model.Curves[ForeignDiscountCurve];
            double fwdRate = Model.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY);
            double FV = (fwdRate - Strike) * DomesticQuantity;
            double PV = discountCurve.Pv(FV, DeliveryDate);

            return PV;
        }

        public CashFlowSchedule ExpectedCashFlows(FundingModel model)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(FundingModel model)
        {
            var foreignCurve = model.FxMatrix.DiscountCurveMap[ForeignCCY];
            var domesticCurve = model.FxMatrix.DiscountCurveMap[DomesticCCY];
            var discountCurve = model.Curves[ForeignDiscountCurve];
            double df = discountCurve.Pv(1.0, DeliveryDate);
            double t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, DeliveryDate);
            double fwdRate = model.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY);

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
        
    }
}
