using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Curves;

namespace Qwack.Core.Tests.Curves
{
    public class FactorPriceCurveFacts
    {
        [Fact]
        public void FactorPriceCurveFact()
        {
            var baseCurve = new ConstantPriceCurve(100, DateTime.Today, TestProviderHelper.CurrencyProvider)
            {
                AssetId = "wooo",
                Currency = TestProviderHelper.CurrencyProvider.GetCurrency("USD"),
                CollateralSpec = "hooo",
                Name = "booo",
                SpotCalendar = TestProviderHelper.CalendarProvider.Collection["NYC"],
                SpotLag = new Dates.Frequency("0d")
            };

            var sut = new FactorPriceCurve(baseCurve, 3);
            Assert.Equal(300.0, sut.GetPriceForDate(DateTime.Today));
            Assert.Equal(300.0, sut.GetPriceForFixingDate(DateTime.Today));
            Assert.Equal(300.0, sut.GetAveragePriceForDates(new[] { DateTime.Today, DateTime.Today.AddDays(1) }));

            Assert.Equal(baseCurve.AssetId, sut.AssetId);
            Assert.Equal(baseCurve.Currency, sut.Currency);
            Assert.Equal(baseCurve.Name, sut.Name);
            Assert.Equal(baseCurve.SpotCalendar, sut.SpotCalendar);
            Assert.Equal(baseCurve.SpotLag, sut.SpotLag);
            Assert.Equal(baseCurve.CurveType, sut.CurveType);
            Assert.Equal(baseCurve.PillarDates, sut.PillarDates);
            Assert.Equal(baseCurve.NumberOfPillars, sut.NumberOfPillars);
            Assert.Equal(baseCurve.BuildDate, sut.BuildDate);
            Assert.Equal(baseCurve.UnderlyingsAreForwards, sut.UnderlyingsAreForwards);
            Assert.Equal(baseCurve.PillarDatesForLabel(DateTime.Today.ToString("yyyy-MM-dd")), sut.PillarDatesForLabel(DateTime.Today.ToString("yyyy-MM-dd")));

            Assert.Throws<NotImplementedException>(() => sut.RebaseDate(DateTime.Today));
            Assert.Throws<NotImplementedException>(() => sut.Name = "yooooo");
            Assert.Throws<NotImplementedException>(() => sut.Currency = null);
            Assert.Throws<NotImplementedException>(() => sut.SpotCalendar = null);
            Assert.Throws<NotImplementedException>(() => sut.SpotLag = new Dates.Frequency());
            Assert.Throws<NotImplementedException>(() => sut.GetDeltaScenarios(0.0, DateTime.Today));
        }
    }
}
