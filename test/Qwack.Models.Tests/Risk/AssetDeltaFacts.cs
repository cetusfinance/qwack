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
using Qwack.Models.Calibrators;
using static System.Security.Cryptography.ECCurve;

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

        private IPvModel GenerateTestDataBasis()
        {
            Utils.Parallel.ParallelUtils.Instance.MultiThreaded = false;

            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var nyc = TestProviderHelper.CalendarProvider.Collection["NYC"];

            var originDate = DateTime.Parse("2023-11-03");
            var ins = new Future
            {
                TradeId = "TestA",
                AssetId = "QS",
                ExpiryDate = originDate.AddDays(30),
                Currency = usd,
                LotSize = 100,
                ContractQuantity = 1,
                Strike = 800,
            };

            var pf = new Portfolio { Instruments = new List<IInstrument> { ins } };

            var discoUsd = new FlatIrCurve(0.00, usd, "DISCO-USD");


            var fModel = new FundingModel(originDate, new[] { discoUsd }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);


            var model = new AssetFxModel(originDate, fModel);

            var curveBR = new ConstantPriceCurve(100, originDate, TestProviderHelper.CurrencyProvider) { AssetId = "BR", Name="BR" };
            var solver = new NewtonRaphsonAssetBasisCurveSolver(TestProviderHelper.CurrencyProvider);
            var basisSwapA = AssetProductFactory.CreateBulletBasisSwap(originDate.AddDays(30), originDate.AddDays(30), -20, "BR", "QS", usd, 3000, -3000 / 7.45);

            var curveQS = new BasisPriceCurve(new() { basisSwapA }, new List<DateTime>() { originDate.AddDays(30) }, discoUsd, curveBR, originDate, Transport.BasicTypes.PriceCurveType.NYMEX, solver)
            { AssetId = "QS", Name = "QS" };

            model.AddPriceCurve("BR", curveBR);
            model.AddPriceCurve("QS", curveQS);
            model.AddFixingDictionary("BR", new FixingDictionary());
            model.AddFixingDictionary("QS", new FixingDictionary());
            model.AttachPortfolio(pf);
            return model;
        }

        [Fact]
        public void AssetCurveBasisDelta()
        {
            var model = GenerateTestDataBasis();
            var cube = model.AssetDelta(false, false);
            var r = cube.GetAllRows();
            Assert.Equal(2, r.Length); //one delta point for outright, one for basis swap

            var ixCurveType = cube.GetColumnIndex("CurveType");
            var rowOutrightDelta = r.Where(x => (string)x.MetaData[ixCurveType] == "Outright").FirstOrDefault();
            Assert.Equal(100 * 7.45, rowOutrightDelta.Value, 6);
            var rowBasisDelta = r.Where(x => (string)x.MetaData[ixCurveType] == "Basis").FirstOrDefault();
            Assert.Equal(100 * 7.45, rowBasisDelta.Value, 6);
        }
    }
}
