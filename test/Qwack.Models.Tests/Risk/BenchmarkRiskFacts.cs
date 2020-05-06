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

namespace Qwack.Models.Tests.Risk
{
    [CollectionDefinition("BenchmarkRiskFacts", DisableParallelization = true)]
    public class BenchmarkRiskFacts
    {
        DateTime _originDate = DateTime.Parse("2019-04-25");

        private IPvModel GenerateTestData()
        {
            Utils.Parallel.ParallelUtils.Instance.MultiThreaded = false;

            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var zar = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR");
            var nyc = TestProviderHelper.CalendarProvider.Collection["NYC"];

            var ins = new Forward
            {
                TradeId = "TestA",
                AssetId = "FakeAsset",
                ExpiryDate = _originDate.AddDays(180),
                PaymentCurrency = zar,
                Notional = 1e6,
                Strike = 1400,
                DiscountCurve = "DISCO-ZAR"
            };
            var pf = new Portfolio { Instruments = new List<IInstrument> { ins } };
            var pillars = new[] { _originDate.AddDays(90), _originDate.AddDays(180) };

            var discoUsd = new IrCurve(pillars, pillars.Select(p => 0.02).ToArray(), _originDate, "DISCO-USD", Interpolator1DType.Linear, usd);
            var discoZar = new IrCurve(pillars, pillars.Select(p => 0.02).ToArray(), _originDate, "DISCO-ZAR", Interpolator1DType.Linear, zar);

            var fxpairs = new List<FxPair>
            {
                new FxPair {Domestic = usd, Foreign =zar,PrimaryCalendar=nyc,SpotLag=2.Bd() },
                new FxPair {Domestic = zar, Foreign =usd,PrimaryCalendar=nyc,SpotLag=2.Bd() },
            };
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, _originDate, new Dictionary<Currency, double> { { zar, 14.0 } }, fxpairs, new Dictionary<Currency, string> { { usd, "DISCO-USD" }, { zar, "DISCO-ZAR" } });
            
            var fModel = new FundingModel(_originDate, new[] { discoUsd, discoZar }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            fModel.SetupFx(fxMatrix);

            var model = new AssetFxModel(_originDate, fModel);

            var curve = new ConstantPriceCurve(100, _originDate, TestProviderHelper.CurrencyProvider);
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
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var ix = new FloatRateIndex
            {
                Currency = usd,
                DayCountBasis = DayCountBasis.Act365F,
                DayCountBasisFixed = DayCountBasis.Act365F,
                FixingOffset = 0.Bd(),
                ResetTenor = 3.Months(),
                ResetTenorFixed = 3.Months(),
                RollConvention = RollType.F
            };

            var f1 = new STIRFuture
            {
                ContractSize = 1e6,
                Currency = usd,
                DCF = 0.25,
                Expiry = _originDate.AddDays(1),
                Position = 1.0,
                Index = ix,
                ForecastCurve = "DISCO-USD",
                SolveCurve = "DISCO-USD",
                TradeId = "f1",
                Price = 95,
                PillarDate = _originDate.AddDays(90)
            };

            var f2 = new STIRFuture
            {
                ContractSize = 1e6,
                Currency = usd,
                DCF = 0.25,
                Expiry = _originDate.AddDays(90),
                Position = 1.0,
                Index = ix,
                ForecastCurve = "DISCO-USD",
                SolveCurve = "DISCO-USD",
                TradeId = "f2",
                Price = 95,
                PillarDate = _originDate.AddDays(180),
            };
            var fic = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider)
            {
                f1,
                f2
            };

            var cube = model.BenchmarkRisk(fic, TestProviderHelper.CurrencyProvider, usd);

            var riskSum = cube.SumOfAllRows;

            Assert.Equal(-13.0, riskSum, 0);
        }

     

    }
}
