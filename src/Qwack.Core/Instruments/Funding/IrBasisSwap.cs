using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class IrBasisSwap : IFundingInstrument
    {
        public IrBasisSwap(DateTime startDate, Frequency swapTenor, double parSpread, bool spreadOnPayLeg, FloatRateIndex payIndex, FloatRateIndex recIndex, string forecastCurvePay, string forecastCurveRec, string discountCurve, decimal? notional = null)
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

            PayLeg = new GenericSwapLeg(StartDate, swapTenor, payIndex.HolidayCalendars, payIndex.Currency, ResetFrequencyPay, BasisPay)
            {
                FixedRateOrMargin = (decimal)ParSpreadPay,
                LegType = SwapLegType.Float,
                Nominal = -(notional ?? 1e6M),
                AccrualDCB = payIndex.DayCountBasis
            };

            RecLeg = new GenericSwapLeg(StartDate, swapTenor, recIndex.HolidayCalendars, recIndex.Currency, ResetFrequencyRec, BasisRec)
            {
                FixedRateOrMargin = (decimal)ParSpreadRec,
                LegType = SwapLegType.Float,
                Nominal = notional ?? 1e6M,
                AccrualDCB = recIndex.DayCountBasis
            };


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
        public Currency Ccy { get; set; }
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
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public DateTime PillarDate { get; set; }

        public DateTime LastSensitivityDate => EndDate;

        public List<string> Dependencies(IFxMatrix matrix)
        {
            var curves = new[] { ForecastCurvePay, ForecastCurveRec, DiscountCurve};
            return curves.Distinct().Where(x => x != SolveCurve).ToList();
        }

        public double Pv(IFundingModel model, bool updateState)
        {
            var updateDF = updateState || model.CurrentSolveCurve == DiscountCurve;
            var updatePayEst = updateState || model.CurrentSolveCurve == ForecastCurvePay;
            var updateRecEst = updateState || model.CurrentSolveCurve == ForecastCurveRec;

            var discountCurve = model.Curves[DiscountCurve];
            var forecastCurvePay = model.Curves[ForecastCurvePay];
            var forecastCurveRec = model.Curves[ForecastCurveRec];
            var payPV = FlowSchedulePay.PV(discountCurve, forecastCurvePay, updateState, updateDF, updatePayEst, BasisPay, null);
            var recPV = FlowScheduleRec.PV(discountCurve, forecastCurveRec, updateState, updateDF, updateRecEst, BasisRec, null);

            return payPV + recPV;
        }

        public CashFlowSchedule ExpectedCashFlows(IFundingModel model) => throw new NotImplementedException();

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            //discounting first
            var discountDict = new Dictionary<DateTime, double>();
            var discountCurve = model.Curves[DiscountCurve];
            foreach (var flow in FlowSchedulePay.Flows.Union(FlowScheduleRec.Flows))
            {
                var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.SettleDate);
                if (discountDict.ContainsKey(flow.SettleDate))
                    discountDict[flow.SettleDate] += -t * flow.Pv;
                else
                    discountDict.Add(flow.SettleDate, -t * flow.Pv);
            }


            //then forecast
            var forecastDictPay = (ForecastCurvePay == DiscountCurve) ? discountDict : new Dictionary<DateTime, double>();
            var forecastDictRec = (ForecastCurveRec == DiscountCurve) ? discountDict : new Dictionary<DateTime, double>();
            var forecastCurvePay = model.Curves[ForecastCurvePay];
            var forecastCurveRec = model.Curves[ForecastCurveRec];
            foreach (var flow in FlowSchedulePay.Flows)
            {
                var df = flow.Fv == flow.Pv ? 1.0 : flow.Pv / flow.Fv;
                var ts = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.AccrualPeriodStart);
                var te = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.AccrualPeriodEnd);
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
                var ts = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.AccrualPeriodStart);
                var te = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.AccrualPeriodEnd);
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


            if (ForecastCurvePay == DiscountCurve && ForecastCurveRec == DiscountCurve)
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurve,discountDict },
            };
            else if (ForecastCurvePay == DiscountCurve)
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurve,discountDict },
                {ForecastCurveRec,forecastDictRec },
            };
            else if (ForecastCurveRec == DiscountCurve)
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurve,discountDict },
                {ForecastCurvePay,forecastDictPay },
            };
            else
                return new Dictionary<string, Dictionary<DateTime, double>>()
            {
                {DiscountCurve,discountDict },
                {ForecastCurvePay,forecastDictPay },
                {ForecastCurveRec,forecastDictRec },
            };
        }
    }
}
