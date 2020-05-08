using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Core.Tests.Instruments
{
    public class AsianSwapStripFacts
    {
        [Fact]
        public void AsianSwapStripFact()
        {
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var fixDates = new[] { DateTime.Today };
            var x = new AsianSwapStrip
            {
                Swaplets = new[] { new AsianSwap { AssetId = "CL", PaymentCurrency = usd, DiscountCurve = "X", FixingDates = fixDates,Strike = 6, Notional=1 } },
            };

            var fakeModel = new Mock<IAssetFxModel>();
            var c = new ConstantPriceCurve(100, DateTime.Today, TestProviderHelper.CurrencyProvider) { Currency = usd };
            fakeModel.Setup(xx => xx.GetPriceCurve(It.IsAny<string>(), null)).Returns(c);

            Assert.Equal(usd, x.Currency);
            Assert.Equal(usd, x.PaymentCurrency);
            var a = x.AssetIds;
            Assert.Contains("CL", a);
            Assert.Single(x.IrCurves(fakeModel.Object));
            Assert.Equal(FxConversionType.None, x.FxType(fakeModel.Object));

            Assert.Equal(6, x.Swaplets.First().Strike);
            var y = (AsianSwapStrip)x.SetStrike(7);
            Assert.Equal(6, x.Swaplets.First().Strike);
            Assert.Equal(7, y.Swaplets.First().Strike);

            var pf = x.PastFixingDates(DateTime.Today.AddDays(1));
            Assert.Contains("CL", pf.Keys);

            var z = (AsianSwapStrip)x.Clone();
            Assert.True(x.Equals(z));
            z.TradeId = "zzz";
            Assert.False(x.Equals(z));

            Assert.Equal(1.0, x.SupervisoryDelta(fakeModel.Object));
        }
    }
}
