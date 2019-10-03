using System;
using Xunit;
using Qwack.Paths.Payoffs;
using System.Linq;
using static Qwack.MonteCarlo.Test.Payoffs.Helpers;

namespace Qwack.MonteCarlo.Test.Payoffs
{
    public class BackPricingOptionFacts
    {
        [Fact]
        public void CanProcess()
        {
            var t = new DateTime(2019, 10, 03);
            var fc = GetFeatureCollection();
            var dates = Enumerable.Range(10, 10).Select(i => t.AddDays(i)).ToList();
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var sut = new BackPricingOption("Asset", dates, dates[5], t.AddDays(100), t.AddDays(100), Core.Basic.OptionType.C, "boo", usd, 1.0);
            var b = GetBlock(20);

            sut.SetupFeatures(fc.Object);
            sut.AverageRegressor.SetupFeatures(fc.Object);
            sut.SettlementRegressor.SetupFeatures(fc.Object);
            sut.Finish(fc.Object);
            sut.AverageRegressor.Finish(fc.Object);
            sut.SettlementRegressor.Finish(fc.Object);
            sut.Process(b);

            Assert.Equal(0.0, sut.AverageResult);
            Assert.Equal(0.0, sut.ResultStdError);

            Assert.True(sut.ResultsByPath.All(x => x == 0.0));
        }
    }
}
