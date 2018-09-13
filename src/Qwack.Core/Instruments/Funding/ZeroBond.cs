using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class ZeroBond : IFundingInstrument
    {
        public ZeroBond(double price, DateTime maturityDate, string discountCurve)
        {
            Price = price;
            MaturityDate = maturityDate;
            DiscountCurve = discountCurve;

            SolveCurve = DiscountCurve;
            PillarDate = MaturityDate;
        }

        public double Notional { get; set; } = 1.0;
        public double Price { get; set; }
        public DateTime MaturityDate { get; set; }
        public Currency Ccy { get; set; }
        public string DiscountCurve { get; set; }
        public string SolveCurve { get; set; }
        public string TradeId { get; set; }

        public DateTime PillarDate { get; set; }

        public double Pv(IFundingModel model, bool updateState)
        {
            return Notional * (model.Curves[DiscountCurve].GetDf(model.BuildDate, MaturityDate) - Price);
        }

        public CashFlowSchedule ExpectedCashFlows(IFundingModel model) => throw new NotImplementedException();

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            //discounting only
            var discountDict = new Dictionary<DateTime, double>();
            var discountCurve = model.Curves[DiscountCurve];
            var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, MaturityDate);
            var p = model.Curves[DiscountCurve].GetDf(model.BuildDate, MaturityDate);
            discountDict.Add(MaturityDate, -t * Notional * p);

            return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurve,discountDict },
            };

        }
    }
}
