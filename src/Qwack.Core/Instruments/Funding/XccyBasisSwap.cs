using System;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class XccyBasisSwap : IFundingInstrument
    {
        public XccyBasisSwap(DateTime startDate, Frequency swapTenor, double parSpread, bool spreadOnPayLeg, FloatRateIndex payIndex, FloatRateIndex recIndex, ExchangeType notionalExchange, MTMSwapType mtmSwapType, string forecastCurvePay, string forecastCurveRec, string discountCurvePay, string discountCurveRec)
        {
            SwapTenor = swapTenor;
            NotionalExchange = notionalExchange;
            MtmSwapType = mtmSwapType;

            ResetFrequencyRec = recIndex.ResetTenor;
            ResetFrequencyPay = payIndex.ResetTenor;

            StartDate = startDate;
            EndDate = StartDate.AddPeriod(payIndex.RollConvention, payIndex.HolidayCalendars, SwapTenor);

            ParSpreadPay = spreadOnPayLeg ? parSpread : 0.0;
            ParSpreadRec = spreadOnPayLeg ? 0.0 : parSpread;
            BasisPay = payIndex.DayCountBasis;
            BasisRec = recIndex.DayCountBasis;

            CcyPay = payIndex.Currency;
            CcyRec = recIndex.Currency;

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

        public double NotionalPay { get; set; }
        public double NotionalRec { get; set; }
        public double ParSpreadPay { get; set; }
        public double ParSpreadRec { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NDates { get; set; }
        public DateTime[] ResetDates { get; set; }
        public Currency CcyPay { get; set; }
        public Currency CcyRec { get; set; }
        public Currency Pvccy { get; set; }
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

        public MTMSwapType MtmSwapType { get; set; }
        public ExchangeType NotionalExchange { get; set; }
        public string SolveCurve { get; set; }

        public double Pv(FundingModel model, bool updateState)
        {
            var updateDfPay = updateState || model.CurrentSolveCurve == DiscountCurvePay;
            var updateDfRec = updateState || model.CurrentSolveCurve == DiscountCurveRec;
            var updatePayEst = updateState || model.CurrentSolveCurve == ForecastCurvePay;
            var updateRecEst = updateState || model.CurrentSolveCurve == ForecastCurveRec;

            return PV(model, updateState, updateDfPay, updateDfRec, updatePayEst, updateRecEst);
        }

        public CashFlowSchedule ExpectedCashFlows(FundingModel model)
        {
            throw new NotImplementedException();
        }
        public double PV(FundingModel model, bool updateState, bool updateDfPay, bool updateDfRec, bool updatePayEst, bool updateRecEst)
        {
            var discountCurvePay = model.Curves[DiscountCurvePay];
            var discountCurveRec = model.Curves[DiscountCurveRec];
            var forecastCurvePay = model.Curves[ForecastCurvePay];
            var forecastCurveRec = model.Curves[ForecastCurveRec];
            double totalPVRec = 0;
            double totalPVPay = 0;


            var payCCY = MtmSwapType == MTMSwapType.ReceiveNotionalFixed ? CcyRec : CcyPay;
            var recCCY = MtmSwapType == MTMSwapType.PayNotionalFixed ? CcyPay : CcyRec;
            Currency baseCCY = Pvccy ?? payCCY;

            double fxPayToBase = model.GetFxRate(model.BuildDate, payCCY, baseCCY);
            double fxRecToBase = model.GetFxRate(model.BuildDate, recCCY, baseCCY);

            double fixedNotional = (double)(MtmSwapType == MTMSwapType.PayNotionalFixed ? PayLeg.Nominal :
                MtmSwapType == MTMSwapType.ReceiveNotionalFixed ? RecLeg.Nominal : 0M);

            for (int i = 0; i < FlowSchedulePay.Flows.Count; i++)
            {
                double fv, df;

                var flow = FlowSchedulePay.Flows[i];

                if (updatePayEst && flow.FlowType != FlowType.FixedAmount)
                {
                    var s = flow.AccrualPeriodStart;
                    var e = flow.AccrualPeriodEnd;
                    var rateLin = forecastCurvePay.GetForwardRate(s, e, RateType.Linear, BasisPay)
                                  + flow.FixedRateOrMargin;
                    var YF = flow.NotionalByYearFraction;
                    fv = rateLin * YF *
                         (MtmSwapType == MTMSwapType.ReceiveNotionalFixed ? fixedNotional : flow.Notional);
                    fv *= -1.0;
                }
                else
                {
                    fv = flow.Fv;
                }

                if (updateDfPay)
                {
                    df = discountCurvePay.Pv(1, flow.SettleDate);
                }
                else
                {
                    df = flow.Pv / flow.Fv;
                }

                var pv = df * fv;

                if (updateState)
                {
                    flow.Fv = fv;
                    flow.Pv = pv;
                }

                totalPVPay += pv;
            }

            for (var i = 0; i < FlowScheduleRec.Flows.Count; i++)
            {
                double FV, DF;

                var flow = FlowScheduleRec.Flows[i];

                if (updateRecEst && flow.FlowType != FlowType.FixedAmount)
                {
                    var s = flow.AccrualPeriodStart;
                    var e = flow.AccrualPeriodEnd;
                    var rateLin = forecastCurveRec.GetForwardRate(s, e, RateType.Linear, BasisRec)
                                     + flow.FixedRateOrMargin;
                    var YF = flow.NotionalByYearFraction;
                    FV = rateLin * YF * (MtmSwapType == MTMSwapType.PayNotionalFixed ? fixedNotional : flow.Notional);
                }
                else
                {
                    FV = flow.Fv;
                }

                if (updateDfRec)
                {
                    DF = discountCurveRec.Pv(1, flow.SettleDate);
                }
                else
                {
                    DF = flow.Pv / flow.Fv;
                }

                var PV = DF * FV;

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
