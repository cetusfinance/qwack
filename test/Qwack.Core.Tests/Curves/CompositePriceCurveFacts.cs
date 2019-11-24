using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Curves;
using Qwack.Models;
using Qwack.Core.Basic;

namespace Qwack.Core.Tests.Curves
{
    public class CompositePriceCurveFacts
    {
        [Fact]
        public void CompositePriceCurveFact()
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
            var baseCurve = new ConstantPriceCurve(100.0, originDate, TestProviderHelper.CurrencyProvider)
            {
                Currency = usd,
                Name="gooo",
                AssetId = "rooo"
            };

            var sut = new CompositePriceCurve(originDate, baseCurve, fModel, zar);
            var sut2 = new CompositePriceCurve(originDate, () => baseCurve, () => fModel, zar);

            var fxOrigin = fModel.GetFxRate(originDate, usd, zar);
            Assert.Equal(baseCurve.GetPriceForDate(originDate) * fxOrigin, sut.GetPriceForDate(originDate));
            Assert.Equal(baseCurve.GetPriceForDate(originDate) * fxOrigin, sut2.GetPriceForDate(originDate));
            Assert.Equal(baseCurve.GetPriceForDate(originDate) * fxOrigin, sut.GetPriceForFixingDate(originDate));
            Assert.Equal(baseCurve.GetPriceForDate(originDate) * fxOrigin, sut.GetAveragePriceForDates(new[] { originDate }));

            Assert.Equal("gooo", sut.Name);
            Assert.Throws<Exception>(() => sut.Name = null);
            Assert.Single(sut.GetDeltaScenarios(0.0, null));
            Assert.Equal(PriceCurveType.Linear, sut.CurveType);
            Assert.False(sut.UnderlyingsAreForwards);
            Assert.Equal(1, sut.NumberOfPillars);
            Assert.Equal("rooo", sut.AssetId);
            Assert.Equal(zar, sut.CompoCurrency);
            Assert.Equal(zar, sut.Currency);
            Assert.Throws<Exception>(() => sut.Currency = null);
            Assert.Equal(usd, sut.CurveCurrency);
            Assert.Single(sut.PillarDates);

            Assert.Equal(DateTime.Today.AddDays(1), sut.RebaseDate(DateTime.Today.AddDays(1)).BuildDate);
            Assert.Equal(originDate, sut.PillarDatesForLabel(originDate.ToString("yyyy-MM-dd")));
            Assert.Throws<Exception>(() => sut.PillarDatesForLabel(""));


        }
    }
}
