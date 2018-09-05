using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class OISFuture : IFundingInstrument
    {
        public double Price { get; set; } = 100.0;
        public double ContractSize { get; set; } = 1.0;
        public double Position { get; set; } = 1.0;
        public FloatRateIndex Index { get; set; }
        public double DCF { get; set; } = 1.0/12.0;
        public Currency CCY { get; set; }
        public DateTime AverageStartDate { get; set; }
        public DateTime AverageEndDate { get; set; }

        public string ForecastCurve { get; set; }
        public string SolveCurve { get; set; }
        public DateTime PillarDate { get; set; }
        public string TradeId { get; set; }
        public double Pv(IFundingModel Model, bool updateState)
        {
            var forecastCurve = Model.Curves[ForecastCurve];
            var fwdRate = forecastCurve.GetForwardRate(AverageStartDate, AverageEndDate, RateType.Linear, Index.DayCountBasis);

            var fairPrice = 100.0 - fwdRate * 100.0;
            var PV = (Price - fairPrice) * Position * ContractSize * DCF;

            return PV;
        }

        public CashFlowSchedule ExpectedCashFlows(IFundingModel model) => throw new NotImplementedException();

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            var forecastDict =  new Dictionary<DateTime, double>();
            var forecastCurve = model.Curves[ForecastCurve];

            var ts = forecastCurve.Basis.CalculateYearFraction(forecastCurve.BuildDate, AverageStartDate);
            var te = forecastCurve.Basis.CalculateYearFraction(forecastCurve.BuildDate, AverageEndDate);
            var fwdRate = forecastCurve.GetForwardRate(AverageStartDate, AverageEndDate, RateType.Linear, Index.DayCountBasis);
            var dPVdR = -100.0;
            var dPVdS = dPVdR * (-ts * (fwdRate + 1.0 / DCF));
            var dPVdE = dPVdR * (te * (fwdRate + 1.0 / DCF));

            forecastDict.Add(AverageStartDate, dPVdS);
            forecastDict.Add(AverageEndDate, dPVdE);

            return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {ForecastCurve, forecastDict },
            };
        }
    }
}
