using System;
using Xunit;
using Qwack.Paths.Payoffs;
using Qwack.Core.Basic;
using System.Linq;
using static Qwack.MonteCarlo.Test.Payoffs.Helpers;

namespace Qwack.MonteCarlo.Test.Payoffs
{
    public class OneTouchFacts
    {
        [Fact]
        public void CanProcess()
        {
            var t = new DateTime(2019, 10, 03);
            var fc = GetFeatureCollection();
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var sut = new OneTouch("Asset", t, t.AddDays(100), 101, "boo", usd, t.AddDays(101), 1.0, BarrierSide.Up, BarrierType.In);
            var b = GetBlock(20);

            sut.SetupFeatures(fc.Object);
            sut.Finish(fc.Object);
            sut.Process(b);

            Assert.Equal(0.0, sut.AverageResult);
            Assert.Equal(0.0, sut.ResultStdError);

            Assert.True(sut.ResultsByPath.All(x => x == 0.0));

            sut = new OneTouch("Asset", t, t.AddDays(100), 101, "boo", usd, t.AddDays(101), 3.0, BarrierSide.Down, BarrierType.In);
            b = GetBlock(20);

            sut.SetupFeatures(fc.Object);
            sut.Finish(fc.Object);
            sut.Process(b);

            Assert.Equal(3.0, sut.AverageResult);
            Assert.Equal(0.0, sut.ResultStdError);

            Assert.True(sut.ResultsByPath.All(x => x == 3.0));

            sut = new OneTouch("Asset", t, t.AddDays(100), 101, "boo", usd, t.AddDays(101), 3.0, BarrierSide.Down, BarrierType.Out);
            b = GetBlock(20);

            sut.SetupFeatures(fc.Object);
            sut.Finish(fc.Object);
            sut.Process(b);

            Assert.Equal(0.0, sut.AverageResult);
            Assert.Equal(0.0, sut.ResultStdError);

            Assert.True(sut.ResultsByPath.All(x => x == 0.0));


            sut = new OneTouch("Asset", t, t.AddDays(100), 101, "boo", usd, t.AddDays(101), 3.0, BarrierSide.Up, BarrierType.Out);
            b = GetBlock(20);

            sut.SetupFeatures(fc.Object);
            sut.Finish(fc.Object);
            sut.Process(b);

            Assert.Equal(3.0, sut.AverageResult);
            Assert.Equal(0.0, sut.ResultStdError);

            Assert.True(sut.ResultsByPath.All(x => x == 3.0));
        }
    }
}
