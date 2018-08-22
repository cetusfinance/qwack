using System;
using System.Collections.Generic;
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

            PayLeg = new GenericSwapLeg(StartDate, swapTenor, payIndex.HolidayCalendars, payIndex.Currency, ResetFrequencyPay, BasisPay)
            {
                FixedRateOrMargin = (decimal)ParSpreadPay,
                NotionalExchange = NotionalExchange,
                Direction = SwapPayReceiveType.Payer,
                LegType = SwapLegType.Float
            };
            RecLeg = new GenericSwapLeg(StartDate, swapTenor, recIndex.HolidayCalendars, recIndex.Currency, ResetFrequencyRec, BasisRec)
            {
                FixedRateOrMargin = (decimal)ParSpreadRec,
                NotionalExchange = NotionalExchange
            };
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
        public DateTime PillarDate { get; set; }

        public double Pv(IFundingModel model, bool updateState)
        {
            var updateDfPay = updateState || model.CurrentSolveCurve == DiscountCurvePay;
            var updateDfRec = updateState || model.CurrentSolveCurve == DiscountCurveRec;
            var updatePayEst = updateState || model.CurrentSolveCurve == ForecastCurvePay;
            var updateRecEst = updateState || model.CurrentSolveCurve == ForecastCurveRec;

            return PV(model, updateState, updateDfPay, updateDfRec, updatePayEst, updateRecEst);
        }

        public CashFlowSchedule ExpectedCashFlows(IFundingModel model) => throw new NotImplementedException();

        public double PV(IFundingModel model, bool updateState, bool updateDfPay, bool updateDfRec, bool updatePayEst, bool updateRecEst)
        {
            var discountCurvePay = model.Curves[DiscountCurvePay];
            var discountCurveRec = model.Curves[DiscountCurveRec];
            var forecastCurvePay = model.Curves[ForecastCurvePay];
            var forecastCurveRec = model.Curves[ForecastCurveRec];
            double totalPVRec = 0;
            double totalPVPay = 0;


            var payCCY = MtmSwapType == MTMSwapType.ReceiveNotionalFixed ? CcyRec : CcyPay;
            var recCCY = MtmSwapType == MTMSwapType.PayNotionalFixed ? CcyPay : CcyRec;
            var baseCCY = Pvccy ?? payCCY;

            var fxPayToBase = model.GetFxRate(model.BuildDate, payCCY, baseCCY);
            var fxRecToBase = model.GetFxRate(model.BuildDate, recCCY, baseCCY);

            var fixedNotional = (double)(MtmSwapType == MTMSwapType.PayNotionalFixed ? PayLeg.Nominal :
                MtmSwapType == MTMSwapType.ReceiveNotionalFixed ? RecLeg.Nominal : 0M);

            for (var i = 0; i < FlowSchedulePay.Flows.Count; i++)
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

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            //discounting first
            var discountDictPay = new Dictionary<DateTime, double>();
            var discountCurvePay = model.Curves[DiscountCurvePay];
            foreach (var flow in FlowSchedulePay.Flows)
            {
                var t = discountCurvePay.Basis.CalculateYearFraction(discountCurvePay.BuildDate, flow.SettleDate);
                if (discountDictPay.ContainsKey(flow.SettleDate))
                    discountDictPay[flow.SettleDate] += -t * flow.Pv;
                else
                    discountDictPay.Add(flow.SettleDate, -t * flow.Pv);
            }
            var discountDictRec = new Dictionary<DateTime, double>();
            var discountCurveRec = model.Curves[DiscountCurveRec];
            foreach (var flow in FlowScheduleRec.Flows)
            {
                var t = discountCurveRec.Basis.CalculateYearFraction(discountCurveRec.BuildDate, flow.SettleDate);
                if (discountDictRec.ContainsKey(flow.SettleDate))
                    discountDictRec[flow.SettleDate] += -t * flow.Pv;
                else
                    discountDictRec.Add(flow.SettleDate, -t * flow.Pv);
            }


            //then forecast
            var forecastDictPay = (ForecastCurvePay == DiscountCurvePay) ? discountDictPay : new Dictionary<DateTime, double>();
            var forecastDictRec = (ForecastCurveRec == DiscountCurveRec) ? discountDictRec : new Dictionary<DateTime, double>();
            var forecastCurvePay = model.Curves[ForecastCurvePay];
            var forecastCurveRec = model.Curves[ForecastCurveRec];
            foreach (var flow in FlowSchedulePay.Flows)
            {
                var df = flow.Fv == flow.Pv ? 1.0 : flow.Pv / flow.Fv;
                var ts = discountCurvePay.Basis.CalculateYearFraction(discountCurvePay.BuildDate, flow.AccrualPeriodStart);
                var te = discountCurvePay.Basis.CalculateYearFraction(discountCurvePay.BuildDate, flow.AccrualPeriodEnd);
                var dPVdR = df * flow.NotionalByYearFraction * flow.Notional;
                var RateFloat = flow.Fv / (flow.Notional * flow.NotionalByYearFraction);
                var dPVdS = dPVdR * (-ts * (RateFloat + 1.0 / flow.NotionalByYearFraction));
                var dPVdE = dPVdR * (te * (RateFloat + 1.0 / flow.NotionalByYearFraction));

                if (forecastDictPay.ContainsKey(flow.AccrualPeriodStart))
                    forecastDictPay[flow.AccrualPeriodStart] += dPVdS;
                else
                    forecastDictPay.Add(flow.AccrualPeriodStart, dPVdS);

                if (forecastDictPay.ContainsKey(flow.AccrualPeriodEnd))
                    forecastDictPay[flow.AccrualPeriodEnd] += dPVdE;
                else
                    forecastDictPay.Add(flow.AccrualPeriodEnd, dPVdE);
            }
            foreach (var flow in FlowScheduleRec.Flows)
            {
                var df = flow.Fv == flow.Pv ? 1.0 : flow.Pv / flow.Fv;
                var ts = discountCurveRec.Basis.CalculateYearFraction(discountCurveRec.BuildDate, flow.AccrualPeriodStart);
                var te = discountCurveRec.Basis.CalculateYearFraction(discountCurveRec.BuildDate, flow.AccrualPeriodEnd);
                var dPVdR = df * flow.NotionalByYearFraction * flow.Notional;
                var RateFloat = flow.Fv / (flow.Notional * flow.NotionalByYearFraction);
                var dPVdS = dPVdR * (-ts * (RateFloat + 1.0 / flow.NotionalByYearFraction));
                var dPVdE = dPVdR * (te * (RateFloat + 1.0 / flow.NotionalByYearFraction));

                if (forecastDictRec.ContainsKey(flow.AccrualPeriodStart))
                    forecastDictRec[flow.AccrualPeriodStart] += dPVdS;
                else
                    forecastDictRec.Add(flow.AccrualPeriodStart, dPVdS);

                if (forecastDictRec.ContainsKey(flow.AccrualPeriodEnd))
                    forecastDictRec[flow.AccrualPeriodEnd] += dPVdE;
                else
                    forecastDictRec.Add(flow.AccrualPeriodEnd, dPVdE);
            }


            if (ForecastCurvePay == DiscountCurvePay && ForecastCurveRec == DiscountCurveRec)
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurvePay,discountDictPay },
                {DiscountCurveRec,discountDictRec },
            };
            else if (ForecastCurvePay == DiscountCurvePay)
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurvePay,discountDictPay },
                {DiscountCurveRec,discountDictRec },
                {ForecastCurveRec,forecastDictRec },
            };
            else if (ForecastCurveRec == DiscountCurveRec)
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurvePay,discountDictPay },
                {DiscountCurveRec,discountDictRec },
                {ForecastCurvePay,forecastDictPay },
            };
            else
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurvePay,discountDictPay },
                {DiscountCurveRec,discountDictRec },
                {ForecastCurvePay,forecastDictPay },
                {ForecastCurveRec,forecastDictRec },
            };
        }
    }
}
