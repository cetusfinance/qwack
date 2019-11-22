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
    public class FloatingRateLoanDepo : IFundingInstrument, IAssetInstrument
    {
        public FloatingRateLoanDepo() { }

        public FloatingRateLoanDepo(CashFlow[] flows, FloatRateIndex floatRateIndex, string forecastCurve, string discountCurve):base()
        {
            FloatRateIndex = floatRateIndex;
            ForecastCurve = forecastCurve;
            DiscountCurve = discountCurve;
            LoanDepoSchedule = new CashFlowSchedule { Flows = flows.ToList() };         
        }

        public FloatingRateLoanDepo(DateTime startDate, Frequency tenor, FloatRateIndex floatRateIndex, double notional, double spread, string forecastCurve, string discountCurve)
        {
            var leg = new GenericSwapLeg(startDate, tenor, floatRateIndex.HolidayCalendars, floatRateIndex.Currency, floatRateIndex.ResetTenor, floatRateIndex.DayCountBasis)
            {
                Nominal = Convert.ToDecimal(notional),
                Currency = floatRateIndex.Currency,
                Direction = SwapPayReceiveType.Pay,
                NotionalExchange = ExchangeType.Both,
                LegType = SwapLegType.Float
            };
            var schedule = leg.GenerateSchedule();

            FloatRateIndex = floatRateIndex;
            ForecastCurve = forecastCurve;
            DiscountCurve = discountCurve;
            LoanDepoSchedule = schedule;
        }

        public double Notional { get; set; }
        public string PortfolioName { get; set; }
        public CashFlowSchedule LoanDepoSchedule { get; set; }
        public FloatRateIndex FloatRateIndex { get; }
        public string ForecastCurve { get; }
        public string DiscountCurve { get; set; }

        public string SolveCurve { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public DateTime PillarDate { get; set; }

        public DateTime LastSensitivityDate => LoanDepoSchedule.Flows.Max(x=>x.SettleDate);

        public string[] AssetIds => new string[0];
        public Currency PaymentCurrency => FloatRateIndex.Currency;
        public Currency Currency => FloatRateIndex.Currency;

        public double Pv(IFundingModel Model, bool updateState) => Pv(Model, updateState, false);
        public double Pv(IFundingModel model, bool updateState, bool ignoreTodayFlows) => 
            LoanDepoSchedule.PV(model.Curves[DiscountCurve], model.Curves[ForecastCurve], updateState, true, true, FloatRateIndex.DayCountBasis, ignoreTodayFlows ? model.BuildDate.AddDays(1) : model.BuildDate);

        public double FlowsT0(IFundingModel model)
        {
            var todayFlows = LoanDepoSchedule.Flows.Where(x => x.SettleDate == model.BuildDate);
            return todayFlows.Any() ? todayFlows.Sum(x => x.Fv) : 0.0;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model) => throw new NotImplementedException();


        public List<string> Dependencies(IFxMatrix matrix) => new List<string>();

        public double CalculateParRate(IFundingModel model)
        {
            //var dFs = LoanDepoSchedule
            //    .Flows
            //    .Select(x => x.SettleDate)
            //    .Select(y => model.Curves[DiscountCurve]
            //    .GetDf(model.BuildDate, y))
            //    .ToArray();
            //var floatRates = LoanDepoSchedule
            //    .Flows
            //    .Select(x => x.GetFloatRate(model.Curves[ForecastCurve], FloatRateIndex.DayCountBasis))
            //    .ToArray();
            //var dcfs = LoanDepoSchedule
            //    .Flows
            //    .Select(x => x.NotionalByYearFraction)
            //    .ToArray();
            //var nominals = LoanDepoSchedule
            //    .Flows
            //    .Select(x => x.Notional)
            //    .ToArray();

            //var sumTop = 0.0;
            //var sumBottom = 0.0;
            //for(var i=0;i<LoanDepoSchedule.Flows.Count;i++)
            //{
            //    var rowA = System.Math.Sign(nominals[i]) * dFs[i];
            //    var rowB = floatRates[i] * dcfs[i];
            //    if (LoanDepoSchedule.Flows[i].FlowType == FlowType.FloatRate)
            //        rowA *= rowB;
            //    sumTop += rowA;
            //    sumBottom += rowB;
            //}

            //var parRate = sumTop / sumBottom;
            //return parRate;

            var targetFunc = new Func<double, double>(spd=>
            {
                return SetParRate(spd).Pv(model, true);
            });

            var par = Math.Solvers.Brent.BrentsMethodSolve(targetFunc, -0.1, 0.5, 0.0001);
            return par;
        }

        public IFundingInstrument Clone() => new FloatingRateLoanDepo(LoanDepoSchedule.Flows.ToArray(), FloatRateIndex, ForecastCurve, DiscountCurve);

        public IFundingInstrument SetParRate(double parRate)
        {
            var flowsNew = LoanDepoSchedule.Flows.ToArray();
            foreach(var flow in flowsNew.Where(x=>x.FlowType==FlowType.FloatRate))
            {
                flow.FixedRateOrMargin = parRate;
            }
            return new FloatingRateLoanDepo(flowsNew, FloatRateIndex, ForecastCurve, DiscountCurve)
            {
                PillarDate = PillarDate,
                SolveCurve = SolveCurve,
                PortfolioName = PortfolioName,
                TradeId = TradeId
            };
        }

        public string[] IrCurves(IAssetFxModel model) => new[] { DiscountCurve, ForecastCurve };

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => new Dictionary<string, List<DateTime>>();

        public FxConversionType FxType(IAssetFxModel model) => FxConversionType.None;

        public string FxPair(IAssetFxModel model) => string.Empty;

        IAssetInstrument IAssetInstrument.Clone() => new FloatingRateLoanDepo(LoanDepoSchedule.Flows.ToArray(), FloatRateIndex, ForecastCurve, DiscountCurve)
        {
            PillarDate = PillarDate,
            SolveCurve = SolveCurve,
            PortfolioName = PortfolioName,
            TradeId = TradeId
        };

        public IAssetInstrument SetStrike(double strike)=> throw new NotImplementedException();

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model)
        {
            Pv(model.FundingModel, true);
            return LoanDepoSchedule.Flows;
        }
    }
}
