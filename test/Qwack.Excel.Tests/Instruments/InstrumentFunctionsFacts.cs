using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Excel.Instruments;
using Qwack.Excel.Utils;
using Qwack.Models.Models;
using Xunit;
using static ExcelDna.Integration.ExcelMissing;

namespace Qwack.Excel.Tests.Instruments
{
    public class InstrumentFunctionsFacts
    {
        [Fact]
        public void CreateAsianSwapFact()
        {
            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Could not parse date generation type - sqw",
                InstrumentFunctions.CreateAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", "sqw", "disco"));

            Assert.Equal("swppp¬0",
                InstrumentFunctions.CreateAsianSwap("swppp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("swppq¬0",
                InstrumentFunctions.CreateAsianSwap("swppq", DateTime.Today.ToOADate(), "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("swppz¬0",
                InstrumentFunctions.CreateAsianSwap("swppz", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.AddDays(100).ToOADate() } }, "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", "BusinessDays", "disco"));
        }

        [Fact]
        public void CreateAsianCrackDiffSwap()
        {
            Assert.Equal("Calendar xxxP not found in cache",
                InstrumentFunctions.CreateAsianCrackDiffSwap("cswp", "Jan-19", "xxP", "xxR", "ZAR", 0.0, 0.0, 0.0, "xxxP", "xxxR", "xxxPay", "2b", "2b", "2b", "disco"));
            Assert.Equal("Calendar xxxR not found in cache",
                InstrumentFunctions.CreateAsianCrackDiffSwap("cswp", "Jan-19", "xxP", "xxR", "ZAR", 0.0, 0.0, 0.0, "NYC", "xxxR", "xxxPay", "2b", "2b", "2b", "disco"));
            Assert.Equal("Calendar xxxPay not found in cache",
                InstrumentFunctions.CreateAsianCrackDiffSwap("cswp", "Jan-19", "xxP", "xxR", "ZAR", 0.0, 0.0, 0.0, "NYC", "NYC", "xxxPay", "2b", "2b", "2b", "disco"));
            Assert.Equal("cswp¬0",
                InstrumentFunctions.CreateAsianCrackDiffSwap("cswp", "Jan-19", "xxP", "xxR", "ZAR", 0.0, 0.0, 0.0, "NYC", "NYC", "NYC", "2b", "2b", "2b", "disco"));
            Assert.Equal("ccswp¬0",
                InstrumentFunctions.CreateAsianCrackDiffSwap("ccswp", (new DateTime(2019, 1, 1)).ToOADate(), "xxP", "xxR", "ZAR", 0.0, 0.0, 0.0, "NYC", "NYC", "NYC", "2b", "2b", "2b", "disco"));
        }


        [Fact]
        public void CreateFuturesCrackDiffSwap() => Assert.Equal("fdswap¬0",
               InstrumentFunctions.CreateFutureCrackDiffSwap("fdswap", "COF9", "QSF9", "CO", "QS", "USD", -19, 1000, 7460, "DISCO"));

        [Fact]
        public void CreateFuturesPositionSwap() => Assert.Equal("futXXX¬0",
               InstrumentFunctions.CreateFuture("futXXX", DateTime.Today, "CO", "USD", 74, 1000, 1000, 1.0));

        [Fact]
        public void CreateFutureOptionPositionSwap()
        {
            Assert.Equal("Could not parse call/put flag xyxy",
                InstrumentFunctions.CreateFutureOption("futOptXXX", DateTime.Today, "CO", "USD", 74, 1000, 1000, "xyxy", "xxxxx", "xxxx"));
            Assert.Equal("Could not parse option style flag xxxxx",
                InstrumentFunctions.CreateFutureOption("futOptXXX", DateTime.Today, "CO", "USD", 74, 1000, 1000, "C", "xxxxx", "xxxx"));
            Assert.Equal("Could not parse margining type flag xxxx",
                InstrumentFunctions.CreateFutureOption("futOptXXX", DateTime.Today, "CO", "USD", 74, 1000, 1000, "C", "European", "xxxx"));
            Assert.Equal("futOptXXX¬0",
                InstrumentFunctions.CreateFutureOption("futOptXXX", DateTime.Today, "CO", "USD", 74, 1000, 1000, "C", "European", "FuturesStyle"));
        }

