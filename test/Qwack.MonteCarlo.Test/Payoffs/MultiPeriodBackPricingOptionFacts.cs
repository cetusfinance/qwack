using System;
using Xunit;
using Qwack.Paths.Payoffs;
using System.Linq;
using static Qwack.MonteCarlo.Test.Payoffs.Helpers;
using System.Collections.Generic;
using Qwack.Transport.BasicTypes;

namespace Qwack.MonteCarlo.Test.Payoffs
{
    public class MultiPeriodBackPricingOptionFacts
    {
        //[Fact]
        //public void CanProcess()
        //{
        //    var t = new DateTime(2019, 10, 03);
        //    var fc = GetFeatureCollection();
        //    var dates1 = Enumerable.Range(10, 10).Select(i => t.AddDays(i)).ToArray();
        //    var dates2 = Enumerable.Range(20, 10).Select(i => t.AddDays(i)).ToArray();
        //    var dates = new List<DateTime[]> { dates1, dates2 };
        //    var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
        //    var sut = new MultiPeriodBackPricingOptionPP("Asset", dates, dates1[5], [t.AddDays(100)], t.AddDays(100), OptionType.C, "boo", usd, 1.0);
        //    var b = GetBlock(20);

        //    sut.SetupFeatures(fc.Object);
        //    foreach(var ar in sut.AverageRegressors)
        //        ar.SetupFeatures(fc.Object);
        //    sut.SettlementRegressor.SetupFeatures(fc.Object);
        //    sut.Finish(fc.Object);
        //    foreach (var ar in sut.AverageRegressors)
        //        ar.Finish(fc.Object);
        //    sut.SettlementRegressor.Finish(fc.Object);
        //    sut.Process(b);

        //    Assert.Equal(0.0, sut.AverageResult);
        //    Assert.Equal(0.0, sut.ResultStdError);

        //    Assert.True(sut.ResultsByPath.All(x => x == 0.0));

        //}
    }
}
