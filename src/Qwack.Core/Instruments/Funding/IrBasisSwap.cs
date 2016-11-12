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
    public class IrBasisSwap : IFundingInstrument
    {
        public IrBasisSwap(DateTime startDate, Frequency swapTenor, double parSpread, bool spreadOnPayLeg, FloatRateIndex payIndex, FloatRateIndex recIndex, string forecastCurvePay, string forecastCurveRec, string discountCurve)
        {
            SwapTenor = swapTenor;

            ResetFrequencyRec = recIndex.ResetTenor;
            ResetFrequencyPay = payIndex.ResetTenor;

            StartDate = startDate;
            EndDate = StartDate.AddPeriod(payIndex.RollConvention, payIndex.HolidayCalendars, SwapTenor);

            ParSpreadPay = spreadOnPayLeg ? parSpread : 0.0;
            ParSpreadRec = spreadOnPayLeg ? 0.0 : parSpread;
            BasisPay = payIndex.DayCountBasis;
            BasisRec = recIndex.DayCountBasis;

            PayLeg = new GenericSwapLeg(StartDate, swapTenor, payIndex.HolidayCalendars, payIndex.Currency, ResetFrequencyPay, BasisPay);
            PayLeg.FixedRateOrMargin = (decimal)ParSpreadPay;
            PayLeg.LegType = SwapLegType.Float;

            RecLeg = new GenericSwapLeg(StartDate, swapTenor, recIndex.HolidayCalendars, recIndex.Currency, ResetFrequencyRec, BasisRec);
            RecLeg.FixedRateOrMargin = (decimal)ParSpreadRec;
            RecLeg.LegType = SwapLegType.Float;


            FlowSchedulePay = PayLeg.GenerateSchedule();
            FlowScheduleRec = RecLeg.GenerateSchedule();

            ResetDates = FlowSchedulePay.Flows.Select(x => x.FixingDateStart)
                .Union(FlowScheduleRec.Flows.Select(y => y.FixingDateStart))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            ForecastCurvePay = forecastCurvePay;
            ForecastCurveRec = forecastCurveRec;
            DiscountCurve = discountCurve;
        }

        public double Notional { get; set; }
        public double ParSpreadPay { get; set; }
        public double ParSpreadRec { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NDates { get; set; }
        public DateTime[] ResetDates { get; set; }
        public Currency CCY { get; set; }
        public GenericSwapLeg PayLeg { get; set; }
        public GenericSwapLeg RecLeg { get; set; }
        public CashFlowSchedule FlowSchedulePay { get; set; }
        public CashFlowSchedule FlowScheduleRec { get; set; }
        public DayCountBasis BasisPay { get; set; }
        public DayCountBasis BasisRec { get; set; }
        public Frequency ResetFrequencyPay { get; set; }
        public Frequency ResetFrequencyRec { get; set; }
        public Frequency SwapTenor { get; set; }
        public string ForecastCurvePay { get; set; }
        public string ForecastCurveRec { get; set; }
        public string DiscountCurve { get; set; }
        public string SolveCurve { get; set; }

        public double Pv(FundingModel model, bool updateState)
        {
            bool updateDF = updateState || model.CurrentSolveCurve == DiscountCurve;
            bool updatePayEst = updateState || model.CurrentSolveCurve == ForecastCurvePay;
            bool updateRecEst = updateState || model.CurrentSolveCurve == ForecastCurveRec;

            return Pv(model.Curves[DiscountCurve], model.Curves[ForecastCurvePay], model.Curves[ForecastCurveRec], updateState, updateDF, updatePayEst, updateRecEst);
        }

        public CashFlowSchedule ExpectedCashFlows(FundingModel model)
        {
            throw new NotImplementedException();
        }
        public double Pv(IrCurve discountCurve, IrCurve forecastCurvePay, IrCurve forecastCurveRec, bool updateState, bool updateDF, bool updatePayEst, bool updateRecEst)
        {
            double totalPV = 0;

            for (int i = 0; i < FlowSchedulePay.Flows.Count; i++)
            {
                double FV, DF;

                var flow = FlowSchedulePay.Flows[i];

                if (updatePayEst)
                {
                    DateTime s = flow.AccrualPeriodStart;
                    DateTime e = flow.AccrualPeriodEnd;
                    double RateLin = forecastCurvePay.GetForwardRate(s, e, RateType.Linear, BasisPay)
                        + flow.FixedRateOrMargin;
                    double YF = flow.DiscountFactor;
                    FV = RateLin * YF * flow.Notional;
                    FV *= -1.0;
                }
                else
                    FV = flow.Fv;

                if (updateDF)
                    DF = discountCurve.Pv(1, flow.SettleDate);
                else
                    DF = (flow.Fv == flow.Pv) ? 1.0 : flow.Pv / flow.Fv;

                double PV = DF * FV;

                if (updateState)
                {
                    flow.Fv = FV;
                    flow.Pv = PV;
                }

                totalPV += PV;
            }

            for (int i = 0; i < FlowScheduleRec.Flows.Count; i++)
            {
                double FV, DF;

                var flow = FlowScheduleRec.Flows[i];

                if (updateRecEst)
                {
                    DateTime s = flow.AccrualPeriodStart;
                    DateTime e = flow.AccrualPeriodEnd;
                    double RateLin = forecastCurveRec.GetForwardRate(s, e, RateType.Linear, BasisRec)
                        + flow.FixedRateOrMargin;
                    double YF = flow.DiscountFactor;
                    FV = RateLin * YF * flow.Notional;
                }
                else
                    FV = flow.Fv;

                if (updateDF)
                    DF = discountCurve.Pv(1, flow.SettleDate);
                else
                    DF = (flow.Fv == flow.Pv) ? 1.0 : flow.Pv / flow.Fv;

                double PV = DF * FV;

                if (updateState)
                {
                    flow.Fv = FV;
                    flow.Pv = PV;
                }

                totalPV += PV;
            }

            return totalPV;
        }

    }
}
