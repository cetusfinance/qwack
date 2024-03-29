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
    public class InflationSwap : IFundingInstrument, ISaCcrEnabledIR, IIsInflationInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public InflationSwap() { }

        public InflationSwap(DateTime startDate, Frequency swapTenor, InflationIndex rateIndex, double parRate,
            SwapPayReceiveType swapType, string forecastCurveCpi, string forecastCurveIr, string discountCurve)
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

            CpiLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency,
                ResetFrequency, BasisFixed)
            {
                FixedRateOrMargin = (decimal)parRate,
                LegType = SwapLegType.InflationCoupon,
                Nominal = 1e6M * (swapType == SwapPayReceiveType.Payer ? -1.0M : 1.0M),
                AccrualDCB = rateIndex.DayCountBasisFixed
            };
            IrLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency,
                ResetFrequency, BasisFloat)
            {
                FixedRateOrMargin = 0.0M,
                LegType = SwapLegType.Float,
                Nominal = 1e6M * (swapType == SwapPayReceiveType.Payer ? 1.0M : -1.0M),
                AccrualDCB = rateIndex.DayCountBasis
            };
            FlowScheduleCpiLinked = CpiLeg.GenerateSchedule();
            foreach(var flow in FlowScheduleCpiLinked.Flows)
            {
                flow.CpiFixingLagInMonths = rateIndex.FixingLag.PeriodCount;
            }
            FlowScheduleIrLinked = IrLeg.GenerateSchedule();

            ResetDates = FlowScheduleIrLinked.Flows.Select(x => x.FixingDateStart).ToArray();

            ForecastCurveCpi = forecastCurveCpi;
            ForecastCurveIr = forecastCurveIr;
            DiscountCurve = discountCurve;
        }

        public double Notional { get; set; }
        public double ParRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double InitialFixing { get; set; }
        public DateTime[] ResetDates { get; set; }
        public Currency Currency { get; set; }
        public GenericSwapLeg CpiLeg { get; set; }
        public GenericSwapLeg IrLeg { get; set; }
        public CashFlowSchedule FlowScheduleCpiLinked { get; set; }
        public CashFlowSchedule FlowScheduleIrLinked { get; set; }
        public DayCountBasis BasisFixed { get; set; }
        public DayCountBasis BasisFloat { get; set; }
        public Frequency ResetFrequency { get; set; }
        public Frequency SwapTenor { get; set; }
        public SwapPayReceiveType SwapType { get; set; }
        public string ForecastCurveCpi { get; set; }
        public string ForecastCurveIr { get; set; }
        public string DiscountCurve { get; set; }
        public string SolveCurve { get; set; }
        public DateTime PillarDate { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public InflationIndex RateIndex { get; set; }
        public string PortfolioName { get; set; }
       

        public DateTime LastSensitivityDate => EndDate;

        public List<string> Dependencies(IFxMatrix matrix) => (new[] { DiscountCurve, ForecastCurveCpi, ForecastCurveIr }).Distinct().Where(x => x != SolveCurve).ToList();

        public double Pv(IFundingModel model, bool updateState)
        {
            var updateDf = updateState || (model.CurrentSolveCurve == DiscountCurve);
            var updateEst = updateState || (model.CurrentSolveCurve == ForecastCurveCpi);

            var discountCurve = model.Curves[DiscountCurve];
            var forecastCurveCpi = model.Curves[ForecastCurveCpi];
            var forecastCurveIr = model.Curves[ForecastCurveIr];

            if (InitialFixing == 0)
            {
                if (!model.TryGetFixingDictionary(ForecastCurveCpi, out var fixingDictionary))
                    throw new Exception($"Fixing dictionary not found for inflation index {ForecastCurveCpi}");

                InitialFixing = InflationUtils.InterpFixing(StartDate, fixingDictionary, RateIndex.FixingLag.PeriodCount);
            }
            var cpiLegPv = FlowScheduleCpiLinked.PV(discountCurve, forecastCurveCpi, updateState, updateDf, updateEst, BasisFloat, null, InitialFixing);
            var irLegPv = FlowScheduleIrLinked.PV(discountCurve, forecastCurveIr, updateState, updateDf, updateEst, BasisFloat, null);

            return cpiLegPv + irLegPv;
        }

        //assumes zero cc rates for now
        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            //discounting first
            var discountDict = new Dictionary<DateTime, double>();
            var discountCurve = model.Curves[DiscountCurve];
            foreach (var flow in FlowScheduleIrLinked.Flows.Union(FlowScheduleCpiLinked.Flows))
            {
                var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.SettleDate);
                if (discountDict.ContainsKey(flow.SettleDate))
                    discountDict[flow.SettleDate] += -t * flow.Pv;
                else
                    discountDict.Add(flow.SettleDate, -t * flow.Pv);
            }


            //then forecast
            var forecastDict = (ForecastCurveCpi == DiscountCurve) ? discountDict : new Dictionary<DateTime, double>();
            var forecastCurve = model.Curves[ForecastCurveCpi];
            foreach (var flow in FlowScheduleIrLinked.Flows)
            {
                var df = flow.Fv == flow.Pv ? 1.0 : flow.Pv / flow.Fv;
                var RateFloat = flow.Fv / (flow.Notional * flow.YearFraction);

                var ts = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.AccrualPeriodStart);
                var te = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.AccrualPeriodEnd);
                var dPVdR = df * flow.YearFraction * flow.Notional;
                var dPVdS = dPVdR * (-ts * (RateFloat + 1.0 / flow.YearFraction));
                var dPVdE = dPVdR * (te * (RateFloat + 1.0 / flow.YearFraction));

                if (forecastDict.ContainsKey(flow.AccrualPeriodStart))
                    forecastDict[flow.AccrualPeriodStart] += dPVdS;
                else
                    forecastDict.Add(flow.AccrualPeriodStart, dPVdS);

                if (forecastDict.ContainsKey(flow.AccrualPeriodEnd))
                    forecastDict[flow.AccrualPeriodEnd] += dPVdE;
                else
                    forecastDict.Add(flow.AccrualPeriodEnd, dPVdE);
            }


            if (ForecastCurveCpi == DiscountCurve)
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurve,discountDict },
            };
            else
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurve,discountDict },
                {ForecastCurveCpi,forecastDict },
            };
        }

        public double CalculateParRate(IFundingModel model)
        {
            var dFs = FlowScheduleIrLinked.Flows.Select(x => x.SettleDate).Select(y => model.Curves[DiscountCurve].GetDf(model.BuildDate, y));
            var floatRates = FlowScheduleIrLinked.Flows.Select(x => x.GetFloatRate(model.Curves[ForecastCurveCpi], BasisFloat)).ToArray();
            var parRate = dFs.Select((x, ix) => x * floatRates[ix]).Sum() / dFs.Sum();
            return parRate;
        }

        public IFundingInstrument Clone() => new InflationSwap
        {
            BasisFixed = BasisFixed,
            BasisFloat = BasisFloat,
            Currency = Currency,
            Counterparty = Counterparty,
            DiscountCurve = DiscountCurve,
            EndDate = EndDate,
            CpiLeg = CpiLeg.Clone(),
            IrLeg = IrLeg.Clone(),
            FlowScheduleCpiLinked = FlowScheduleCpiLinked.Clone(),
            FlowScheduleIrLinked = FlowScheduleIrLinked.Clone(),
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

        public IFundingInstrument SetParRate(double parRate) => new InflationSwap(StartDate, SwapTenor, RateIndex, parRate, SwapType, ForecastCurveCpi, ForecastCurveIr, DiscountCurve)
        {
            TradeId = TradeId,
            SolveCurve = SolveCurve,
            PillarDate = PillarDate,
            Notional = Notional,
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
            Pv(model.FundingModel, true);
            return FlowScheduleCpiLinked.Flows.Concat(FlowScheduleIrLinked.Flows).ToList();
        }

        public double SuggestPillarValue(IFundingModel model) => ParRate;
    }
}
