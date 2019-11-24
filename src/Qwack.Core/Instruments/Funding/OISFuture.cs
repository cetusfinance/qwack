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
        public Currency Currency { get; set; }
        public DateTime AverageStartDate { get; set; }
        public DateTime AverageEndDate { get; set; }
        public string PortfolioName { get; set; }
        public string ForecastCurve { get; set; }
        public string SolveCurve { get; set; }
        public DateTime PillarDate { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }

        public DateTime LastSensitivityDate => AverageEndDate;

        public List<string> Dependencies(IFxMatrix matrix) => new List<string>();

        public double Pv(IFundingModel Model, bool updateState)
        {
            var fairPrice = CalculateParRate(Model);
            var PV = (Price - fairPrice) * Position * ContractSize * DCF;
            return PV;
        }

        public double CalculateParRate(IFundingModel Model)
        {
            var forecastCurve = Model.Curves[ForecastCurve];
            var fwdRate = forecastCurve.GetForwardRate(AverageStartDate, AverageEndDate, RateType.Linear, Index.DayCountBasis);

            var fairPrice = 100.0 - fwdRate * 100.0;
            return fairPrice;
        }

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

        public IFundingInstrument Clone() => new OISFuture
        {
            AverageEndDate = AverageEndDate,
            AverageStartDate = AverageStartDate,
            Currency = Currency,
            ContractSize = ContractSize,
            Counterparty = Counterparty,
            DCF = DCF,
            ForecastCurve = ForecastCurve,
            Index = Index,
            PillarDate = PillarDate,
            Position = Position,
            Price = Price,
            SolveCurve = SolveCurve,
            TradeId = TradeId
        };

        public IFundingInstrument SetParRate(double parRate)
        {
            var newIns = (OISFuture)Clone();
            newIns.Price = parRate;
            return newIns;
        }

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => new List<CashFlow>();
    }
}
