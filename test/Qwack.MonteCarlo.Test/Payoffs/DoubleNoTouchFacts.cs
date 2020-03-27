using System;
using Xunit;
using Qwack.Paths.Payoffs;
using Qwack.Core.Basic;
using System.Linq;
using static Qwack.MonteCarlo.Test.Payoffs.Helpers;

namespace Qwack.MonteCarlo.Test.Payoffs
{
    public class DoubleNoTouchFacts
    {
        [Fact]
        public void CanProcess()
        {
            var t = new DateTime(2019, 10, 03);
            var fc = GetFeatureCollection();
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");

            //no hit, double knock in
            var sut = new DoubleNoTouch("Asset", t, t.AddDays(100), 99, 101, "boo", usd, t.AddDays(101), 1.0, BarrierType.In, usd);
            var b = GetBlock(20);

            sut.SetupFeatures(fc.Object);
            sut.Finish(fc.Object);
            sut.Process(b);

            Assert.Equal(0.0, sut.AverageResult);
            Assert.Equal(0.0, sut.ResultStdError);

            Assert.True(sut.ResultsByPath.All(x => x == 0.0));

            //no hit, double knock out
            sut = new DoubleNoTouch("Asset", t, t.AddDays(100), 99, 101, "boo", usd, t.AddDays(101), 3.0, BarrierType.Out, usd);
            b = GetBlock(20);

            sut.SetupFeatures(fc.Object);
            sut.Finish(fc.Object);
            sut.Process(b);

            Assert.Equal(3.0, sut.AverageResult);
            Assert.Equal(0.0, sut.ResultStdError);

            Assert.True(sut.ResultsByPath.All(x => x == 3.0));

            //one hit, double knock in
            sut = new DoubleNoTouch("Asset", t, t.AddDays(100), 101, 103, "boo", usd, t.AddDays(101), 3.0, BarrierType.In, usd);
            b = GetBlock(20);

            sut.SetupFeatures(fc.Object);
            sut.Finish(fc.Object);
            sut.Process(b);

            Assert.Equal(0.0, sut.AverageResult);
            Assert.Equal(0.0, sut.ResultStdError);

            Assert.True(sut.ResultsByPath.All(x => x == 0.0));

            //one hit, double knock out
            sut = new DoubleNoTouch("Asset", t, t.AddDays(100), 101, 103, "boo", usd, t.AddDays(101), 3.0, BarrierType.Out, usd);
            b = GetBlock(20);

            sut.SetupFeatures(fc.Object);
            sut.Finish(fc.Object);
            sut.Process(b);

            Assert.Equal(0.0, sut.AverageResult);
            Assert.Equal(0.0, sut.ResultStdError);

            Assert.True(sut.ResultsByPath.All(x => x == 0.0));
        }
    }
}
