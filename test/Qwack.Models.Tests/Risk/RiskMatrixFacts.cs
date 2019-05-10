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

namespace Qwack.Models.Tests.PnLAttribution
{
    [CollectionDefinition("RiskMatrixFacts", DisableParallelization = true)]
    public class RiskMatrixFacts
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

            var discoUsd = new FlatIrCurve(0.02, usd, "DISCO-USD");
            var discoZar = new FlatIrCurve(0.05, zar, "DISCO-ZAR");
            var fxpairs = new List<FxPair>
            {
                new FxPair {Domestic = usd, Foreign =zar,SettlementCalendar=nyc,SpotLag=2.Bd() },
                new FxPair {Domestic = zar, Foreign =usd,SettlementCalendar=nyc,SpotLag=2.Bd() },
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
        public void BasicRiskMatrixFacts()
        {
            var model = GenerateTestData();
            var zar = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR");
            var rm = new RiskMatrix("FakeAsset", zar, MutationType.FlatShift, RiskMetric.FxDelta, 10, 0.1, 2, TestProviderHelper.CurrencyProvider, true);

            var cube = rm.Generate(model);
            
            foreach(var row in cube.GetAllRows())
            {
                if((string)row.MetaData[5]=="FakeAsset~0" && (string)row.MetaData[6]=="ZAR~0")
                {
                    Assert.Equal(0.0, row.Value);
                }
            }
        }

     

    }
}
