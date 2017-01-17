using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class STIRFuture : IFundingInstrument
    {
        public double Price { get; set; } = 100.0;
        public double ContractSize { get; set; } = 1.0;
        public double Position { get; set; } = 1.0;
        public FloatRateIndex Index { get; set; }
        public double DCF { get; set; } = 0.25;
        public Currency CCY { get; set; }
        public DateTime Expiry { get; set; }

        public double ConvexityAdjustment { get; set; }

        public string ForecastCurve { get; set; }
        public string SolveCurve { get; set; }

        public double Pv(FundingModel Model, bool updateState)
        {
            DateTime rateStart = Expiry.AddPeriod(RollType.F, Index.HolidayCalendars, Index.FixingOffset);
            DateTime rateEnd = rateStart.AddPeriod(Index.RollConvention, Index.HolidayCalendars, Index.ResetTenor);
            var forecastCurve = Model.Curves[ForecastCurve];
            double fwdRate = forecastCurve.GetForwardRate(rateStart, rateEnd, RateType.Linear, Index.DayCountBasis);

            double fairPrice = 100.0 - (fwdRate + ConvexityAdjustment) * 100.0;
            double PV = (Price - fairPrice) * Position * ContractSize * DCF;

            return PV;
        }

        public CashFlowSchedule ExpectedCashFlows(FundingModel model)
        {
            throw new NotImplementedException();
        }
    }
}
