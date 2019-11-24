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
    public class ForwardFacts
    {
        [Fact]
        public void ForwardFact()
        {
            var orgin = new DateTime(2019, 06, 12);
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var zar = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR");
            var fixDates = new[] { orgin };
            var x = new Forward()
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
            var xFx = new Forward()
            {
                AssetId = "USD/ZAR",
                DiscountCurve = "Y",
                ExpiryDate = orgin,
                FxConversionType = FxConversionType.None,
                Notional = 1,
                PaymentCurrency = usd,
                PaymentDate = orgin,
                SpotLag = 0.Bd(),
                Strike = 1000,
            };
            var x2 = new Forward()
            {
                AssetId = "QS",
                DiscountCurve = "X",
                ExpiryDate = orgin,
                FxConversionType = FxConversionType.AverageThenConvert,
                Notional = 1,
                PaymentCurrency = zar,
                PaymentDate = orgin,
                SpotLag = 0.Bd(),
                Strike = 1000,
            };

            var fakeModel = new Mock<IAssetFxModel>();
            var c = new ConstantPriceCurve(100, DateTime.Today, TestProviderHelper.CurrencyProvider) { Currency = usd };
            fakeModel.Setup(xx => xx.GetPriceCurve(It.IsAny<string>(), null)).Returns(c);
            fakeModel.Setup(xx => xx.BuildDate).Returns(DateTime.Today);
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, orgin, new Dictionary<Currency, double>(), new List<FxPair>(), new Dictionary<Currency, string> { { usd, "X" }, { zar, "Y" } });
            var fModel = new Mock<IFundingModel>();
            fModel.Setup(xx => xx.FxMatrix).Returns(fxMatrix);
            fakeModel.Setup(xx => xx.FundingModel).Returns(fModel.Object);

            Assert.Equal(usd, x.Currency);
            Assert.Equal(usd, x.PaymentCurrency);
            var a = x.AssetIds;
            Assert.Contains("QS", a);

            Assert.Single(x.IrCurves(fakeModel.Object));
            var ir2 = xFx.IrCurves(fakeModel.Object);
            Assert.Contains("X", ir2);
            Assert.Contains("Y", ir2);
            ir2 = x2.IrCurves(fakeModel.Object);
            Assert.Contains("X", ir2);
            Assert.Contains("Y", ir2);

            Assert.Equal(string.Empty, x.FxPair(fakeModel.Object));
            Assert.Equal("USD/ZAR", x2.FxPair(fakeModel.Object));

            Assert.Equal(FxConversionType.None, x.FxType(fakeModel.Object));
            Assert.Equal(orgin, x.LastSensitivityDate);

            var pf = x.PastFixingDates(orgin.AddDays(1));
            Assert.Contains("QS", pf.Keys);

            var y = (Forward)x.Clone();
            Assert.True(x.Equals(y));
            y.TradeId = "xxx";
            Assert.False(x.Equals(y));

            var z = (Forward)x.SetStrike(0);
            Assert.Equal(0, z.Strike);

            Assert.Equal(1.0, z.SupervisoryDelta(fakeModel.Object));
        }
    }
}
