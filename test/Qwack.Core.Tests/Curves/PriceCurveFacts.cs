using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Curves;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Tests.Curves
{
    public class PriceCurveFacts
    {
        [Fact]
        public void PriceCurveFact()
        {
            var origin = new DateTime(2019, 05, 28);

            var z1 = new BasicPriceCurve(origin, new[] { new DateTime(2019, 05, 31), new DateTime(2019, 06, 30) }, new[] { 100.0, 110.0 }, PriceCurveType.NYMEX, TestProviderHelper.CurrencyProvider);
            Assert.Equal(2, z1.NumberOfPillars);
            var z1r = z1.RebaseDate(origin.AddDays(10));
            Assert.Single(z1r.PillarDates);
            Assert.Equal(1, z1r.NumberOfPillars);
            Assert.Equal(110, z1r.GetPriceForFixingDate(origin));

            var z2 = new BasicPriceCurve(origin, new[] { new DateTime(2019, 05, 31), new DateTime(2019, 06, 30) }, new[] { 100.0, 110.0 }, PriceCurveType.ICE, TestProviderHelper.CurrencyProvider);
            var z2r = z2.RebaseDate(origin.AddDays(10));
            Assert.Single(z1r.PillarDates);

            var z3 = new BasicPriceCurve(origin, new[] { new DateTime(2019, 05, 31), new DateTime(2019, 06, 30) }, new[] { 100.0, 110.0 }, PriceCurveType.LME, TestProviderHelper.CurrencyProvider);
            var z3r = z3.RebaseDate(origin.AddDays(10));
            Assert.Single(z1r.PillarDates);
        }

        [Fact]
        public void SparsePriceCurveFact()
        {
            var origin = new DateTime(2019, 05, 28);
            var pillars = new[] {
                new DateTime(2019, 12, 31),
                new DateTime(2020, 12, 31),
                new DateTime(2021, 12, 31)
            };
            var prices = new[] { 100.0, 90.0, 85.0 };
            var z = new SparsePriceCurve(origin, pillars, prices, SparsePriceCurveType.Coal, TestProviderHelper.CurrencyProvider, new[] { "A", "B", "C" });

            Assert.Throws<NotImplementedException>(() => z.CurveType);
            Assert.Same(pillars, z.PillarDates);
            Assert.Equal(pillars.Length, z.NumberOfPillars);
            Assert.True(z.UnderlyingsAreForwards);
            Assert.Equal(100, z.GetPriceForDate(pillars[0].AddDays(-10)));
            Assert.Equal(100, z.GetPriceForFixingDate(pillars[0].AddDays(-10)));

            var ds = z.GetDeltaScenarios(0.1, null);
            Assert.True(ds.ContainsKey("A"));
            Assert.True(ds.ContainsKey("B"));
            Assert.True(ds.ContainsKey("C"));

            Assert.Equal(pillars[1], z.PillarDatesForLabel("B"));

            var zr = z.RebaseDate(origin.AddDays(1));
            Assert.Equal(100, zr.GetPriceForDate(pillars[0].AddDays(-10)));

            zr = z.RebaseDate(pillars[0].AddDays(1));
            Assert.Equal(90, zr.GetPriceForDate(pillars[0].AddDays(-10)));
        }
    }
}
