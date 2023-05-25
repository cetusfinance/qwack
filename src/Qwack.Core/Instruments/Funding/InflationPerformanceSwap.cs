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
    public class InflationPerformanceSwap : IFundingInstrument, ISaCcrEnabledIR, IIsInflationInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public InflationPerformanceSwap() { }

        public InflationPerformanceSwap(DateTime startDate, Frequency swapTenor, InflationIndex rateIndex, double parRate, double notional,
            SwapPayReceiveType swapType, string forecastCurveCpi, string discountCurve)
        {
            SwapTenor = swapTenor;
            ResetFrequency = rateIndex.ResetFrequency;
            StartDate = startDate;
            EndDate = StartDate.AddPeriod(rateIndex.RollConvention, rateIndex.HolidayCalendars, SwapTenor);
            ParRate = parRate;
            BasisFloat = rateIndex.DayCountBasis;
            BasisFixed = rateIndex.DayCountBasisFixed;
            SwapType = swapType;
            RateIndex = rateIndex;
            Currency = rateIndex.Currency;
            Notional = notional;
            ResetDates = new[] { StartDate, EndDate };

            ForecastCurveCpi = forecastCurveCpi;
            DiscountCurve = discountCurve;

            T = StartDate.CalculateYearFraction(EndDate, BasisFixed);
            FixedFlow = (System.Math.Pow(1.0 + parRate, T) - 1.0) * Notional * (swapType == SwapPayReceiveType.Payer ? -1.0 : 1.0);
        }

        public double FixedFlow { get; set; }
        public double T { get; set; }

        public double Notional { get; set; }
        public double ParRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double InitialFixing { get; set; }
        public DateTime[] ResetDates { get; set; }
        public Currency Currency { get; set; }

        public DayCountBasis BasisFixed { get; set; }
        public DayCountBasis BasisFloat { get; set; }
        public Frequency ResetFrequency { get; set; }
        public Frequency SwapTenor { get; set; }
        public SwapPayReceiveType SwapType { get; set; }
        public string ForecastCurveCpi { get; set; }
        public string DiscountCurve { get; set; }
        public string SolveCurve { get; set; }

        private DateTime? _pillarDate;
        public DateTime PillarDate
        {
            get => _pillarDate ?? EndDate;
            set
            {
                _pillarDate = value;
            }
        }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public InflationIndex RateIndex { get; set; }
        public string PortfolioName { get; set; }

        public DateTime LastSensitivityDate => EndDate;

        public List<string> Dependencies(IFxMatrix matrix) => (new[] { DiscountCurve, ForecastCurveCpi }).Distinct().Where(x => x != SolveCurve).ToList();

        public double Pv(IFundingModel model, bool updateState)
        {
            var discountCurve = model.Curves[DiscountCurve];
            var forecastCurveCpi = model.Curves[ForecastCurveCpi];
   
            if (InitialFixing == 0)
            {
                if (!model.TryGetFixingDictionary(ForecastCurveCpi, out var fixingDictionary))
                    throw new Exception($"Fixing dictionary not found for inflation index {ForecastCurveCpi}");

                InitialFixing = InflationUtils.InterpFixing(StartDate, fixingDictionary, RateIndex.FixingLag.PeriodCount);
            }

            var forecast = (forecastCurveCpi as CPICurve).GetForecast(EndDate, RateIndex.FixingLag.PeriodCount);

            var cpiPerf = forecast / InitialFixing - 1;

            var cpiLegFv = cpiPerf * Notional * (SwapType == SwapPayReceiveType.Payer ? 1.0 : -1.0);
            var fixedLegFv = FixedFlow;

            var df = discountCurve.GetDf(model.BuildDate, EndDate);

            return (cpiLegFv + fixedLegFv) * df;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            throw new NotImplementedException();
        }

        public double CalculateParRate(IFundingModel model)
        {
            var forecastCurveCpi = model.Curves[ForecastCurveCpi];
            if (InitialFixing == 0)
            {
                if (!model.TryGetFixingDictionary(ForecastCurveCpi, out var fixingDictionary))
                    throw new Exception($"Fixing dictionary not found for inflation index {ForecastCurveCpi}");

                InitialFixing = InflationUtils.InterpFixing(StartDate, fixingDictionary, RateIndex.FixingLag.PeriodCount);
            }

            var forecast = (forecastCurveCpi as CPICurve).GetForecast(EndDate, RateIndex.FixingLag.PeriodCount);

            var cpiPerf = forecast / InitialFixing;

            return System.Math.Pow(cpiPerf, 1 / T) - 1;
        }

        public IFundingInstrument Clone() => new InflationPerformanceSwap
        {
            BasisFixed = BasisFixed,
            BasisFloat = BasisFloat,
            Currency = Currency,
            Counterparty = Counterparty,
            DiscountCurve = DiscountCurve,
            EndDate = EndDate,
            FixedFlow = FixedFlow,        
            ForecastCurveCpi = ForecastCurveCpi,
            InitialFixing = InitialFixing,
            Notional = Notional,
            ParRate = ParRate,
            PillarDate = PillarDate,
            ResetDates = ResetDates,
            ResetFrequency = ResetFrequency,
            SolveCurve = SolveCurve,
            StartDate = StartDate,
            SwapTenor = SwapTenor,
            SwapType = SwapType,
            TradeId = TradeId,
            RateIndex = RateIndex,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet
        };

        public IFundingInstrument SetParRate(double parRate) => new InflationPerformanceSwap(StartDate, SwapTenor, RateIndex, parRate, Notional, SwapType, ForecastCurveCpi, DiscountCurve)
        {
            TradeId = TradeId,
            SolveCurve = SolveCurve,
            PillarDate = PillarDate,
            RateIndex = RateIndex,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet
        };

        public double TradeNotional => System.Math.Abs(Notional);
        public virtual double EffectiveNotional(IAssetFxModel model, double? MPOR = null) => SupervisoryDelta(model) * AdjustedNotional(model) * MaturityFactor(model.BuildDate, MPOR);
        public double AdjustedNotional(IAssetFxModel model) => TradeNotional * SupervisoryDuration(model.BuildDate);
        private double tStart(DateTime today) => today.CalculateYearFraction(StartDate, DayCountBasis.Act365F);
        private double tEnd(DateTime today) => today.CalculateYearFraction(EndDate, DayCountBasis.Act365F);
        public double SupervisoryDuration(DateTime today) => SaCcrUtils.SupervisoryDuration(tStart(today), tEnd(today));
        public virtual double SupervisoryDelta(IAssetFxModel model) => (SwapType == SwapPayReceiveType.Pay ? 1.0 : -1.0) * System.Math.Sign(Notional);
        public double MaturityFactor(DateTime today, double? MPOR = null) => MPOR.HasValue ? SaCcrUtils.MfMargined(MPOR.Value) : SaCcrUtils.MfUnmargined(tEnd(today));
        public string HedgingSet { get; set; }
        public int MaturityBucket(DateTime today) => tEnd(today) <= 1.0 ? 1 : tEnd(today) <= 5.0 ? 2 : 3;

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model)
        {
            var discountCurve = model.FundingModel.Curves[DiscountCurve];
            var forecastCurveCpi = model.FundingModel.Curves[ForecastCurveCpi];

            if (InitialFixing == 0)
            {
                if (!model.FundingModel.TryGetFixingDictionary(ForecastCurveCpi, out var fixingDictionary))
                    throw new Exception($"Fixing dictionary not found for inflation index {ForecastCurveCpi}");

                InitialFixing = InflationUtils.InterpFixing(StartDate, fixingDictionary, RateIndex.FixingLag.PeriodCount);
            }

            var forecast = (forecastCurveCpi as CPICurve).GetForecast(EndDate, RateIndex.FixingLag.PeriodCount);

            var cpiPerf = forecast / InitialFixing - 1;

            var cpiLegFv = cpiPerf * Notional * (SwapType == SwapPayReceiveType.Payer ? 1.0 : -1.0);
            var fixedLegFv = FixedFlow;

            return new List<CashFlow>
            {
                new CashFlow { Fv = cpiLegFv, Currency = Currency, SettleDate = EndDate }, 
                new CashFlow { Fv = fixedLegFv, Currency = Currency, SettleDate = EndDate },
            };
        }

        public double SuggestPillarValue(IFundingModel model) => ParRate;
    }
}
