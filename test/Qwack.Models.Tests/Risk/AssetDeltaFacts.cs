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
using Qwack.Models.Risk;
using Qwack.Core.Instruments.Asset;

namespace Qwack.Models.Tests.Risk
{
    [CollectionDefinition("AssetDeltaFacts", DisableParallelization = true)]
    public class AssetDeltaFacts
    {
        private IPvModel GenerateTestData()
        {
            Utils.Parallel.ParallelUtils.Instance.MultiThreaded = false;

            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var zar = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR");
            var nyc = TestProviderHelper.CalendarProvider.Collection["NYC"];

            var originDate = DateTime.Parse("2019-04-25");
            var ins = new Forward
            {
                TradeId = "TestA",
                AssetId = "FakeAsset",
                ExpiryDate = originDate.AddDays(30),
                PaymentCurrency = zar,
                Notional = 1e6,
                Strike = 1400,
                DiscountCurve = "DISCO-ZAR"
            };
            var pf = new Portfolio { Instruments = new List<IInstrument> { ins } };

            var discoUsd = new FlatIrCurve(0.00, usd, "DISCO-USD");
            var discoZar = new FlatIrCurve(0.00, zar, "DISCO-ZAR");
            var fxpairs = new List<FxPair>
            {
                new FxPair {Domestic = usd, Foreign =zar,PrimaryCalendar=nyc,SpotLag=2.Bd() },
                new FxPair {Domestic = zar, Foreign =usd,PrimaryCalendar=nyc,SpotLag=2.Bd() },
            };
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, originDate, new Dictionary<Currency, double> { { zar, 14.0 } }, fxpairs, new Dictionary<Currency, string> { { usd, "DISCO-USD" }, { zar, "DISCO-ZAR" } });
            
            var fModel = new FundingModel(originDate, new[] { discoUsd, discoZar }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            fModel.SetupFx(fxMatrix);

            var model = new AssetFxModel(originDate, fModel);

            var curve = new ConstantPriceCurve(100, originDate, TestProviderHelper.CurrencyProvider);
            model.AddPriceCurve("FakeAsset", curve);
            model.AddFixingDictionary("FakeAsset", new FixingDictionary());
            model.AttachPortfolio(pf);
            return model;
        }

        [Fact]
        public void AssetParallelDeltaFacts()
        {
            var model = GenerateTestData();
            var cube = model.AssetParallelDelta(TestProviderHelper.CurrencyProvider);

            Assert.Equal(1e8, cube.GetAllRows().First().Value);
        }

     

    }
}
