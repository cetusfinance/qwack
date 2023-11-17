using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Core.Tests.Instruments
{
    public class AsianBasisSwapFacts
    {
        [Fact]
        public void AsianBasisSwapFact()
        {
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var fixDates = new[] { DateTime.Today };
            var x = new AsianBasisSwap
            {
                PaySwaplets = new[] { new AsianSwap { AssetId = "CL", PaymentCurrency = usd, DiscountCurve = "X", FixingDates = fixDates, Strike = -6 } },
                RecSwaplets = new[] { new AsianSwap { AssetId = "QS", PaymentCurrency = usd, DiscountCurve = "X", FixingDates = fixDates } },
            };

            //strike is leg2 premium in leg1 units

            var fakeModel = new Mock<IAssetFxModel>();
            var c = new ConstantPriceCurve(100, DateTime.Today, TestProviderHelper.CurrencyProvider) { Currency = usd };
            fakeModel.Setup(xx => xx.GetPriceCurve(It.IsAny<string>(), null)).Returns(c);

            Assert.Equal(usd, x.Currency);
            Assert.Equal(usd, x.PaymentCurrency);
            var a = x.AssetIds;
            Assert.Contains("CL", a);
            Assert.Contains("QS", a);
            Assert.Single(x.IrCurves(fakeModel.Object));
            Assert.Equal(FxConversionType.None, x.FxType(fakeModel.Object));
            Assert.Equal(string.Empty, x.FxPair(fakeModel.Object));

            Assert.Equal(6, x.Strike);
            var y = (AsianBasisSwap)x.SetStrike(7);
            Assert.Equal(6, x.Strike);
            Assert.Equal(7, y.Strike);

            var pf = x.PastFixingDates(DateTime.Today.AddDays(1));
            Assert.Contains("CL", pf.Keys);
            Assert.Contains("QS", pf.Keys);

            Assert.True(x.Equals(x));
            Assert.False(x.Equals(y));
        }
    }
}
