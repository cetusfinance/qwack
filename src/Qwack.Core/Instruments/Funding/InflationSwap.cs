using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Funding
{
    public class InflationSwap : IFundingInstrument, ISaCcrEnabledIR
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public InflationSwap() { }

        public InflationSwap(DateTime startDate, Frequency swapTenor, InflationIndex rateIndex, double parRate,
            SwapPayReceiveType swapType, string forecastCurve, string discountCurve)
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

            FixedLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency,
                ResetFrequency, BasisFixed)
            {
                FixedRateOrMargin = (decimal)parRate,
                LegType = SwapLegType.Fixed,
                Nominal = 1e6M * (swapType == SwapPayReceiveType.Payer ? -1.0M : 1.0M),
                AccrualDCB = rateIndex.DayCountBasisFixed
            };
            FloatLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency,
                ResetFrequency, BasisFloat)
            {
                FixedRateOrMargin = 0.0M,
                LegType = SwapLegType.Float,
                Nominal = 1e6M * (swapType == SwapPayReceiveType.Payer ? 1.0M : -1.0M),
                AccrualDCB = rateIndex.DayCountBasis
            };
            FlowScheduleFixed = FixedLeg.GenerateSchedule();
            FlowScheduleFloat = FloatLeg.GenerateSchedule();

            ResetDates = FlowScheduleFloat.Flows.Select(x => x.FixingDateStart).ToArray();

            ForecastCurve = forecastCurve;
            DiscountCurve = discountCurve;
        }

        public double Notional { get; set; }
        public double ParRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NDates { get; set; }
        public DateTime[] ResetDates { get; set; }
        public Currency Currency { get; set; }
        public GenericSwapLeg FixedLeg { get; set; }
        public GenericSwapLeg FloatLeg { get; set; }
        public CashFlowSchedule FlowScheduleFixed { get; set; }
        public CashFlowSchedule FlowScheduleFloat { get; set; }
        public DayCountBasis BasisFixed { get; set; }
        public DayCountBasis BasisFloat { get; set; }
        public Frequency ResetFrequency { get; set; }
        public Frequency SwapTenor { get; set; }
        public SwapPayReceiveType SwapType { get; set; }
        public string ForecastCurve { get; set; }
        public string DiscountCurve { get; set; }
        public string SolveCurve { get; set; }
        public DateTime PillarDate { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public InflationIndex RateIndex { get; set; }
        public string PortfolioName { get; set; }

        public DateTime LastSensitivityDate => EndDate;

        public List<string> Dependencies(IFxMatrix matrix) => (new[] { DiscountCurve, ForecastCurve }).Distinct().Where(x => x != SolveCurve).ToList();

        public double Pv(IFundingModel model, bool updateState)
        {
            var updateDf = updateState || (model.CurrentSolveCurve == DiscountCurve);
            var updateEst = updateState || (model.CurrentSolveCurve == ForecastCurve);

            var discountCurve = model.Curves[DiscountCurve];
            var forecastCurve = model.Curves[ForecastCurve];
            var fixedPv = FlowScheduleFixed.PV(discountCurve, forecastCurve, updateState, updateDf, updateEst, BasisFloat, null);
            var floatPv = FlowScheduleFloat.PV(discountCurve, forecastCurve, updateState, updateDf, updateEst, BasisFloat, null);

            return fixedPv + floatPv;
        }

        //assumes zero cc rates for now
        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            //discounting first
            var discountDict = new Dictionary<DateTime, double>();
            var discountCurve = model.Curves[DiscountCurve];
            foreach (var flow in FlowScheduleFloat.Flows.Union(FlowScheduleFixed.Flows))
            {
                var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.SettleDate);
                if (discountDict.ContainsKey(flow.SettleDate))
                    discountDict[flow.SettleDate] += -t * flow.Pv;
                else
                    discountDict.Add(flow.SettleDate, -t * flow.Pv);
            }


            //then forecast
            var forecastDict = (ForecastCurve == DiscountCurve) ? discountDict : new Dictionary<DateTime, double>();
            var forecastCurve = model.Curves[ForecastCurve];
            foreach (var flow in FlowScheduleFloat.Flows)
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


            if (ForecastCurve == DiscountCurve)
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurve,discountDict },
            };
            else
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurve,discountDict },
                {ForecastCurve,forecastDict },
            };
        }

        public double CalculateParRate(IFundingModel model)
        {
            var dFs = FlowScheduleFloat.Flows.Select(x => x.SettleDate).Select(y => model.Curves[DiscountCurve].GetDf(model.BuildDate, y));
            var floatRates = FlowScheduleFloat.Flows.Select(x => x.GetFloatRate(model.Curves[ForecastCurve], BasisFloat)).ToArray();
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
            FixedLeg = FixedLeg.Clone(),
            FloatLeg = FloatLeg.Clone(),
            FlowScheduleFixed = FlowScheduleFixed.Clone(),
            FlowScheduleFloat = FlowScheduleFloat.Clone(),
            ForecastCurve = ForecastCurve,
            NDates = NDates,
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

        public IFundingInstrument SetParRate(double parRate) => new InflationSwap(StartDate, SwapTenor, RateIndex, parRate, SwapType, ForecastCurve, DiscountCurve)
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
            return FlowScheduleFixed.Flows.Concat(FlowScheduleFloat.Flows).ToList();
        }

        public double SuggestPillarValue(IFundingModel model) => ParRate;
    }
}
