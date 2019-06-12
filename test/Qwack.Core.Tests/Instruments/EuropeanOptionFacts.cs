using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Xunit;

namespace Qwack.Core.Tests.Instruments
{
    public class EuropeanOptionFacts
    {
        [Fact]
        public void EuropeanOptionFact()
        {
            var orgin = new DateTime(2019, 06, 12);
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var fixDates = new[] { orgin };
            var x = new EuropeanOption()
            {
                AssetId = "QS",
                DiscountCurve = "X",
                ExpiryDate = orgin,
                FxConversionType = FxConversionType.None,
                Notional = 1,
                PaymentCurrency = usd,
                PaymentDate = orgin,
                SpotLag = 0.Bd(),
                Strike = 1000,
            };

            var fakeModel = new Mock<IAssetFxModel>();
            var c = new ConstantPriceCurve(100, DateTime.Today, TestProviderHelper.CurrencyProvider) { Currency = usd };
            fakeModel.Setup(xx => xx.GetPriceCurve(It.IsAny<string>(), null)).Returns(c);
            fakeModel.Setup(xx => xx.BuildDate).Returns(DateTime.Today);

            Assert.Equal(usd, x.Currency);
            Assert.Equal(usd, x.PaymentCurrency);
            var a = x.AssetIds;
            Assert.Contains("QS", a);
            Assert.Single(x.IrCurves(fakeModel.Object));
            Assert.Equal(FxConversionType.None, x.FxType(fakeModel.Object));
            Assert.Equal(orgin, x.LastSensitivityDate);

            var pf = x.PastFixingDates(orgin.AddDays(1));
            Assert.Contains("QS", pf.Keys);

            Assert.True(x == x);
            var y = (EuropeanOption)x.Clone();
            y.TradeId = "xxx";
            Assert.False(x == y);

            var z = (EuropeanOption)x.SetStrike(0);
            Assert.Equal(0, z.Strike);

        }
    }
}
