using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class XccyBasisSwap : IFundingInstrument
    {
        public double NotionalPay { get; set; }
        public double NotionalRec { get; set; }
        public double ParSpreadPay { get; set; }
        public double ParSpreadRec { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NDates { get; set; }
        public DateTime[] ResetDates { get; set; }
        public Currency CCYPay { get; set; }
        public Currency CCYRec { get; set; }
        public Currency PVCCY { get; set; }
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
        public string DiscountCurvePay { get; set; }
        public string DiscountCurveRec { get; set; }

        public MTMSwapType MTMSwapType { get; set; }
        public ExchangeType NotionalExchange { get; set; }
        public string SolveCurve { get; set; }

        public XccyBasisSwap(DateTime startDate, Frequency swapTenor, double parSpread, bool spreadOnPayLeg, FloatRateIndex payIndex, FloatRateIndex recIndex, ExchangeType notionalExchange, MTMSwapType mtmSwapType, string forecastCurvePay, string forecastCurveRec, string discountCurvePay, string discountCurveRec)
        {
            SwapTenor = swapTenor;
            NotionalExchange = notionalExchange;
            MTMSwapType = mtmSwapType;

            ResetFrequencyRec = recIndex.ResetTenor;
            ResetFrequencyPay = payIndex.ResetTenor;

            StartDate = startDate;
            EndDate = StartDate.AddPeriod(payIndex.RollConvention, payIndex.HolidayCalendars, SwapTenor);

            ParSpreadPay = spreadOnPayLeg ? parSpread : 0.0;
            ParSpreadRec = spreadOnPayLeg ? 0.0 : parSpread;
            BasisPay = payIndex.DayCountBasis;
            BasisRec = recIndex.DayCountBasis;

            CCYPay = payIndex.Currency;
            CCYRec = recIndex.Currency;

            PayLeg = new GenericSwapLeg(StartDate, swapTenor, payIndex.HolidayCalendars, payIndex.Currency, ResetFrequencyPay, BasisPay);
            PayLeg.FixedRateOrMargin = (decimal)ParSpreadPay;
            PayLeg.NotionalExchange = NotionalExchange;
            PayLeg.Direction = SwapPayReceiveType.Payer;
            PayLeg.LegType = SwapLegType.Float;

            RecLeg = new GenericSwapLeg(StartDate, swapTenor, recIndex.HolidayCalendars, recIndex.Currency, ResetFrequencyRec, BasisRec);
            RecLeg.FixedRateOrMargin = (decimal)ParSpreadRec;
            RecLeg.NotionalExchange = NotionalExchange;
            PayLeg.Direction = SwapPayReceiveType.Receiver;
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
            DiscountCurvePay = discountCurvePay;
            DiscountCurveRec = discountCurveRec;
        }

        public double Pv(FundingModel model, bool updateState)
        {
            bool updateDFPay = updateState || model.CurrentSolveCurve == DiscountCurvePay;
            bool updateDFRec = updateState || model.CurrentSolveCurve == DiscountCurveRec;
            bool updatePayEst = updateState || model.CurrentSolveCurve == ForecastCurvePay;
            bool updateRecEst = updateState || model.CurrentSolveCurve == ForecastCurveRec;

            return PV(model, updateState, updateDFPay, updateDFRec, updatePayEst, updateRecEst);
        }

        public CashFlowSchedule ExpectedCashFlows(FundingModel model)
        {
            throw new NotImplementedException();
        }
        public double PV(FundingModel model, bool updateState, bool updateDFPay, bool updateDFRec, bool updatePayEst, bool updateRecEst)
        {
            var discountCurvePay = model.Curves[DiscountCurvePay];
            var discountCurveRec = model.Curves[DiscountCurveRec];
            var forecastCurvePay = model.Curves[ForecastCurvePay];
            var forecastCurveRec = model.Curves[ForecastCurveRec];
            double totalPVRec = 0;
            double totalPVPay = 0;


            var payCCY = MTMSwapType == MTMSwapType.ReceiveNotionalFixed ? CCYRec : CCYPay;
            var recCCY = MTMSwapType == MTMSwapType.PayNotionalFixed ? CCYPay : CCYRec;
            Currency baseCCY = PVCCY ?? payCCY;

            double fxPayToBase = model.GetFxRate(model.BuildDate, payCCY, baseCCY);
            double fxRecToBase = model.GetFxRate(model.BuildDate, recCCY, baseCCY);

            double fixedNotional = (double)(MTMSwapType == MTMSwapType.PayNotionalFixed ? PayLeg.Nominal :
                MTMSwapType == MTMSwapType.ReceiveNotionalFixed ? RecLeg.Nominal : 0M);

            for (int i = 0; i < FlowSchedulePay.Flows.Count; i++)
            {
                double FV, DF;

                var flow = FlowSchedulePay.Flows[i];

                if (updatePayEst && flow.FlowType != FlowType.FixedAmount)
                {
                    DateTime s = flow.AccrualPeriodStart;
                    DateTime e = flow.AccrualPeriodEnd;
                    double RateLin = forecastCurvePay.GetForwardRate(s, e, RateType.Linear, BasisPay)
                        + flow.FixedRateOrMargin;
                    double YF = flow.NotionalByYearFraction;
                    FV = RateLin * YF * (MTMSwapType == MTMSwapType.ReceiveNotionalFixed ? fixedNotional : flow.Notional);
                    FV *= -1.0;
                }
                else
                    FV = flow.Fv;

                if (updateDFPay)
                    DF = discountCurvePay.Pv(1, flow.SettleDate);
                else
                    DF = (flow.Fv == flow.Pv) ? 1.0 : flow.Pv / flow.Fv;

                double PV = DF * FV;

                if (updateState)
                {
                    flow.Fv = FV;
                    flow.Pv = PV;
                }

                totalPVPay += PV;
            }

            for (int i = 0; i < FlowScheduleRec.Flows.Count; i++)
            {
                double FV, DF;

                var flow = FlowScheduleRec.Flows[i];

                if (updateRecEst && flow.FlowType != FlowType.FixedAmount)
                {
                    DateTime s = flow.AccrualPeriodStart;
                    DateTime e = flow.AccrualPeriodEnd;
                    double RateLin = forecastCurveRec.GetForwardRate(s, e, RateType.Linear, BasisRec)
                        + flow.FixedRateOrMargin;
                    double YF = flow.NotionalByYearFraction;
                    FV = RateLin * YF * (MTMSwapType == MTMSwapType.PayNotionalFixed ? fixedNotional : flow.Notional);
                }
                else
                    FV = flow.Fv;

                if (updateDFRec)
                    DF = discountCurveRec.Pv(1, flow.SettleDate);
                else
                    DF = (flow.Fv == flow.Pv) ? 1.0 : flow.Pv / flow.Fv;

                double PV = DF * FV;

                if (updateState)
                {
                    flow.Fv = FV;
                    flow.Pv = PV;
                }

                totalPVPay += PV;
            }

            return totalPVRec * fxRecToBase + totalPVPay * fxPayToBase;
        }

    }
}