        [Fact]
        public void CreateMonthlyAsianSwapFact()
        {
            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateMonthlyAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateMonthlyAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Could not parse date generation type - sqw",
                InstrumentFunctions.CreateMonthlyAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", "sqw", "disco"));

            Assert.Equal("swppp1¬0",
                InstrumentFunctions.CreateMonthlyAsianSwap("swppp1", "Jan-19", "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("swppq1¬0",
                InstrumentFunctions.CreateMonthlyAsianSwap("swppq1", DateTime.Today.ToOADate(), "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("swppz1¬0",
                InstrumentFunctions.CreateMonthlyAsianSwap("swppz1", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.AddDays(100).ToOADate() } }, "xx", "ZAR", 0.0, 0.0, "NYC", "NYC", "2b", "2b", "BusinessDays", "disco"));
        }

        [Fact]
        public void CreateCustomAsianSwapFact()
        {
            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateCustomAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, new[] { 0.0 }, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateCustomAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, new[] { 0.0 }, "NYC", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Could not parse date generation type - sqw",
                InstrumentFunctions.CreateCustomAsianSwap("swp", "Jan-19", "xx", "ZAR", 0.0, new[] { 0.0 }, "NYC", "NYC", "2b", "2b", "sqw", "disco"));

            Assert.Equal("Expecting a Nx2 array of period dates",
                InstrumentFunctions.CreateCustomAsianSwap("swppp", "Jan-19", "xx", "ZAR", 0.0, new[] { 0.0 }, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("Number of notionals must match number of periods",
                InstrumentFunctions.CreateCustomAsianSwap("swppp", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.ToOADate() } }, "xx", "ZAR", 0.0, new[] { 0.0, 0.0 }, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("swppp¬0",
                InstrumentFunctions.CreateCustomAsianSwap("swppp", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.ToOADate() } }, "xx", "ZAR", 0.0, new[] { 0.0 }, "NYC", "NYC", "2b", "2b", Value, "disco"));
        }

