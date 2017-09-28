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
            var rateStart = Expiry.AddPeriod(RollType.F, Index.HolidayCalendars, Index.FixingOffset);
            var rateEnd = rateStart.AddPeriod(Index.RollConvention, Index.HolidayCalendars, Index.ResetTenor);
            var forecastCurve = Model.Curves[ForecastCurve];
            var fwdRate = forecastCurve.GetForwardRate(rateStart, rateEnd, RateType.Linear, Index.DayCountBasis);

            var fairPrice = 100.0 - (fwdRate + ConvexityAdjustment) * 100.0;
            var PV = (Price - fairPrice) * Position * ContractSize * DCF;

            return PV;
        }

        public CashFlowSchedule ExpectedCashFlows(FundingModel model) => throw new NotImplementedException();

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(FundingModel model)
        {
            //only forecast for STIR future
            var forecastDict =  new Dictionary<DateTime, double>();
            var forecastCurve = model.Curves[ForecastCurve];

            var rateStart = Expiry.AddPeriod(RollType.F, Index.HolidayCalendars, Index.FixingOffset);
            var rateEnd = rateStart.AddPeriod(Index.RollConvention, Index.HolidayCalendars, Index.ResetTenor);

            var ts = forecastCurve.Basis.CalculateYearFraction(forecastCurve.BuildDate, rateStart);
            var te = forecastCurve.Basis.CalculateYearFraction(forecastCurve.BuildDate, rateEnd);
            var fwdRate = forecastCurve.GetForwardRate(rateStart, rateEnd, RateType.Linear, Index.DayCountBasis);
            var dPVdR = -100.0;
            var dPVdS = dPVdR * (-ts * (fwdRate + 1.0 / DCF));
            var dPVdE = dPVdR * (te * (fwdRate + 1.0 / DCF));

            forecastDict.Add(rateStart, dPVdS);
            forecastDict.Add(rateEnd, dPVdE);

            return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {ForecastCurve, forecastDict },
            };
        }
    }
}
