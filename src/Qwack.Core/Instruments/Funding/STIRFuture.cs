using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class STIRFuture : IFundingInstrument, IAssetInstrument
    {
        public double Price { get; set; } = 100.0;
        public double ContractSize { get; set; } = 1.0;
        public double Position { get; set; } = 1.0;
        public FloatRateIndex Index { get; set; }
        public double DCF { get; set; } = 0.25;
        public Currency Currency { get; set; }
        public DateTime Expiry { get; set; }
        public string PortfolioName { get; set; }
        public double ConvexityAdjustment { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string ForecastCurve { get; set; }
        public string SolveCurve { get; set; }
        public DateTime PillarDate { get; set; }

        public DateTime LastSensitivityDate => Expiry;

        public double UnitPV01 => ContractSize * DCF * 0.0001;

        public string[] AssetIds => Array.Empty<string>();

        public Currency PaymentCurrency => Currency;

        public double Pv(IFundingModel Model, bool updateState)
        {
            var fairPrice = CalculateParRate(Model);
            var PV = (fairPrice - Price) / 100.0 * Position * ContractSize * DCF;
            return PV;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
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

        public List<string> Dependencies(IFxMatrix matrix) => new List<string>();

        public double CalculateParRate(IFundingModel Model)
        {
            var rateStart = Expiry.AddPeriod(RollType.F, Index.HolidayCalendars, Index.FixingOffset);
            var rateEnd = rateStart.AddPeriod(Index.RollConvention, Index.HolidayCalendars, Index.ResetTenor);
            var forecastCurve = Model.Curves[ForecastCurve];
            var fwdRate = forecastCurve.GetForwardRate(rateStart, rateEnd, RateType.Linear, Index.DayCountBasis);

            var fairPrice = 100.0 - (fwdRate + ConvexityAdjustment) * 100.0;
            return fairPrice;
        }

        public IFundingInstrument Clone() => new STIRFuture
        {
            ConvexityAdjustment = ConvexityAdjustment,
            Expiry = Expiry,
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
            TradeId = TradeId,
            PortfolioName = PortfolioName
        };

        public IFundingInstrument SetParRate(double parRate)
        {
            var newIns = (STIRFuture)Clone();
            newIns.Price = parRate;
            return newIns;
        }

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => new List<CashFlow>();

        public string[] IrCurves(IAssetFxModel model) => new[] { ForecastCurve };
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => new Dictionary<string, List<DateTime>>();
        public FxConversionType FxType(IAssetFxModel model) => FxConversionType.None;
        public string FxPair(IAssetFxModel model) => null;
        IAssetInstrument IAssetInstrument.Clone() => (STIRFuture)Clone();

        public IAssetInstrument SetStrike(double strike)
        {
            throw new NotImplementedException();
        }

        public double SuggestPillarValue(IFundingModel model) => (100.0 - Price) / 100.0;
    }
}