        [Fact]
        public void CreateAsianOptionFact()
        {
            Assert.Equal("Could not parse put/call flag - px",
                InstrumentFunctions.CreateAsianOption("swp", "Jan-19", "xx", "ZAR", 0.0, "px", 0.0, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateAsianOption("swp", "Jan-19", "xx", "ZAR", 0.0, "P", 0.0, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateAsianOption("swp", "Jan-19", "xx", "ZAR", 0.0, "P", 0.0, "NYC", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Could not parse date generation type - zzzz",
                InstrumentFunctions.CreateAsianOption("swp", "Jan-19", "xx", "ZAR", 0.0, "P", 0.0, "NYC", "NYC", "2b", "2b", "zzzz", "disco"));

            Assert.Equal("opttt¬0",
                InstrumentFunctions.CreateAsianOption("opttt", "Jan-19", "xx", "ZAR", 0.0, "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("optttA¬0",
                InstrumentFunctions.CreateAsianOption("optttA", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.ToOADate() } }, "xx", "ZAR", 0.0, "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("optttB¬0",
                InstrumentFunctions.CreateAsianOption("optttB", DateTime.Today.ToOADate(), "xx", "ZAR", 0.0, "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

        }

        [Fact]
        public void CreateAsianLoockbackOptionFact()
        {
            Assert.Equal("Could not parse put/call flag - px",
                InstrumentFunctions.CreateAsianLookbackOption("swp", "Jan-19", "xx", "ZAR", "px", 0.0, "xxx", "xxx", "2b", "2b", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateAsianLookbackOption("swp", "Jan-19", "xx", "ZAR", "P", 0.0, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateAsianLookbackOption("swp", "Jan-19", "xx", "ZAR", "P", 0.0, "NYC", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Could not parse date generation type - zzzz",
                InstrumentFunctions.CreateAsianLookbackOption("swp", "Jan-19", "xx", "ZAR", "P", 0.0, "NYC", "NYC", "2b", "2b", "zzzz", "disco"));

            Assert.Equal("LBopttt¬0",
                InstrumentFunctions.CreateAsianLookbackOption("LBopttt", "Jan-19", "xx", "ZAR", "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("LBoptttA¬0",
                InstrumentFunctions.CreateAsianLookbackOption("LBoptttA", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.ToOADate() } }, "xx", "ZAR", "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("LBoptttB¬0",
                InstrumentFunctions.CreateAsianLookbackOption("LBoptttB", DateTime.Today.ToOADate(), "xx", "ZAR", "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));
        }

        [Fact]
        public void CreateBackpricingOptionFact()
        {
            Assert.Equal("Could not parse put/call flag - px",
                InstrumentFunctions.CreateBackPricingOption("swp", "Jan-19", "xx", "ZAR", "px", 0.0, "xxx", "xxx", "2b", "2b", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateBackPricingOption("swp", "Jan-19", "xx", "ZAR", "P", 0.0, "xxx", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateBackPricingOption("swp", "Jan-19", "xx", "ZAR", "P", 0.0, "NYC", "xxx", "2z", "2f", Value, "disco"));

            Assert.Equal("Could not parse date generation type - zzzz",
                InstrumentFunctions.CreateBackPricingOption("swp", "Jan-19", "xx", "ZAR", "P", 0.0, "NYC", "NYC", "2b", "2b", "zzzz", "disco"));

            Assert.Equal("BPopttt¬0",
                InstrumentFunctions.CreateBackPricingOption("BPopttt", "Jan-19", "xx", "ZAR", "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("BPoptttA¬0",
                InstrumentFunctions.CreateBackPricingOption("BPoptttA", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.ToOADate() } }, "xx", "ZAR", "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("BPoptttB¬0",
                InstrumentFunctions.CreateBackPricingOption("BPoptttB", DateTime.Today.ToOADate(), "xx", "ZAR", "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));

            Assert.Equal("BPoptttC¬0",
                InstrumentFunctions.CreateBackPricingOption("BPoptttC", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.ToOADate(), DateTime.Today.ToOADate() } }, "xx", "ZAR", "P", 0.0, "NYC", "NYC", "2b", "2b", Value, "disco"));
        }

        [Fact]
        public void CreateMultiPeriodBackpricingOptionFact()
        {
            Assert.Equal("Could not parse put/call flag - px",
                InstrumentFunctions.CreateMultiPeriodBackPricingOption("swp", "Jan-19", DateTime.Today, DateTime.Today, "xx", "ZAR", "px", 0.0, "xxx", "2b", Value, "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateMultiPeriodBackPricingOption("swp", "Jan-19", DateTime.Today, DateTime.Today, "xx", "ZAR", "P", 0.0, "xxx", "2b", Value, "disco"));

            Assert.Equal("Could not parse date generation type - zzzz",
                InstrumentFunctions.CreateMultiPeriodBackPricingOption("swp", "Jan-19", DateTime.Today, DateTime.Today, "xx", "ZAR", "P", 0.0, "NYC", "2b", "zzzz", "disco"));

            Assert.Equal("Period dates must be a Nx2 range",
                InstrumentFunctions.CreateMultiPeriodBackPricingOption("mbpOptA", "Jan-19", DateTime.Today, DateTime.Today, "xx", "ZAR", "P", 0.0, "NYC", "2b", Value, "disco"));

            Assert.Equal("mbpOptA¬0",
                InstrumentFunctions.CreateMultiPeriodBackPricingOption("mbpOptA", new object[,] { { DateTime.Today.ToOADate(), DateTime.Today.ToOADate() } }, DateTime.Today, DateTime.Today, "xx", "ZAR", "P", 0.0, "NYC", "2b", Value, "disco"));
        }

        [Fact]
        public void CreateAmericanBarrierOptionFact()
        {
            Assert.Equal("Could not parse barrier side bs",
                InstrumentFunctions.CreateAmericanBarrierOption("swp", "xx", DateTime.Today, DateTime.Today, DateTime.Today, DateTime.Today, "ZAR", "px", 0.0, 100.0, 110.0, "bs", "bt", "disco", "2b", "nocal"));

            Assert.Equal("Could not parse barrier type bt",
                InstrumentFunctions.CreateAmericanBarrierOption("swp", "xx", DateTime.Today, DateTime.Today, DateTime.Today, DateTime.Today, "ZAR", "px", 0.0, 100.0, 110.0, "Up", "bt", "disco", "2b", "nocal"));

            Assert.Equal("Could not parse put/call flag - px",
                InstrumentFunctions.CreateAmericanBarrierOption("swp", "xx", DateTime.Today, DateTime.Today, DateTime.Today, DateTime.Today, "ZAR", "px", 0.0, 100.0, 110.0, "Up", "KO", "disco", "2b", "nocal"));

            Assert.Equal("Calendar nocal not found in cache",
                InstrumentFunctions.CreateAmericanBarrierOption("swp", "xx", DateTime.Today, DateTime.Today, DateTime.Today, DateTime.Today, "ZAR", "P", 0.0, 100.0, 110.0, "Up", "KO", "disco", "2b", "nocal"));

            Assert.Equal("ambo¬0",
                InstrumentFunctions.CreateAmericanBarrierOption("ambo", "xx", DateTime.Today, DateTime.Today, DateTime.Today, DateTime.Today, "ZAR", "P", 0.0, 100.0, 110.0, "Up", "KO", "disco", "2b", "NYC"));
        }

        [Fact]
        public void CreateEuropeanOptionFact()
        {
            Assert.Equal("Could not parse put/call flag - px",
                InstrumentFunctions.CreateEuropeanOption("euOptt", DateTime.Today, "xx", "ZAR", 100.0, "px", 0.0, "xxx", "2b", "2b", "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateEuropeanOption("euOptt", DateTime.Today, "xx", "ZAR", 100.0, "P", 0.0, "xxx", "2b", "2b", "disco"));

            Assert.Equal("euOptt¬0",
                InstrumentFunctions.CreateEuropeanOption("euOptt", DateTime.Today, "xx", "ZAR", 100.0, "P", 0.0, "NYC", "2b", "2b", "disco"));
        }

        [Fact]
        public void CreateEuropeanFXOptionFact()
        {
            Assert.Equal("Could not parse put/call flag - px",
                InstrumentFunctions.CreateEuropeanFxOption("euFxOptt", DateTime.Today, "pair", 100.0, "px", 0.0, "xxx", "2b", "2b", "disco"));

            Assert.Equal("Calendar xxx not found in cache",
                InstrumentFunctions.CreateEuropeanFxOption("euFxOptt", DateTime.Today, "pair", 100.0, "P", 0.0, "xxx", "2b", "2b", "disco"));

            Assert.Equal("Currency pai not found in cache",
                InstrumentFunctions.CreateEuropeanFxOption("euFxOptt", DateTime.Today, "pair", 100.0, "P", 0.0, "NYC", "2b", "2b", "disco"));

            Assert.Equal("euFxOptt¬0",
                InstrumentFunctions.CreateEuropeanFxOption("euFxOptt", DateTime.Today, "USD/ZAR", 100.0, "P", 0.0, "NYC", "2b", "2b", "disco"));
        }

        [Fact]
        public void ProductParRateFact()
        {
            InstrumentFunctions.CreateAsianSwap("swpFake", "Jan-19", "xx", "USD", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco");

            var irs = new Mock<IrSwap>();
            ContainerStores.GetObjectCache<IrSwap>().PutObject("prodIRS", new SessionItem<IrSwap>() { Name = "prodIRS", Value = irs.Object });

            var model = new Mock<IAssetFxModel>();
            model.Setup(m => m.GetPriceCurve("xx", null)).Returns(new ConstantPriceCurve(100, DateTime.Today, TestProviderHelper.CurrencyProvider));
            var fModel = new Mock<IFundingModel>();
            fModel.Setup(fm => fm.GetDf(It.IsAny<Currency>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(1.0);
            model.Setup(m => m.FundingModel).Returns(fModel.Object);

            ContainerStores.GetObjectCache<IAssetFxModel>().PutObject("model", new SessionItem<IAssetFxModel>() { Name = "model", Value = model.Object });

            Assert.Equal("Could not find model with name xxx",
                InstrumentFunctions.ProductParRate("trd", "xxx"));

            Assert.Equal("Could not find any trade with name trd",
                InstrumentFunctions.ProductParRate("trd", "model"));

            Assert.Equal("Could not find asset trade with name prodIRS",
                InstrumentFunctions.ProductParRate("prodIRS", "model"));

            Assert.Equal(1000000.0,
                InstrumentFunctions.ProductParRate("swpFake", "model"));
        }

        [Fact]
        public void ProductPVFact()
        {
            InstrumentFunctions.CreateAsianSwap("swpFake", "Jan-19", "xx", "USD", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco");

            var irs = new Mock<IrSwap>();
            ContainerStores.GetObjectCache<IrSwap>().PutObject("prodIRS", new SessionItem<IrSwap>() { Name = "prodIRS", Value = irs.Object });

            var model = new Mock<IAssetFxModel>();
            model.Setup(m => m.GetPriceCurve("xx", null)).Returns(new ConstantPriceCurve(100, DateTime.Today, TestProviderHelper.CurrencyProvider));
            var fModel = new Mock<IFundingModel>();
            fModel.Setup(fm => fm.GetDf(It.IsAny<Currency>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(1.0);
            model.Setup(m => m.FundingModel).Returns(fModel.Object);

            ContainerStores.GetObjectCache<IAssetFxModel>().PutObject("model", new SessionItem<IAssetFxModel>() { Name = "model", Value = model.Object });

            Assert.Equal("Could not find model with name xxx",
                InstrumentFunctions.ProductPV("trd", "xxx", Value));

            Assert.Equal("Could not find any trade with name trd",
                InstrumentFunctions.ProductPV("trd", "model", Value));

            Assert.Equal("Could not find asset trade with name prodIRS",
                InstrumentFunctions.ProductPV("prodIRS", "model", Value));

            Assert.Equal(0.0,
                InstrumentFunctions.ProductPV("swpFake", "model", Value));
        }

        [Fact]
        public void PortfolioPVFact()
        {
            InstrumentFunctions.CreateAsianSwap("swpFake", "Jan-19", "xx", "USD", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco");

            var model = new Mock<IAssetFxModel>();
            model.Setup(m => m.GetPriceCurve("xx", null)).Returns(new ConstantPriceCurve(100, DateTime.Today, TestProviderHelper.CurrencyProvider));
            var fModel = new Mock<IFundingModel>();
            fModel.Setup(fm => fm.GetDf(It.IsAny<Currency>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(1.0);
            model.Setup(m => m.FundingModel).Returns(fModel.Object);

            ContainerStores.GetObjectCache<IAssetFxModel>().PutObject("model", new SessionItem<IAssetFxModel>() { Name = "model", Value = model.Object });

            Assert.Equal("Could not find portfolio or trade with name trd",
                InstrumentFunctions.AssetPortfolioPV("out", "trd", "xxx", Value));

            Assert.Equal("Could not find model with name xxx",
                InstrumentFunctions.AssetPortfolioPV("out", "swpFake", "xxx", Value));

            Assert.Equal("out¬0",
                InstrumentFunctions.AssetPortfolioPV("out", "swpFake", "model", Value));

            Assert.Equal("out¬1",
                InstrumentFunctions.AssetPortfolioPV("out", "swpFake", "model", "USD"));
        }

        [Fact]
        public void AssetPnLAttributionFacts()
        {
            InstrumentFunctions.CreateAsianSwap("swpFake", "Jan-19", "xx", "USD", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco");

            var model = new Mock<IAssetFxModel>();
            ContainerStores.GetObjectCache<IAssetFxModel>().PutObject("model", new SessionItem<IAssetFxModel>() { Name = "model", Value = model.Object });

            var pnlAttribMock = new Mock<IPnLAttributor>();
            pnlAttribMock
                .Setup(p => p.BasicAttribution(It.IsAny<Portfolio>(), It.IsAny<IAssetFxModel>(), It.IsAny<IAssetFxModel>(), It.IsAny<Currency>(), It.IsAny<ICurrencyProvider>()))
                .Returns(new ResultCube());
            ContainerStores.PnLAttributor = pnlAttribMock.Object;

            Assert.Equal("Could not find portfolio or trade with name trd",
                InstrumentFunctions.AssetPnLAttribution("outhhg", "trd", "ms1", "ms2", "USD"));

            Assert.Equal("Could not find model with name ms1",
                InstrumentFunctions.AssetPnLAttribution("outhhg", "swpFake", "ms1", "ms2", "USD"));

            Assert.Equal("Could not find model with name ms2",
                InstrumentFunctions.AssetPnLAttribution("outhhg", "swpFake", "model", "ms2", "USD"));

            Assert.Equal("outhhg¬0",
                InstrumentFunctions.AssetPnLAttribution("outhhg", "swpFake", "model", "model", "USD"));

            pnlAttribMock.Verify(p => p.BasicAttribution(It.IsAny<Portfolio>(), It.IsAny<IAssetFxModel>(), It.IsAny<IAssetFxModel>(), It.IsAny<Currency>(), It.IsAny<ICurrencyProvider>()), Times.Once);
        }

        [Fact]
        public void AssetPnLAttributionExplainFacts()
        {
            InstrumentFunctions.CreateAsianSwap("swpFake", "Jan-19", "xx", "USD", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco");

            var model = new Mock<IAssetFxModel>();
            ContainerStores.GetObjectCache<IAssetFxModel>().PutObject("model", new SessionItem<IAssetFxModel>() { Name = "model", Value = model.Object });

            var pnlAttribMock = new Mock<IPnLAttributor>();
            pnlAttribMock
                .Setup(p => p.ExplainAttribution(It.IsAny<Portfolio>(), It.IsAny<IAssetFxModel>(), It.IsAny<IAssetFxModel>(), It.IsAny<ICube>(), It.IsAny<Currency>(), It.IsAny<ICurrencyProvider>()))
                .Returns(new ResultCube());
            ContainerStores.PnLAttributor = pnlAttribMock.Object;

            ContainerStores.GetObjectCache<ICube>().PutObject("greekzzz", new SessionItem<ICube>() { Name = "greekzzz", Value = new ResultCube() });

            Assert.Equal("Could not find portfolio or trade with name trd",
                InstrumentFunctions.AssetPnLAttributionExplain("outhhgz", "trd", "ms1", "ms2", "stavros", "USD"));

            Assert.Equal("Could not find model with name ms1",
                InstrumentFunctions.AssetPnLAttributionExplain("outhhgz", "swpFake", "ms1", "ms2", "stavros", "USD"));

            Assert.Equal("Could not find model with name ms2",
                InstrumentFunctions.AssetPnLAttributionExplain("outhhgz", "swpFake", "model", "ms2", "stavros", "USD"));

            Assert.Equal("Could not find greeks cube with name stavros",
                InstrumentFunctions.AssetPnLAttributionExplain("outhhgz", "swpFake", "model", "model", "stavros", "USD"));

            Assert.Equal("outhhgz¬0",
                InstrumentFunctions.AssetPnLAttributionExplain("outhhgz", "swpFake", "model", "model", "greekzzz", "USD"));

            pnlAttribMock.Verify(p => p.ExplainAttribution(It.IsAny<Portfolio>(), It.IsAny<IAssetFxModel>(), It.IsAny<IAssetFxModel>(), It.IsAny<ICube>(), It.IsAny<Currency>(), It.IsAny<ICurrencyProvider>()), Times.Once);
        }

        [Fact]
        public void AssetPnLAttributionExplainWithActivityFacts()
        {
            InstrumentFunctions.CreateAsianSwap("swpFake", "Jan-19", "xx", "USD", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco");

            var model = new Mock<IAssetFxModel>();
            ContainerStores.GetObjectCache<IAssetFxModel>().PutObject("model", new SessionItem<IAssetFxModel>() { Name = "model", Value = model.Object });

            var pnlAttribMock = new Mock<IPnLAttributor>();
            pnlAttribMock
                .Setup(p => p.ExplainAttribution(It.IsAny<Portfolio>(), It.IsAny<Portfolio>(), It.IsAny<IAssetFxModel>(), It.IsAny<IAssetFxModel>(), It.IsAny<Currency>(), It.IsAny<ICurrencyProvider>(), It.IsAny<bool>()))
                .Returns(new ResultCube());
            ContainerStores.PnLAttributor = pnlAttribMock.Object;

            Assert.Equal("Could not find portfolio or trade with name trdS",
                InstrumentFunctions.AssetPnLAttributionExplainWithActivity("outzzy", "trdS", "trdE", "ms1", "ms2", "USD", false));

            Assert.Equal("Could not find portfolio or trade with name trdE",
                InstrumentFunctions.AssetPnLAttributionExplainWithActivity("outzzy", "swpFake", "trdE", "ms1", "ms2", "USD", false));

            Assert.Equal("Could not find model with name ms1",
                InstrumentFunctions.AssetPnLAttributionExplainWithActivity("outzzy", "swpFake", "swpFake", "ms1", "ms2", "USD", false));

            Assert.Equal("Could not find model with name ms2",
                InstrumentFunctions.AssetPnLAttributionExplainWithActivity("outzzy", "swpFake", "swpFake", "model", "ms2", "USD", false));

            Assert.Equal("outzzy¬0",
                InstrumentFunctions.AssetPnLAttributionExplainWithActivity("outzzy", "swpFake", "swpFake", "model", "model", "USD", false));

            pnlAttribMock.Verify(p => p.ExplainAttribution(It.IsAny<Portfolio>(), It.IsAny<Portfolio>(), It.IsAny<IAssetFxModel>(), It.IsAny<IAssetFxModel>(), It.IsAny<Currency>(), It.IsAny<ICurrencyProvider>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public void FilterPortfolioFacts()
        {
            InstrumentFunctions.CreateAsianSwap("swpFake", "Jan-19", "xx", "USD", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco");
            InstrumentFunctions.CreatePortfolio("pfOutK", new object[,] { { "swpFake" } });

            Assert.Equal("Portfolio hhhh not found",
                InstrumentFunctions.FilterPortfolio("filterPfOutK", "hhhh", new object[] { "swpFake" }, false));

            Assert.Equal("filterPfOutK¬0",
                InstrumentFunctions.FilterPortfolio("filterPfOutK", "pfOutK", new object[] { "swpFake" }, false));
        }

        [Fact]
        public void FilterPortfolioByNameFacts()
        {
            InstrumentFunctions.CreateAsianSwap("swpFake", "Jan-19", "xx", "USD", 0.0, 0.0, "NYC", "NYC", "2b", "2b", Value, "disco");
            InstrumentFunctions.CreatePortfolio("pfOutK", new object[,] { { "swpFake" } });

            Assert.Equal("Portfolio hhhh not found",
                InstrumentFunctions.FilterPortfolioByName("filterPfOutKk", "hhhh", new object[] { "swpFake" }));

            Assert.Equal("filterPfOutKk¬0",
                InstrumentFunctions.FilterPortfolioByName("filterPfOutKk", "pfOutK", new object[] { "swpFake" }));
        }
    }
}
