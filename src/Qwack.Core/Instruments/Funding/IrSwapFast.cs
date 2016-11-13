using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class IrSwapFast : IFundingInstrument
    {
        public IrSwapFast(DateTime startDate, Frequency swapTenor, FloatRateIndex rateIndex, double parRate, SwapPayReceiveType swapType, string discountCurve, string forecastCurve)
        {
            SwapTenor = swapTenor;
            ResetFrequency = rateIndex.ResetTenor;
            StartDate = startDate;
            EndDate = StartDate.AddPeriod(rateIndex.RollConvention, rateIndex.HolidayCalendars, SwapTenor);
            ParRate = parRate;
            BasisFloat = rateIndex.DayCountBasis;
            BasisFixed = rateIndex.DayCountBasisFixed;
            SwapType = swapType;

            FixedLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency, ResetFrequency, BasisFixed);
            FixedLeg.FixedRateOrMargin = (decimal)parRate;
            FixedLeg.LegType = SwapLegType.Fixed;

            FloatLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency, ResetFrequency, BasisFloat);
            FloatLeg.FixedRateOrMargin = 0.0M;
            FloatLeg.LegType = SwapLegType.Float;

            FlowScheduleFixed = FixedLeg.GenerateSchedule();
            FlowScheduleFloat = FloatLeg.GenerateSchedule();

            ResetDates = FlowScheduleFloat.Flows.Select(x => x.FixingDateStart).ToArray();

            ForecastCurve = forecastCurve;
            DiscountCurve = discountCurve;

            UpdateFV();
        }

        private double[] fixedFlowsFV;
        private double[] fixedFlowsTBasis;
        private double[] floatFlowsFV;
        private double[] floatFlowsTBasis;
        public string SolveCurve { get; set; }

        private void UpdateFV()
        {
            fixedFlowsFV = new double[FlowScheduleFixed.Flows.Count];
            fixedFlowsTBasis = new double[FlowScheduleFixed.Flows.Count];
            for (int i = 0; i < FlowScheduleFixed.Flows.Count; i++)
            {
                DateTime s = FlowScheduleFixed.Flows[i].AccrualPeriodStart;
                DateTime e = FlowScheduleFixed.Flows[i].AccrualPeriodEnd;
                double RateLin = FlowScheduleFixed.Flows[i].FixedRateOrMargin;
                double YF = DateExtensions.CalculateYearFraction(s, e, BasisFixed);
                double FV = RateLin * YF * FlowScheduleFixed.Flows[i].Notional;
                FV *= (SwapType == SwapPayReceiveType.Payer) ? 1.0 : -1.0;
                fixedFlowsFV[i] = FV;
                fixedFlowsTBasis[i] = YF;
            }
            floatFlowsFV = new double[FlowScheduleFloat.Flows.Count];
            floatFlowsTBasis = new double[FlowScheduleFloat.Flows.Count];
            for (int i = 0; i < FlowScheduleFloat.Flows.Count; i++)
            {
                DateTime s = FlowScheduleFloat.Flows[i].AccrualPeriodStart;
                DateTime e = FlowScheduleFloat.Flows[i].AccrualPeriodEnd;
                double YF = DateExtensions.CalculateYearFraction(s, e, BasisFloat);

                double FV = YF * FlowScheduleFloat.Flows[i].Notional;
                FV *= (SwapType == SwapPayReceiveType.Payer) ? -1.0 : 1.0;
                floatFlowsFV[i] = FV;
                floatFlowsTBasis[i] = YF;
            }
        }

        #region Properties
        public double Notional { get; private set; }
        public double ParRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NDates { get; set; }
        public DateTime[] ResetDates { get; set; }
        public Currency CCY { get; set; }
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
        #endregion
        public double Pv(FundingModel model, bool updateState)
        {
            return Pv(model.Curves[DiscountCurve], model.Curves[ForecastCurve]);
        }

        private double Pv(IrCurve discountCurve, IrCurve forecastCurve)
        {
            double totalPV = 0;
            var ffPV = fixedFlowsFV;
            var flows = FlowScheduleFixed.Flows;
            for (int i = 0; i < flows.Count; i++)
            {
                totalPV += discountCurve.Pv(ffPV[i], flows[i].SettleDate);
            }

            ffPV = floatFlowsFV;
            var ffTBasis = floatFlowsTBasis;
            flows = FlowScheduleFloat.Flows;
            for (int i = 0; i < FlowScheduleFloat.Flows.Count; i++)
            {
                DateTime s = flows[i].AccrualPeriodStart;
                DateTime e = flows[i].AccrualPeriodEnd;
                double RateLin = forecastCurve.GetForwardRate(s, e, RateType.Linear, ffTBasis[i]);
                var FV = ffPV[i] * RateLin;

                totalPV += discountCurve.Pv(FV, flows[i].SettleDate);
            }

            return totalPV;
        }

        public CashFlowSchedule ExpectedCashFlows(FundingModel model)
        {
            throw new NotImplementedException();
        }
    }
}