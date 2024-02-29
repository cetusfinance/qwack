using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Xunit;
using Qwack.Transport.BasicTypes;
using Qwack.Core.Curves;
using Qwack.Core.Basic;
using Qwack.Dates;
using System.Linq;
using Qwack.Models.Risk;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Cubes;

namespace Qwack.Models.Tests.Risk
{

    [CollectionDefinition("AssetBenchmarkRiskFacts", DisableParallelization = true)]
    public class AssetBenchmarkRiskFacts
    {
        DateTime _originDate = DateTime.Parse("2023-11-17");

        Dictionary<string, double> _qsCurve = new()
        {
                { "2023-12-12", 781.763},
                { "2024-01-11", 772.159},
                { "2024-02-12", 766.417},
                { "2024-03-12", 759.3},
                { "2024-04-11", 752.841},
                { "2024-05-10", 748.141},
                { "2024-06-12", 745.538},
        };

        Dictionary<string, double> _coCurve = new()
        {
                { "2023-11-30", 77.42},
                { "2023-12-28", 77.51},
                { "2024-01-31", 77.5},
                { "2024-02-29", 77.44},
                { "2024-03-28", 77.35},
                { "2024-04-30", 77.2},
                { "2024-05-31", 77},
        };

        private IAssetFxModel GenerateTestData()
        {
            Utils.Parallel.ParallelUtils.Instance.MultiThreaded = false;

            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var cal = TestProviderHelper.CalendarProvider.GetCalendar("USD");

            var ins = AssetProductFactory.CreateTermAsianSwap(DateTime.Parse("2024-01-01"), DateTime.Parse("2024-03-31"), 800, "QS", cal, DateTime.Parse("2024-04-02"), usd, notional: 10000);
            ins.TradeId = "TestA";
            ins.DiscountCurve = "DISCO-USD";

            var pf = new Portfolio { Instruments = new List<IInstrument> { ins } };

            var discoUsd = new ConstantRateIrCurve(0.0, _originDate, "DISCO-USD", usd) { Currency = usd };

            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, _originDate, new Dictionary<Currency, double>(), new List<FxPair>(), new Dictionary<Currency, string> { { usd, "DISCO-USD" } });

            var fModel = new FundingModel(_originDate, new[] { discoUsd }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            fModel.SetupFx(fxMatrix);

            var model = new AssetFxModel(_originDate, fModel);

            var curveQS = new BasicPriceCurve(_originDate, _qsCurve.Keys.Select(DateTime.Parse).ToArray(), _qsCurve.Values.ToArray(), PriceCurveType.ICE, TestProviderHelper.CurrencyProvider)
            {
                AssetId = "QS",
                Name = "QS"
            };
            var curveCO = new BasicPriceCurve(_originDate, _coCurve.Keys.Select(DateTime.Parse).ToArray(), _coCurve.Values.ToArray(), PriceCurveType.ICE, TestProviderHelper.CurrencyProvider)
            {
                AssetId = "CO",
                Name = "CO"
            };

            model.AddPriceCurve("QS", curveQS);
            model.AddPriceCurve("CO", curveCO);
            model.AttachPortfolio(pf);
            return model;
        }

        [Fact]
        public void SimpleBenchmarkRiskFacts()
        {
            var model = GenerateTestData();
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");

            var lotSize = 100;
            var riskIns = _qsCurve.Select(x => (IAssetInstrument)new Future
            {
                AssetId = "QS",
                ContractQuantity = 100,
                Currency = usd,
                ExpiryDate = DateTime.Parse(x.Key),
                LotSize = lotSize,
                Strike = x.Value,
                TradeId = "QS|" + x.Key
            }).ToList();

            var spec = new AssetCurveBenchmarkSpec
            {
                CurveName = "QS",
                CurveType = PriceCurveType.ICE,
                Instruments = riskIns
            };

            var cube = AssetBenchmarkCurveRisk.Produce(model, new List<AssetCurveBenchmarkSpec> { spec }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider, usd);

            var riskSum = cube.SumOfAllRows;


            var expectedUnderZeroDiscount = (model.Portfolio.Instruments.First() as AsianSwap).Notional / lotSize;
            Assert.Equal(expectedUnderZeroDiscount, riskSum, 5);
        }

