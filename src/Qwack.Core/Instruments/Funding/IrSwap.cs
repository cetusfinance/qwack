using System;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class IrSwap : IFundingInstrument
    {
        public IrSwap(DateTime startDate, string swapTenor, FloatRateIndex rateIndex, double parRate,
            SwapPayReceiveType swapType, string discountCurve, string forecastCurve)
        {
            SwapTenor = new Frequency(swapTenor);
            ResetFrequency = rateIndex.ResetTenor;
            StartDate = startDate;
            EndDate = StartDate.AddPeriod(rateIndex.RollConvention, rateIndex.HolidayCalendars, SwapTenor);
            ParRate = parRate;
            BasisFloat = rateIndex.DayCountBasis;
            BasisFixed = rateIndex.DayCountBasisFixed;
            SwapType = swapType;

            FixedLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency,
                ResetFrequency, BasisFixed);
            FixedLeg.FixedRateOrMargin = (decimal) parRate;
            FixedLeg.LegType = SwapLegType.Fixed;

            FloatLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency,
                ResetFrequency, BasisFloat);
            FloatLeg.FixedRateOrMargin = 0.0M;
            FloatLeg.LegType = SwapLegType.Float;

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

        private double PV(IrCurve discountCurve, IrCurve forecastCurve, bool updateState, bool updateDf,
            bool updateEstimate)
        {
            double totalPv = 0;

            for (var i = 0; i < FlowScheduleFixed.Flows.Count; i++)
            {
                var flow = FlowScheduleFixed.Flows[i];
                double fv, df;
                if (updateState)
                {
                    var rateLin = flow.FixedRateOrMargin;
                    var yf = flow.DiscountFactor;
                    fv = rateLin * yf * flow.Notional;
                    fv *= SwapType == SwapPayReceiveType.Payer ? 1.0 : -1.0;
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
                    var yf = flow.DiscountFactor;
                    fv = rateLin * yf * flow.Notional;
                    fv *= SwapType == SwapPayReceiveType.Payer ? -1.0 : 1.0;
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
    }
}
