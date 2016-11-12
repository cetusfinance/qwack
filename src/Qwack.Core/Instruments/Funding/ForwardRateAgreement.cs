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
    public class ForwardRateAgreement : IFundingInstrument
    {
        public ForwardRateAgreement(DateTime valDate, string FRACode, double parRate, FloatRateIndex rateIndex, SwapPayReceiveType payRec, FraDiscountingType fraType, string forecastCurve, string discountCurve)
        {
            string[] code = FRACode.ToUpper().Split('X');
            StartDate = valDate.AddPeriod(rateIndex.RollConvention, rateIndex.HolidayCalendars, new Frequency(code[0] + "M"));
            ResetDate = StartDate.AddPeriod(RollType.P, rateIndex.HolidayCalendars, rateIndex.FixingOffset);
            EndDate = new TenorDateRelative(rateIndex.ResetTenor);

            ParRate = parRate;
            Basis = rateIndex.DayCountBasis;
            PayRec = payRec;

            FRALeg = new GenericSwapLeg(StartDate, EndDate.Date(StartDate, rateIndex.RollConvention, rateIndex.HolidayCalendars), rateIndex.HolidayCalendars, rateIndex.Currency, rateIndex.ResetTenor, Basis);
            FRALeg.FixedRateOrMargin = (decimal)ParRate;
            FlowScheduleFRA = FRALeg.GenerateSchedule();

            FRALeg.FixedRateOrMargin = (decimal)ParRate;
            FRALeg.LegType = SwapLegType.Fra;
            FlowScheduleFRA.Flows[0].SettleDate = StartDate;
            ForecastCurve = forecastCurve;
            DiscountCurve = discountCurve;

            FRAType = fraType;
        }

        public double Notional { get; set; }
        public double ParRate { get; set; }
        public DateTime StartDate { get; set; }
        public ITenorDate EndDate { get; set; }
        public DateTime ResetDate { get; set; }
        public Currency CCY { get; set; }
        public GenericSwapLeg FRALeg { get; set; }
        public CashFlowSchedule FlowScheduleFRA { get; set; }
        public DayCountBasis Basis { get; set; }
        public SwapPayReceiveType PayRec { get; set; }
        public string ForecastCurve { get; set; }
        public string DiscountCurve { get; set; }
        public FraDiscountingType FRAType { get; set; }
        public string SolveCurve { get; set; }

        public double Pv(FundingModel model, bool updateState)
        {
            bool updateDF = updateState || model.CurrentSolveCurve == DiscountCurve;
            bool updateEst = updateState || model.CurrentSolveCurve == ForecastCurve;
            return Pv(model.Curves[DiscountCurve], model.Curves[ForecastCurve], updateState, updateDF, updateEst);
        }

        public CashFlowSchedule ExpectedCashFlows(FundingModel model)
        {
            throw new NotImplementedException();
        }

        public double Pv(IrCurve discountCurve, IrCurve forecastCurve, bool updateState, bool updateDF, bool updateEstimate)
        {
            double totalPV = 0;

            if (FlowScheduleFRA.Flows.Count != 1)
                throw new NotImplementedException("FRA should have a sinlge flow");

            var flow = FlowScheduleFRA.Flows.Single();

            DateTime s = flow.AccrualPeriodStart;
            DateTime e = flow.AccrualPeriodEnd;

            double FV, DF;
            if (updateEstimate)
            {
                double RateFix = flow.FixedRateOrMargin;
                double RateFloat = forecastCurve.GetForwardRate(s, e, RateType.Linear, Basis);
                double YF = flow.DiscountFactor;
                FV = ((RateFloat - RateFix) * YF) / (1 + RateFloat * YF);

                FV *= (PayRec == SwapPayReceiveType.Payer) ? 1.0 : -1.0;
            }
            else
                FV = flow.Fv;

            if (updateDF)
                DF = discountCurve.Pv(1.0, flow.SettleDate);
            else
                DF = (flow.Fv == flow.Pv) ? 1.0 : flow.Pv / flow.Fv;

            totalPV = discountCurve.Pv(FV, flow.SettleDate);

            if (updateState)
            {
                flow.Fv = FV;
                flow.Pv = totalPV;
            }
            return totalPV;
        }
    }
}
