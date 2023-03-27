using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Models;
using Qwack.Models.Calibrators;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Core.Tests.Inflation
{
    public class InflationCurveFacts
    {
        [Fact]
        public void TestInflationCurve()
        {
            var vd = new DateTime(2023, 03, 01);
            var usd = TestProviderHelper.CurrencyProvider.GetCurrencySafe("USD");
            var usdIrCurve = new ConstantRateIrCurve(0.05, vd, "USD-CURVE", usd)
            {
                SolveStage = -1,
            };

            var infIx = new InflationIndex
            {
                Currency = usd,
                DayCountBasis = DayCountBasis.Act360,
                DayCountBasisFixed = DayCountBasis.Act360,
                FixingInterpolation = Interpolator1DType.Linear,
                FixingLag = 1.Months(),
                ResetFrequency = 1.Years()
            };

            var infSwap1y = new InflationSwap(vd, 1.Years(), infIx, 0.045, Core.Basic.SwapPayReceiveType.Pay, "USD-CPI", "USD-CURVE", "USD-CURVE")
            {
                SolveCurve = "USD-CPI"
            };
            var infSwap2y = new InflationSwap(vd, 2.Years(), infIx, 0.045, Core.Basic.SwapPayReceiveType.Pay, "USD-CPI", "USD-CURVE", "USD-CURVE")
            {
                SolveCurve = "USD-CPI"
            };

            var fic = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider)
            {
                infSwap1y,
                infSwap2y
            };

            var fixings = new Dictionary<DateTime, double>()
            {
                { vd, 120 },
                { vd.AddMonths(-1), 110 },
                { vd.AddMonths(-2), 100 },
            };

            var pillars = new[] { vd.AddYears(1), vd.AddYears(2) };
            var rates = new[] { 120.0, 121.0 };

            var cpiCurve = new CPICurve(vd, pillars, rates, infIx, fixings)
            {
                Name = "USD-CPI",
                SolveStage = 0,
            };


            var model = new FundingModel(vd, new IIrCurve[] { usdIrCurve, cpiCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var S = new NewtonRaphsonMultiCurveSolverStaged
            {
                JacobianBump = 0.01,
                Tollerance =  0.00000001,
                MaxItterations = 100,
            };

            S.Solve(model, fic);

            Assert.DoesNotContain(rates, double.IsNaN);
        }
    }
}
