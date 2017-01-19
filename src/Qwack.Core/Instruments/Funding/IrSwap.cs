using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class IrSwap : IFundingInstrument
    {
        public IrSwap(DateTime startDate, Frequency swapTenor, FloatRateIndex rateIndex, double parRate,
            SwapPayReceiveType swapType,  string forecastCurve, string discountCurve)
        {
            SwapTenor = swapTenor;
            ResetFrequency = rateIndex.ResetTenor;
            StartDate = startDate;
            EndDate = StartDate.AddPeriod(rateIndex.RollConvention, rateIndex.HolidayCalendars, SwapTenor);
            ParRate = parRate;
            BasisFloat = rateIndex.DayCountBasis;
            BasisFixed = rateIndex.DayCountBasisFixed;
            SwapType = swapType;

            FixedLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency,
                ResetFrequency, BasisFixed);
            FixedLeg.FixedRateOrMargin = (decimal)parRate;
            FixedLeg.LegType = SwapLegType.Fixed;
            FixedLeg.Nominal = 1e6M * (swapType == SwapPayReceiveType.Payer ? -1.0M : 1.0M);

            FloatLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency,
                ResetFrequency, BasisFloat);
            FloatLeg.FixedRateOrMargin = 0.0M;
            FloatLeg.LegType = SwapLegType.Float;
            FloatLeg.Nominal = 1e6M * (swapType == SwapPayReceiveType.Payer ? 1.0M : -1.0M);

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
        public Currency Ccy { get; set; }
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

        public double Pv(FundingModel model, bool updateState)
        {
            var updateDf = updateState || (model.CurrentSolveCurve == DiscountCurve);
            var updateEst = updateState || (model.CurrentSolveCurve == ForecastCurve);

            return PV(model.Curves[DiscountCurve], model.Curves[ForecastCurve], updateState, updateDf, updateEst);
        }

        private double PV(IrCurve discountCurve, IrCurve forecastCurve, bool updateState, bool updateDf, bool updateEstimate)
        {
            double totalPv = 0;

            for (var i = 0; i < FlowScheduleFixed.Flows.Count; i++)
            {
                var flow = FlowScheduleFixed.Flows[i];
                double fv, df;
                if (updateState)
                {
                    var rateLin = flow.FixedRateOrMargin;
                    var yf = flow.NotionalByYearFraction;
                    fv = rateLin * yf * flow.Notional;
                }
                else
                {
                    fv = flow.Fv;
                }

                if (updateDf)
                {
                    df = discountCurve.Pv(1, flow.SettleDate);
                }
                else
                {
                    df = flow.Fv == flow.Pv ? 1.0 : flow.Pv / flow.Fv;
                }

                var pv = fv * df;

                totalPv += pv;

                if (updateState)
                {
                    flow.Fv = fv;
                    flow.Pv = pv;
                }
            }

            for (var i = 0; i < FlowScheduleFloat.Flows.Count; i++)
            {
                var flow = FlowScheduleFloat.Flows[i];
                double fv, df;

                if (updateEstimate)
                {
                    var s = flow.AccrualPeriodStart;
                    var e = flow.AccrualPeriodEnd;
                    var rateLin = forecastCurve.GetForwardRate(s, e, RateType.Linear, BasisFloat);
                    var yf = flow.NotionalByYearFraction;
                    fv = rateLin * yf * flow.Notional;
                }
                else
                {
                    fv = flow.Fv;
                }

                if (updateDf)
                    df = discountCurve.Pv(1, flow.SettleDate);
                else
                    df = flow.Fv == flow.Pv ? 1.0 : flow.Pv / flow.Fv;

                var pv = fv * df;
                totalPv += pv;

                if (updateState)
                {
                    flow.Fv = fv;
                    flow.Pv = pv;
                }
            }
            return totalPv;
        }

        public CashFlowSchedule ExpectedCashFlows(FundingModel model)
        {
            throw new NotImplementedException();
        }

        //assumes zero cc rates for now
        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(FundingModel model)
        {
            //discounting first
            var discountDict = new Dictionary<DateTime, double>();
            var discountCurve = model.Curves[DiscountCurve];
            foreach (var flow in FlowScheduleFloat.Flows.Union(FlowScheduleFixed.Flows))
            {
                double t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.SettleDate);
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
                double RateFloat = flow.Fv / (flow.Notional * flow.NotionalByYearFraction);

                double ts = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.AccrualPeriodStart);
                double te = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.AccrualPeriodEnd);
                var dPVdR = df * flow.NotionalByYearFraction * flow.Notional;
                var dPVdS = dPVdR * (-ts * (RateFloat + 1.0 / flow.NotionalByYearFraction));
                var dPVdE = dPVdR * (te * (RateFloat + 1.0 / flow.NotionalByYearFraction));

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
    }
}
