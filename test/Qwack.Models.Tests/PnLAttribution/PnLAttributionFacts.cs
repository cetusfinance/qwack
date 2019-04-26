using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Xunit;
using Qwack.Models.Tests;
using Qwack.Core.Curves;
using Qwack.Core.Basic;
using Qwack.Dates;
using System.Linq;
using Qwack.Models.Models;

namespace Qwack.Models.Tests.PnLAttribution
{
    [CollectionDefinition("PnLAttributionTests", DisableParallelization = true)]
    public class PnLAttributionFacts
    {
        private (IAssetFxModel startModel, IAssetFxModel endModel, Portfolio portfolio) GenerateTestData()
        {
            Utils.Parallel.ParallelUtils.Instance.MultiThreaded = false;

            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var zar = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR");
            var nyc = TestProviderHelper.CalendarProvider.Collection["NYC"];

            var originDate = DateTime.Parse("2019-04-25");
            var ins = new FxForward
            {
                TradeId = "TestA",
                DeliveryDate = originDate.AddDays(30),
                DomesticCCY = zar,
                ForeignCCY = usd,
                DomesticQuantity = 1e6,
                Strike = 14,
                ForeignDiscountCurve = "DISCO-USD"
            };
            var pf = new Portfolio { Instruments = new List<IInstrument> { ins } };

            var discoUsd = new FlatIrCurve(0.02, usd, "DISCO-USD");
            var discoZar = new FlatIrCurve(0.05, zar, "DISCO-ZAR");
            var fxpairs = new List<FxPair>
            {
                new FxPair {Domestic = usd, Foreign =zar,SettlementCalendar=nyc,SpotLag=2.Bd() },
                new FxPair {Domestic = zar, Foreign =usd,SettlementCalendar=nyc,SpotLag=2.Bd() },
            };
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(zar, originDate, new Dictionary<Currency, double> { { usd, 14.0 } }, fxpairs, new Dictionary<Currency, string> { { usd, "DISCO-USD" }, { zar, "DISCO-ZAR" } });
            
            var fModel = new FundingModel(originDate, new[] { discoUsd, discoZar }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            fModel.SetupFx(fxMatrix);
            var startModel = new AssetFxModel(originDate, fModel);

            var endFModel = fModel.DeepClone();
            endFModel.FxMatrix.SpotRates[usd] = 15;
            var endModel = startModel.Clone(endFModel);

            return (startModel, endModel, pf);
        }

        [Fact]
        public void BasicPnLAttributionFacts()
        {
            var (startModel, endModel, portfolio) = GenerateTestData();
            var zar = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR");

            var result = Models.PnLAttribution.BasicAttribution(portfolio, startModel, endModel, zar, TestProviderHelper.CurrencyProvider);
            var sum = result.GetAllRows().Sum(x => x.Value);
            var expected = portfolio.PV(endModel, zar).GetAllRows().Sum(x => x.Value) 
                - portfolio.PV(startModel, zar).GetAllRows().Sum(x => x.Value);
            Assert.Equal(expected, sum, 10);
        }

        [Fact]
        public void ExplainPnLAttributionFacts()
        {
            var (startModel, endModel, portfolio) = GenerateTestData();
            var zar = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR");

            var result = Models.PnLAttribution.ExplainAttributionInLineGreeks(portfolio, startModel, endModel, zar, TestProviderHelper.CurrencyProvider);
            var sum = result.GetAllRows().Sum(x => x.Value);
            var expected = portfolio.PV(endModel, zar).GetAllRows().Sum(x => x.Value)
                - portfolio.PV(startModel, zar).GetAllRows().Sum(x => x.Value);
            Assert.Equal(expected, sum, 10);
        }

    }
}