        [Fact]
        public void CrackBenchmarkRiskFacts()
        {
            var model = GenerateTestData();
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var cal = TestProviderHelper.CalendarProvider.GetCalendar("USD");

            var lotSizeCO = 1000;
            var riskInsA = _coCurve.Select(x => (IAssetInstrument)new Future
            {
                AssetId = "CO",
                ContractQuantity = 100,
                Currency = usd,
                ExpiryDate = DateTime.Parse(x.Key),
                LotSize = lotSizeCO,
                Strike = x.Value,
                TradeId = "CO|" + x.Key
            }).ToList();

            var firstDate = _originDate.LastDayOfMonth().AddDays(1);
            var lastDate = model.Portfolio.LastSensitivityDate.LastDayOfMonth();
            var riskInsB = new List<IAssetInstrument>();
            while (firstDate < lastDate)
            {
                var ins = AssetProductFactory.CreateTermAsianBasisSwap(firstDate,
                                                                       firstDate.LastDayOfMonth(),
                                                                       1,
                                                                       SwapPayReceiveType.Payer,
                                                                       "CO",
                                                                       "QS",
                                                                       cal,
                                                                       cal,
                                                                       firstDate.LastDayOfMonth(),
                                                                       usd,
                                                                       notionalLeg1: 1000,
                                                                       notionalLeg2: 1000 / 7.45);
                ins.TradeId = "Crack|" + firstDate.ToString("MMMyy");
                riskInsB.Add(ins);
                firstDate = firstDate.LastDayOfMonth().AddDays(1);
            }
            

            var specA = new AssetCurveBenchmarkSpec
            {
                CurveName = "CO",
                CurveType = PriceCurveType.ICE,
                Instruments = riskInsA
            };

            var specB = new AssetCurveBenchmarkSpec
            {
                CurveName = "QS",
                CurveType = PriceCurveType.ICE,
                DependsOnCurves = new string[] {"CO"},
                Instruments = riskInsB
            };


            var cube = AssetBenchmarkCurveRisk.Produce(model, new List<AssetCurveBenchmarkSpec> { specA, specB }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider, usd);

            var riskCo = cube.Filter(new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("Curve", "CO") });
            var riskQs = cube.Filter(new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("Curve", "QS") });

            var expectedUnderZeroDiscountForCoFutures = (model.Portfolio.Instruments.First() as AsianSwap).Notional * 7.45 / 1000;
            Assert.Equal(expectedUnderZeroDiscountForCoFutures, riskCo.SumOfAllRows, 5);

            var expectedUnderZeroDiscountForCoQsCracks = (model.Portfolio.Instruments.First() as AsianSwap).Notional * 7.45 / 1000;
            Assert.Equal(expectedUnderZeroDiscountForCoQsCracks, riskQs.SumOfAllRows, 5);
        }

        [Fact]
        public void CrackBenchmarkRiskNoRestripFacts()
        {
            var model = GenerateTestData();
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var cal = TestProviderHelper.CalendarProvider.GetCalendar("USD");

            var lotSizeCO = 1000;
            var riskInsA = _coCurve.Select(x => (IAssetInstrument)new Future
            {
                AssetId = "CO",
                ContractQuantity = 100,
                Currency = usd,
                ExpiryDate = DateTime.Parse(x.Key),
                LotSize = lotSizeCO,
                Strike = x.Value,
                TradeId = "CO|" + x.Key
            }).ToList();

            var firstDate = _originDate.LastDayOfMonth().AddDays(1);
            var lastDate = model.Portfolio.LastSensitivityDate.LastDayOfMonth();
            var riskInsB = new List<IAssetInstrument>();
            while (firstDate < lastDate)
            {
                var ins = AssetProductFactory.CreateTermAsianBasisSwap(firstDate,
                                                                       firstDate.LastDayOfMonth(),
                                                                       1,
                                                                       SwapPayReceiveType.Payer,
                                                                       "CO",
                                                                       "QS",
                                                                       cal,
                                                                       cal,
                                                                       firstDate.LastDayOfMonth(),
                                                                       usd,
                                                                       notionalLeg1: 1000,
                                                                       notionalLeg2: 1000 / 7.45);
                ins.TradeId = "Crack|" + firstDate.ToString("MMMyy");
                riskInsB.Add(ins);
                firstDate = firstDate.LastDayOfMonth().AddDays(1);
            }


            var specA = new AssetCurveBenchmarkSpec
            {
                CurveName = "CO",
                CurveType = PriceCurveType.ICE,
                Instruments = riskInsA
            };

            var specB = new AssetCurveBenchmarkSpec
            {
                CurveName = "QS",
                CurveType = PriceCurveType.ICE,
                DependsOnCurves = new string[] { "CO" },
                Instruments = riskInsB
            };


            var cube = AssetBenchmarkCurveRisk.Produce(model, new List<AssetCurveBenchmarkSpec> { specA, specB }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider, usd, false);

            var riskCo = cube.Filter(new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("Curve", "CO") });
            var riskQs = cube.Filter(new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("Curve", "QS") });

            var expectedUnderZeroDiscountForCoFutures = (model.Portfolio.Instruments.First() as AsianSwap).Notional * 7.45 / 1000;
            Assert.Equal(expectedUnderZeroDiscountForCoFutures, riskCo.SumOfAllRows, 5);

            var expectedUnderZeroDiscountForCoQsCracks = (model.Portfolio.Instruments.First() as AsianSwap).Notional * 7.45 / 1000;
            Assert.Equal(expectedUnderZeroDiscountForCoQsCracks, riskQs.SumOfAllRows, 5);
        }
    }
}
