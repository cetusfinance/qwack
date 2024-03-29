using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Models;
using Qwack.Models.Calibrators;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Curves;
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
            var usdIrCurve = new ConstantRateIrCurve(0.05, vd, "USD.CURVE", usd)
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

            var infSwap1y = new InflationSwap(vd, 1.Years(), infIx, 0.045, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };
            var infSwap2y = new InflationSwap(vd, 2.Years(), infIx, 0.045, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };

            var fic = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider)
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

            var pillars = new[] { vd.AddYears(1).FirstDayOfMonth(), vd.AddYears(2).FirstDayOfMonth() };
            var rates = new[] { 120.0, 121.0 };

            var cpiCurve = new CPICurve(vd, pillars, rates, infIx)
            {
                Name = "USD.CPI",
                SolveStage = 0,
            };

            var model = new FundingModel(vd, new IIrCurve[] { usdIrCurve, cpiCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            model.AddFixingDictionary("USD.CPI", new FixingDictionary(fixings));

            var S = new NewtonRaphsonMultiCurveSolverStaged
            {
                JacobianBump = 0.01,
                Tollerance = 0.00000001,
                MaxItterations = 100,
            };

            S.Solve(model, fic);

            Assert.DoesNotContain(rates, double.IsNaN);
        }

        [Fact]
        public void TestInflationCurve2()
        {
            var vd = new DateTime(2023, 03, 01);
            var usd = TestProviderHelper.CurrencyProvider.GetCurrencySafe("USD");
            var usdIrCurve = new ConstantRateIrCurve(0.05, vd, "USD.CURVE", usd)
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

            var infSwap6m = new InflationPerformanceSwap(vd, 6.Months(), infIx, 0.045, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI",
            };
            var infSwap1y = new InflationPerformanceSwap(vd, 1.Years(), infIx, 0.05, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };
            var infSwap2y = new InflationPerformanceSwap(vd, 2.Years(), infIx, 0.055, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };
            var infSwap3y = new InflationPerformanceSwap(vd, 3.Years(), infIx, 0.06, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };
            var infSwap5y = new InflationPerformanceSwap(vd, 5.Years(), infIx, 0.08, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };


            var fic = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider)
            {
                infSwap6m,
                infSwap1y,
                infSwap2y,
                infSwap3y,
                infSwap5y,
            };

            var curves = fic.ImplyContainedCurves(vd, Interpolator1DType.Linear);
            curves.Add("USD.CURVE",usdIrCurve);

            var fixings = new Dictionary<DateTime, double>()
            {
                { vd.AddMonths(-1), 110 },
                { vd.AddMonths(-2), 100 },
            };

            var model = new FundingModel(vd, curves.Values.ToArray(), TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            model.AddFixingDictionary("USD.CPI", new FixingDictionary(fixings));

            var S = new NewtonRaphsonMultiCurveSolverStaged
            {
                JacobianBump = 0.01,
                Tollerance = 0.00000001,
                MaxItterations = 100,
            };

            S.Solve(model, fic);

            var cpiCurve = curves["USD.CPI"] as CPICurve;
            Assert.DoesNotContain(cpiCurve.CpiRates, double.IsNaN);
            foreach(var i in fic)
            {
                Assert.Equal(0, i.Pv(model, false), 8);
            }
        }

        [Fact]
        public void TestInflationCurve2CC()
        {
            var vd = new DateTime(2023, 03, 01);
            var usd = TestProviderHelper.CurrencyProvider.GetCurrencySafe("USD");
            var usdIrCurve = new ConstantRateIrCurve(0.05, vd, "USD.CURVE", usd)
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

            var infSwap6m = new InflationPerformanceSwap(vd, 6.Months(), infIx, 0.045, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI",
            };
            var infSwap1y = new InflationPerformanceSwap(vd, 1.Years(), infIx, 0.05, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };
            var infSwap2y = new InflationPerformanceSwap(vd, 2.Years(), infIx, 0.055, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };
            var infSwap3y = new InflationPerformanceSwap(vd, 3.Years(), infIx, 0.06, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };
            var infSwap5y = new InflationPerformanceSwap(vd, 5.Years(), infIx, 0.08, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };


            var fic = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider)
            {
                infSwap6m,
                infSwap1y,
                infSwap2y,
                infSwap3y,
                infSwap5y,
            };

            var curves = fic.ImplyContainedCurves(vd, Interpolator1DType.Linear);
            curves.Add("USD.CURVE", usdIrCurve);
            var cpiCurve = curves["USD.CPI"] as CPICurve;
            cpiCurve.CpiInterpolationType = CpiInterpolationType.CC;
            cpiCurve.SpotFixing = 100;
            curves["USD.CPI"] = cpiCurve.Clone(); //force interpolator rebuild

            var fixings = new Dictionary<DateTime, double>()
            {
                { vd.AddMonths(-1), 110 },
                { vd.AddMonths(-2), 100 },
            };

            var model = new FundingModel(vd, curves.Values.ToArray(), TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            model.AddFixingDictionary("USD.CPI", new FixingDictionary(fixings));

            var S = new NewtonRaphsonMultiCurveSolverStaged
            {
                JacobianBump = 0.01,
                Tollerance = 0.00000001,
                MaxItterations = 100,
            };

            S.Solve(model, fic);

            cpiCurve = curves["USD.CPI"] as CPICurve;
            Assert.DoesNotContain(cpiCurve.CpiRates, double.IsNaN);
            foreach (var i in fic)
            {
                Assert.Equal(0, i.Pv(model, false), 8);
            }
        }

        [Fact]
        public void TestInflationCurve2CCSeasonal()
        {
            var vd = new DateTime(2023, 03, 01);
            var usd = TestProviderHelper.CurrencyProvider.GetCurrencySafe("USD");
            var usdIrCurve = new ConstantRateIrCurve(0.05, vd, "USD.CURVE", usd)
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

            var infSwap6m = new InflationPerformanceSwap(vd, 6.Months(), infIx, 0.045, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI",
            };
            var infSwap1y = new InflationPerformanceSwap(vd, 1.Years(), infIx, 0.05, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };
            var infSwap2y = new InflationPerformanceSwap(vd, 2.Years(), infIx, 0.055, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };
            var infSwap3y = new InflationPerformanceSwap(vd, 3.Years(), infIx, 0.06, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };
            var infSwap5y = new InflationPerformanceSwap(vd, 5.Years(), infIx, 0.08, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI"
            };


            var fic = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider)
            {
                infSwap6m,
                infSwap1y,
                infSwap2y,
                infSwap3y,
                infSwap5y,
            };

            var spotCpiLevels = new Dictionary<string, double> { { "USD.CPI", 100 } };
            var seasonality = new Dictionary<string, double[]> { { "USD.CPI", new[] { 0.002085,0.002227,0.002227,0.000286,0.001534,0.003166,-0.000895,-0.002116,-0.001549,0.000169,-0.00331,-0.003823}  } };

            var fixings = new Dictionary<DateTime, double>()
            {
                { vd.AddMonths(-1), 110 },
                { vd.AddMonths(-2), 100 },
            };
            var fixingsByIx = new Dictionary<string, Dictionary<DateTime, double>>()
            {
                { "USD.CPI", fixings }
            };

            var curves = fic.ImplyContainedCurves(vd, Interpolator1DType.Linear, spotCpiLevels: spotCpiLevels, cpiSeasonalalFactors: seasonality, fixings: fixingsByIx);
            curves.Add("USD.CURVE", usdIrCurve);
            
         
            var model = new FundingModel(vd, curves.Values.ToArray(), TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            model.AddFixingDictionary("USD.CPI", new FixingDictionary(fixings));

            var S = new NewtonRaphsonMultiCurveSolverStaged
            {
                JacobianBump = 0.01,
                Tollerance = 0.00000001,
                MaxItterations = 100,
            };

            S.Solve(model, fic);

            var cpiCurve = curves["USD.CPI"] as SeasonalCpiCurve;
            Assert.DoesNotContain(cpiCurve.CpiRates, double.IsNaN);
            foreach (var i in fic)
            {
                Assert.Equal(0, i.Pv(model, false), 8);
            }
        }


        [Fact]
        public void TestInflationPerfSwap()
        {
            var vd = new DateTime(2023, 02, 01);
            var usd = TestProviderHelper.CurrencyProvider.GetCurrencySafe("USD");
            var usdIrCurve = new ConstantRateIrCurve(0.05, vd, "USD.CURVE", usd)
            {
                SolveStage = -1,
            };

            var infIx = new InflationIndex
            {
                Currency = usd,
                DayCountBasis = DayCountBasis.Act_Act,
                DayCountBasisFixed = DayCountBasis.Act_Act,
                FixingInterpolation = Interpolator1DType.Linear,
                FixingLag = 1.Months(),
                ResetFrequency = 1.Years()
            };

            var pillars = new DateTime[] { vd.FirstDayOfMonth(), vd.AddYears(1).AddMonths(-1).FirstDayOfMonth() };
            var cpiRates = new double[] { 100, 150 };
            var cpiCurve = new CPICurve(vd, pillars, cpiRates, infIx) { Name = "USD.CPI" };
            var fixingDict = new Dictionary<DateTime, double> { { vd.AddMonths(-1), 100.0 } };

            var fModel = new FundingModel(vd, new IIrCurve[] { usdIrCurve, cpiCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            fModel.AddFixingDictionary("USD.CPI", new FixingDictionary(fixingDict));
            var infSwap1y = new InflationPerformanceSwap(vd, 1.Years(), infIx, 0.045, 1e6, SwapPayReceiveType.Pay, "USD.CPI", "USD.CURVE")
            {
                SolveCurve = "USD.CPI",
            };

            Assert.Equal(0.5, infSwap1y.CalculateParRate(fModel), 2);
            Assert.NotEqual(0, infSwap1y.Pv(fModel, false), 3);
            infSwap1y = infSwap1y.SetParRate(infSwap1y.CalculateParRate(fModel)) as InflationPerformanceSwap;
            Assert.Equal(0, infSwap1y.Pv(fModel, false), 8);
        }


        [Fact]
        public void TestInfInterpolation()
        {
            var indexA = 100;
            var indexB = 110;
            var fDate = new DateTime(2023, 6, 15);

            var sut = InflationUtils.InterpFixing(fDate, indexA, indexB);

            Assert.Equal(105, sut, 8);
        }
    }
}
