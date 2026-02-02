using System;
using Xunit;
using Qwack.Paths.Payoffs;
using System.Linq;
using static Qwack.MonteCarlo.Test.Payoffs.Helpers;
using Qwack.Transport.BasicTypes;

namespace Qwack.MonteCarlo.Test.Payoffs
{
    public class LookBackOptionFacts
    {
        //[Fact]
        //public void CanProcess()
        //{
        //    var t = new DateTime(2019, 10, 03);
        //    var fc = GetFeatureCollection();
        //    var dates = Enumerable.Range(10, 10).Select(i => t.AddDays(i)).ToList();
        //    var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
        //    var sut = new LookBackOptionPP("Asset", dates, OptionType.C, "boo", usd, t.AddDays(100), 1.0, usd, dates.Last(), [dates.Last()]);
        //    var b = GetBlock(20);

        //    sut.SetupFeatures(fc.Object);
        //    sut.Finish(fc.Object);
        //    sut.Process(b);

        //    Assert.Equal(0.0, sut.AverageResult);
        //    Assert.Equal(0.0, sut.ResultStdError);

        //    Assert.True(sut.ResultsByPath.All(x => x == 0.0));

        //}
    }
}
