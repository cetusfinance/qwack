using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Funding
{
    public class ForwardRateAgreement : IFundingInstrument
    {
        public ForwardRateAgreement() { }

        public ForwardRateAgreement(DateTime valDate, string fraCode, double parRate, FloatRateIndex rateIndex, SwapPayReceiveType payRec, FraDiscountingType fraType, string forecastCurve, string discountCurve):base()
        {
            var code = fraCode.ToUpper().Split('X');
            StartDate = valDate.AddPeriod(rateIndex.RollConvention, rateIndex.HolidayCalendars, new Frequency(code[0] + "M"));
            ResetDate = StartDate.AddPeriod(RollType.P, rateIndex.HolidayCalendars, rateIndex.FixingOffset);
            EndDate = new TenorDateRelative(rateIndex.ResetTenor);
            RateIndex = rateIndex;
            ParRate = parRate;
            Basis = rateIndex.DayCountBasis;
            PayRec = payRec;

            FraLeg = new GenericSwapLeg(StartDate, EndDate.Date(StartDate, rateIndex.RollConvention, rateIndex.HolidayCalendars), rateIndex.HolidayCalendars, rateIndex.Currency, rateIndex.ResetTenor, Basis)
            {
                FixedRateOrMargin = (decimal)ParRate
            };
            FlowScheduleFra = FraLeg.GenerateSchedule();

            FraLeg.FixedRateOrMargin = (decimal)ParRate;
            FraLeg.LegType = SwapLegType.Fra;
            FlowScheduleFra.Flows[0].SettleDate = StartDate;
            ForecastCurve = forecastCurve;
            DiscountCurve = discountCurve;

            FraType = fraType;

            PillarDate = FlowScheduleFra.Flows[0].AccrualPeriodEnd;
        }

        public ForwardRateAgreement(DateTime startDate, double parRate, FloatRateIndex rateIndex, SwapPayReceiveType payRec, FraDiscountingType fraType, string forecastCurve, string discountCurve)
        {
            StartDate = startDate;
            ResetDate = StartDate.AddPeriod(RollType.P, rateIndex.HolidayCalendars, rateIndex.FixingOffset);
            EndDate = new TenorDateRelative(rateIndex.ResetTenor);
            RateIndex = rateIndex;
            ParRate = parRate;
            Basis = rateIndex.DayCountBasis;
            PayRec = payRec;

            FraLeg = new GenericSwapLeg(StartDate, EndDate.Date(StartDate, rateIndex.RollConvention, rateIndex.HolidayCalendars), rateIndex.HolidayCalendars, rateIndex.Currency, rateIndex.ResetTenor, Basis)
            {
                FixedRateOrMargin = (decimal)ParRate
            };
            FlowScheduleFra = FraLeg.GenerateSchedule();

            FraLeg.FixedRateOrMargin = (decimal)ParRate;
            FraLeg.LegType = SwapLegType.Fra;
            FlowScheduleFra.Flows[0].SettleDate = StartDate;
            ForecastCurve = forecastCurve;
            DiscountCurve = discountCurve;

            FraType = fraType;
            PillarDate = FlowScheduleFra.Flows[0].AccrualPeriodEnd;
        }

        public double Notional { get; set; }
        public double ParRate { get; set; }
        public DateTime StartDate { get; set; }
        public ITenorDate EndDate { get; set; }
        public DateTime ResetDate { get; set; }
        public Currency Currency { get; set; }
        public GenericSwapLeg FraLeg { get; set; }
        public FloatRateIndex RateIndex { get; set; }
        public CashFlowSchedule FlowScheduleFra { get; set; }
        public DayCountBasis Basis { get; set; }
        public SwapPayReceiveType PayRec { get; set; }
        public string ForecastCurve { get; set; }
        public string DiscountCurve { get; set; }
        public FraDiscountingType FraType { get; set; }
        public string SolveCurve { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public DateTime PillarDate { get; set; }
        public string PortfolioName { get; set; }
        public DateTime LastSensitivityDate => FlowScheduleFra.Flows.Last().AccrualPeriodEnd;

        public List<string> Dependencies(IFxMatrix matrix) => (new[] { DiscountCurve, ForecastCurve }).Distinct().Where(x => x != SolveCurve).ToList();

        public double Pv(IFundingModel model, bool updateState)
        {
            var updateDF = updateState || model.CurrentSolveCurve == DiscountCurve;
            var updateEst = updateState || model.CurrentSolveCurve == ForecastCurve;
            return Pv(model.Curves[DiscountCurve], model.Curves[ForecastCurve], updateState, updateDF, updateEst);
        }

        public double CalculateParRate(IFundingModel model) => FlowScheduleFra.Flows.First().GetFloatRate(model.Curves[ForecastCurve], Basis);

        public double Pv(IrCurve discountCurve, IrCurve forecastCurve, bool updateState, bool updateDF, bool updateEstimate)
        {
            var totalPV = 0.0;

            if (FlowScheduleFra.Flows.Count != 1)
                throw new InvalidOperationException("FRA should have a sinlge flow");

            var flow = FlowScheduleFra.Flows.Single();

            var s = flow.AccrualPeriodStart;
            var e = flow.AccrualPeriodEnd;

            double FV, DF;
            if (updateEstimate)
            {
                var RateFix = flow.FixedRateOrMargin;
                var RateFloat = forecastCurve.GetForwardRate(s, e, RateType.Linear, Basis);
                var YF = flow.NotionalByYearFraction;
                FV = ((RateFloat - RateFix) * YF) / (1 + RateFloat * YF) * flow.Notional;

                FV *= (PayRec == SwapPayReceiveType.Payer) ? 1.0 : -1.0;
            }
            else
                FV = flow.Fv;

            if (updateDF)
                DF = discountCurve.Pv(1.0, flow.SettleDate);
            else
                DF = flow.Pv / flow.Fv;

            totalPV = discountCurve.Pv(FV, flow.SettleDate);

            if (!updateState) return totalPV;
            flow.Fv = FV;
            flow.Pv = totalPV;
            return totalPV;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            //discounting first
            var discountDict = new Dictionary<DateTime, double>();
            var discountCurve = model.Curves[DiscountCurve];
            var flow = FlowScheduleFra.Flows.Single();
            var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.SettleDate);

            discountDict.Add(flow.SettleDate, -t * flow.Pv);


            //then forecast
            var forecastDict = (ForecastCurve == DiscountCurve) ? discountDict : new Dictionary<DateTime, double>();
            var forecastCurve = model.Curves[ForecastCurve];
            var s = flow.AccrualPeriodStart;
            var e = flow.AccrualPeriodEnd;
            var RateFix = flow.FixedRateOrMargin;
            var RateFloat = forecastCurve.GetForwardRate(s, e, RateType.Linear, Basis);

            var df = flow.Fv == flow.Pv ? 1.0 : flow.Pv / flow.Fv;
            var ts = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.AccrualPeriodStart);
            var te = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, flow.AccrualPeriodEnd);

            //https://www.mathsisfun.com/calculus/derivatives-rules.html quotient rule
            var f = (RateFloat - RateFix) * flow.NotionalByYearFraction * flow.Notional * df;
            var g = (1 + RateFloat * flow.NotionalByYearFraction);
            var fd = flow.NotionalByYearFraction * flow.Notional * df;
            var gd = flow.NotionalByYearFraction;

            var dPVdR = (fd * g - gd * f) / (g * g);
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

        public IFundingInstrument Clone() => new ForwardRateAgreement
        {
            Basis = Basis,
            Currency = Currency,
            Counterparty = Counterparty,
            DiscountCurve = DiscountCurve,
            EndDate = EndDate,
            FlowScheduleFra = FlowScheduleFra.Clone(),
            ForecastCurve = ForecastCurve,
            FraLeg = FraLeg.Clone(),
            FraType = FraType,
            Notional = Notional,
            ParRate = ParRate,
            PayRec = PayRec,
            PillarDate = PillarDate,
            ResetDate = ResetDate,
            SolveCurve = SolveCurve,
            StartDate = StartDate,
            TradeId = TradeId
        };

        public IFundingInstrument SetParRate(double parRate) =>
            new ForwardRateAgreement(StartDate, parRate, RateIndex, PayRec, FraType, ForecastCurve, DiscountCurve)
            {
                Notional = Notional,
                SolveCurve = SolveCurve,
                Currency = Currency,
            };


        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => FlowScheduleFra.Flows;

        public double SuggestPillarValue(IFundingModel model) => ParRate;
    }
}
