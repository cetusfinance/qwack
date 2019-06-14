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
    public class FutureOptionFacts
    {
        [Fact]
        public void FutureOptionFact()
        {
            var orgin = new DateTime(2019, 06, 12);
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var fixDates = new[] { orgin };
            var x = new FuturesOption()
            {
                AssetId = "QS",
                ExpiryDate = orgin,
                Strike = 1000,
                ContractQuantity = 1,
                LotSize = 1,
                Currency = usd,
                PriceMultiplier = 1.0,
                CallPut = OptionType.C
            };

            var fakeModel = new Mock<IAssetFxModel>();
            var c = new ConstantPriceCurve(100, DateTime.Today, TestProviderHelper.CurrencyProvider) { Currency = usd };
            fakeModel.Setup(xx => xx.GetPriceCurve(It.IsAny<string>(), null)).Returns(c);
            fakeModel.Setup(xx => xx.BuildDate).Returns(DateTime.Today);

            Assert.Equal(usd, x.Currency);
            Assert.Equal(usd, x.PaymentCurrency);
            var a = x.AssetIds;
            Assert.Contains("QS", a);
            Assert.Empty(x.IrCurves(fakeModel.Object));
            Assert.Equal(FxConversionType.None, x.FxType(fakeModel.Object));
            Assert.Equal(orgin, x.LastSensitivityDate);
            Assert.Empty(x.PastFixingDates(orgin.AddDays(1)));

            var y = (FuturesOption)x.Clone();
            Assert.True(x.Equals(y));
            y.TradeId = "xxx";
            Assert.False(x.Equals(y));

            var z = (FuturesOption)x.SetStrike(0);
            Assert.Equal(0, z.Strike);

        }
    }
}
