using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Curves;
using System.Linq;

namespace Qwack.Core.Tests.Curves
{
    public class ContangoPriceCurveFacts
    {
        [Fact]
        public void ContangoPriceCurveFact()
        {
            var pillars = new[] { DateTime.Today.AddDays(100), DateTime.Today.AddDays(200) };
            var rates = new[] { 0.1, 0.2 };
            var sut = new ContangoPriceCurve(DateTime.Today, 100.0, DateTime.Today, pillars, rates, TestProviderHelper.CurrencyProvider);

            Assert.Equal(PriceCurveType.Linear, sut.CurveType);
            Assert.False(sut.UnderlyingsAreForwards);
            Assert.True(Enumerable.SequenceEqual(pillars, sut.PillarDates));
            Assert.Equal(pillars.Length, sut.NumberOfPillars);

            Assert.Equal(100.0 * (1.0 + 100.0 / 360.0 * 0.1), sut.GetPriceForDate(pillars[0]));
            Assert.Equal(100.0 * (1.0 + 100.0 / 360.0 * 0.1), sut.GetAveragePriceForDates(new[] { pillars[0] }));
            Assert.Equal(100.0 * (1.0 + 150.0 / 360.0 * 0.15), sut.GetPriceForDate(DateTime.Today.AddDays(150)));

            Assert.Equal(DateTime.Today, sut.PillarDatesForLabel("Spot"));
            Assert.Equal(DateTime.Today.AddDays(100), sut.PillarDatesForLabel(DateTime.Today.AddDays(100).ToString("yyyy-MM-dd")));

            var rb = sut.RebaseDate(DateTime.Today.AddDays(1));
            Assert.Equal(DateTime.Today.AddDays(1), rb.BuildDate);

            var ds = sut.GetDeltaScenarios(0.1, null);
            Assert.Equal("Spot", ds.First().Key);
        }
    }
}
