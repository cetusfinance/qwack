using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Curves;
using Qwack.Models;
using Qwack.Core.Basic;

namespace Qwack.Core.Tests.Curves
{
    public class FxForwardCurveFacts
    {
        [Fact]
        public void FxForwardCurveFact()
        {
            var originDate = new DateTime(2019, 05, 28);
            var zar = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR");
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var zarCurve = new ConstantRateIrCurve(0.07, originDate, "ZARCurve", zar);
            var usdCurve = new ConstantRateIrCurve(0.02, originDate, "USDCurve", usd);
            var fModel = new FundingModel(originDate, new[] { zarCurve, usdCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            var fxPair = new FxPair { Domestic = zar, Foreign = usd, SpotLag = new Dates.Frequency("0d") };
            fxMatrix.Init(zar, originDate, new Dictionary<Currency, double> { { usd, 10.0 } }, new List<FxPair>() { fxPair }, new Dictionary<Currency, string> { { usd, "USDCurve" }, { zar, "ZARCurve" } });
            fModel.SetupFx(fxMatrix);
            var sut = new FxForwardCurve(originDate, fModel, zar, usd)
            {
                Name = "gooo",
            };
            var sut2 = new FxForwardCurve(originDate, new Func<Models.IFundingModel>(() => fModel), zar, usd);

            Assert.Equal(10, sut.GetPriceForDate(originDate));
            Assert.Equal(10, sut2.GetPriceForDate(originDate));
            Assert.Equal(10, sut.GetPriceForFixingDate(originDate));
            Assert.Equal(10, sut.GetAveragePriceForDates(new[] { originDate }));

            Assert.Equal("gooo", sut.Name);
            Assert.Equal(new Dictionary<string,IPriceCurve>(), sut.GetDeltaScenarios(0.0,null));
            Assert.Equal(PriceCurveType.Linear, sut.CurveType);
            Assert.True(sut.UnderlyingsAreForwards);
            Assert.Equal(0, sut.NumberOfPillars);
            Assert.Equal(usd.Ccy, sut.AssetId);
            Assert.Null(sut.PillarDates);

            Assert.Throws<NotImplementedException>(() => sut.RebaseDate(DateTime.Today));
            Assert.Throws<NotImplementedException>(() => sut.PillarDatesForLabel(""));
            Assert.Throws<Exception>(() => sut.Currency = null);
        }
    }
}
