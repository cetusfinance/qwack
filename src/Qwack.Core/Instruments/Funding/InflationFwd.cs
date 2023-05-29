using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Funding
{
    public class InflationFwd : IFundingInstrument, ISaCcrEnabledIR, IIsInflationInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public InflationFwd() { }

        public InflationFwd(DateTime fixingDate, InflationIndex rateIndex, double strike, double notional, string forecastCurveCpi, string discountCurve)
        {
            FixingDate = fixingDate;
            Strike = strike;
            RateIndex = rateIndex;
            Currency = rateIndex.Currency;
            Notional = notional;
            ResetDates = new[] { FixingDate };

            ForecastCurveCpi = forecastCurveCpi;
            DiscountCurve = discountCurve;
        }


        public double Notional { get; set; }
        public double Strike { get; set; }
        public DateTime FixingDate { get; set; }
        public double InitialFixing { get; set; }
        public DateTime[] ResetDates { get; set; }
        public Currency Currency { get; set; }

        public string ForecastCurveCpi { get; set; }
        public string DiscountCurve { get; set; }
        public string SolveCurve { get; set; }

        private DateTime? _pillarDate;
        public DateTime PillarDate
        {
            get => _pillarDate ?? FixingDate;
            set
            {
                _pillarDate = value;
            }
        }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public InflationIndex RateIndex { get; set; }
        public string PortfolioName { get; set; }

        public DateTime LastSensitivityDate => FixingDate;

        public List<string> Dependencies(IFxMatrix matrix) => (new[] { DiscountCurve, ForecastCurveCpi }).Distinct().Where(x => x != SolveCurve).ToList();

        public double Pv(IFundingModel model, bool updateState)
        {
            var discountCurve = model.Curves[DiscountCurve];
            var forecastCurveCpi = model.Curves[ForecastCurveCpi];      

            var forecast = (forecastCurveCpi as CPICurve).GetForecast(FixingDate, RateIndex.FixingLag.PeriodCount);
            var fv = (forecast - Strike) * Notional;
            var df = discountCurve.GetDf(model.BuildDate, FixingDate);

            return fv * df;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            throw new NotImplementedException();
        }

        public double CalculateParRate(IFundingModel model)
        {
            var forecastCurveCpi = model.Curves[ForecastCurveCpi];
            var forecast = (forecastCurveCpi as CPICurve).GetForecast(FixingDate, RateIndex.FixingLag.PeriodCount);
            return forecast;
        }

        public IFundingInstrument Clone() => new InflationFwd
        {
            Currency = Currency,
            Counterparty = Counterparty,
            DiscountCurve = DiscountCurve,     
            ForecastCurveCpi = ForecastCurveCpi,
            InitialFixing = InitialFixing,
            Notional = Notional,
            Strike = Strike,
            PillarDate = PillarDate,
            ResetDates = ResetDates,
            SolveCurve = SolveCurve,
            TradeId = TradeId,
            RateIndex = RateIndex,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet,

        };

        public IFundingInstrument SetParRate(double parRate)
        {
            return new InflationFwd(FixingDate, RateIndex, Strike, Notional, ForecastCurveCpi, DiscountCurve)
            {
                TradeId = TradeId,
                SolveCurve = SolveCurve,
                PillarDate = PillarDate,
                RateIndex = RateIndex,
                PortfolioName = PortfolioName,
                HedgingSet = HedgingSet
            };
        }

        public double TradeNotional => System.Math.Abs(Notional);
        public virtual double EffectiveNotional(IAssetFxModel model, double? MPOR = null) => SupervisoryDelta(model) * AdjustedNotional(model) * MaturityFactor(model.BuildDate, MPOR);
        public double AdjustedNotional(IAssetFxModel model) => TradeNotional * SupervisoryDuration(model.BuildDate);
        private double tStart(DateTime today) => today.CalculateYearFraction(FixingDate, DayCountBasis.Act365F);
        private double tEnd(DateTime today) => today.CalculateYearFraction(FixingDate, DayCountBasis.Act365F);
        public double SupervisoryDuration(DateTime today) => SaCcrUtils.SupervisoryDuration(tStart(today), tEnd(today));
        public virtual double SupervisoryDelta(IAssetFxModel model) => System.Math.Sign(Notional);
        public double MaturityFactor(DateTime today, double? MPOR = null) => MPOR.HasValue ? SaCcrUtils.MfMargined(MPOR.Value) : SaCcrUtils.MfUnmargined(tEnd(today));
        public string HedgingSet { get; set; }
        public int MaturityBucket(DateTime today) => tEnd(today) <= 1.0 ? 1 : tEnd(today) <= 5.0 ? 2 : 3;

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model)
        {
            var forecastCurveCpi = model.FundingModel.Curves[ForecastCurveCpi];

            var forecast = (forecastCurveCpi as CPICurve).GetForecast(FixingDate, RateIndex.FixingLag.PeriodCount);

            var fv = (forecast - Strike) * Notional;

            return new List<CashFlow>
            {
                new CashFlow { Fv = fv, Currency = Currency, SettleDate = FixingDate }, 
            };
        }

        public double SuggestPillarValue(IFundingModel model) => Strike;
    }
}
