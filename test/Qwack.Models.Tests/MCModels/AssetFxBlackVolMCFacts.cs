using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Models.MCModels;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Options.VolSurfaces;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Dates;

namespace Qwack.Models.Tests.MCModels
{
    public class AssetFxBlackVolMCFacts
    {
        private AssetFxMCModel GetSut(bool expensiveFutures, BaseMetric baseMetric=BaseMetric.PV)
        {
            var buildDate = DateTime.Parse("2018-10-04");
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            TestProviderHelper.CalendarProvider.Collection.TryGetCalendar("NYC", out var usdCal);
            var dfCurve = new IrCurve(new[] { buildDate, buildDate.AddDays(1000) }, new[] { 0.0, 0.0 }, buildDate, "disco", Math.Interpolation.Interpolator1DType.Linear, usd, "DISCO");

            var comCurve = new PriceCurve(buildDate, new[] { buildDate, buildDate.AddDays(15), buildDate.AddDays(100) }, new[] { 100.0, 100.0, 100.0 }, PriceCurveType.NYMEX, TestProviderHelper.CurrencyProvider)
            {
                Name = "CL",
                AssetId = "CL"
            };
            var comSurface = new ConstantVolSurface(buildDate, 0.32) { AssetId = "CL" }; 
            var fModel = new FundingModel(buildDate, new Dictionary<string, IrCurve> { { "DISCO", dfCurve } }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var fxM = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxM.Init(usd, buildDate, new Dictionary<Currency, double>(), new List<FxPair>(), new Dictionary<Currency, string> { { usd, "DISCO" } });
            fModel.SetupFx(fxM);

            var aModel = new AssetFxModel(buildDate, fModel);
            aModel.AddVolSurface("CL", comSurface);
            aModel.AddPriceCurve("CL", comCurve);

            var product = AssetProductFactory.CreateTermAsianSwap(buildDate.AddDays(10), buildDate.AddDays(20), 99, "CL", usdCal, buildDate.AddDays(21), usd);
            product.TradeId = "waaah";
            product.DiscountCurve = "DISCO";


            var pfolio = new Portfolio { Instruments = new List<IInstrument> { product } };
            var settings = new McSettings
            {
                Generator = RandomGeneratorType.MersenneTwister,
                McModelType = McModelType.Black,
                NumberOfPaths = 2048,
                NumberOfTimesteps = 1,
                ReportingCurrency = usd,
                ExposureDates = new DateTime[] { buildDate.AddDays(5), buildDate.AddDays(20), buildDate.AddDays(22) },
                ExpensiveFuturesSimulation = expensiveFutures,
                Parallelize = false,
                FuturesMappingTable = new Dictionary<string, string> { { "CL","CL"} },
                Metric = baseMetric
            };
            var sut = new AssetFxMCModel(buildDate, pfolio, aModel, settings, TestProviderHelper.CurrencyProvider, TestProviderHelper.FutureSettingsProvider, TestProviderHelper.CalendarProvider);
            return sut;
        }

        [Fact]
        public void CanRunPV()
        {
            var sut = GetSut(false);
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var pvCube = sut.PV(usd);

            Assert.Equal(1.0, pvCube.GetAllRows().First().Value, 0);
        }

        [Fact]
        public void CanRunPV2()
        {
            var sut = GetSut(true);
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var pvCube = sut.PV(usd);

            Assert.Equal(1.0, pvCube.GetAllRows().First().Value, 0);
        }

        [Fact]
        public void CanRunPFE()
        {
            var sut = GetSut(false, BaseMetric.PFE);
            var pvCube = sut.PFE(0.95);

            Assert.Equal(3, pvCube.GetAllRows().Length);
            Assert.Equal(0.0, pvCube.GetAllRows().Last().Value);
        }
    }
}
