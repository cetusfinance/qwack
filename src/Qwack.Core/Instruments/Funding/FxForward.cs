using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;

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
    }
}
