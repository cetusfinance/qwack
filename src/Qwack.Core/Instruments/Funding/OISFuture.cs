using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Funding
{
    public class OISFuture : IFundingInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public double Price { get; set; } = 100.0;
        public double ContractSize { get; set; } = 1.0;
        public double Position { get; set; } = 1.0;
        public FloatRateIndex Index { get; set; }
        public double DCF { get; set; } = 1.0 / 12.0;
        public Currency Currency { get; set; }
        public DateTime AverageStartDate { get; set; }
        public DateTime AverageEndDate { get; set; }
        public string PortfolioName { get; set; }
        public string ForecastCurve { get; set; }
        public string SolveCurve { get; set; }
        public DateTime PillarDate { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }

        public double UnitPV01 => ContractSize * DCF * 0.0001;

        public DateTime LastSensitivityDate => AverageEndDate;

        public List<string> Dependencies(IFxMatrix matrix) => new();

        public double Pv(IFundingModel Model, bool updateState)
        {
            var fairPrice = CalculateParRate(Model);
            var PV = (Price - fairPrice) * Position * ContractSize * DCF;
            return PV;
        }

        private List<DateTime> _avgDates;
        public double CalculateParRate(IFundingModel Model)
        {
            var forecastCurve = Model.Curves[ForecastCurve];
            var cal = Index?.HolidayCalendars ?? Currency.SettlementCalendar;
            double fwdRate = 0;
            if (Model?.BuildDate > AverageStartDate && forecastCurve is IrCurve fc &&  fc.Fixings!=null)
            {
                var avgDates = _avgDates ?? AverageStartDate.BusinessDaysInPeriod(AverageEndDate, cal);
                var rates = avgDates.Select(d => fc.Fixings.TryGetValue(d, out var x) ? x : 0).ToArray();
                var index = 1.0;
                for(var i=0;i< avgDates.Count-1; i++)
                {
                    if (rates[i] == 0 && avgDates[i]>= Model.BuildDate)
                    {
                        rates[i] = forecastCurve.GetForwardRate(avgDates[i], avgDates[i+1], RateType.Linear, Index.DayCountBasis);
                    }
                    else if (i>0 && rates[i] == 0 && rates[i-1]!=0)
                        rates[i] = rates[i-1];

                    var d = avgDates[i + 1].Subtract(avgDates[i]).TotalDays;
                    index *= 1 + rates[i] * d / 360.0;
                }
                var bigD = AverageEndDate.Subtract(AverageStartDate).TotalDays;
                fwdRate = (index - 1) * 360.0 / bigD;

                _avgDates = avgDates;
            }
            else
                fwdRate = forecastCurve.GetForwardRate(AverageStartDate, AverageEndDate, RateType.Linear, Index.DayCountBasis);

            var fairPrice = 100.0 - fwdRate * 100.0;
            return fairPrice;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            var forecastDict = new Dictionary<DateTime, double>();
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

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => new();

        public double SuggestPillarValue(IFundingModel model) => (100.0 - Price) / 100.0;
    }
}
