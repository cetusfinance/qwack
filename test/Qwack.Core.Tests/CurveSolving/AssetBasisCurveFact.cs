using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Math.Interpolation;
using Qwack.Math.Utils;
using Qwack.Providers.Json;
using Xunit;

namespace Qwack.Core.Tests.CurveSolving
{
    public class AssetBasisCurveFact
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly string JsonFuturesPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "futuresettings.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        public static readonly IFutureSettingsProvider futureSettingsProvider = new FutureSettingsFromJson(CalendarProvider, JsonFuturesPath);

        [Fact]
        public void StripCrackedCurve()
        {
            var buildDate = new DateTime(2018, 07, 28);
            string[] futures = { "COV8", "COX8", "COZ8", "COF9", "COG9" };
            double[] futuresPrices = { 77, 78, 79, 79.5, 79.75 };
            string[] periods = { "AUG18", "SEP18", "OCT18", "NOV18" };
            double[] strikes = { -10, -11, -12, -13};

            var cal = CalendarProvider.Collection["LON"];
            var usd = new Currency("USD", DayCountBasis.Act365F, cal);

            var bPillars = futures.Select(x => FutureCode.GetExpiryFromCode(x, futureSettingsProvider)).ToArray();
            var brentCurve = new PriceCurve(buildDate, bPillars, futuresPrices, PriceCurveType.ICE, futures)
            {
                AssetId = "Brent"
            };


            var instruments = periods.Select((p, ix) =>
            AssetProductFactory.CreateTermAsianBasisSwap(p, strikes[ix], "Brent", "Sing180", cal, cal, cal, 0.Bd(), usd, 0.Bd(), 0.Bd(), 1000, 1000/6.35))
            .ToList();
            var pillars = instruments.Select(x => x.RecSwaplets.Max(sq => sq.AverageEndDate)).ToList();

            DateTime[] dPillars = { buildDate, buildDate.AddDays(1000) };
            double[] dRates = { 0, 0 };
            var discountCurve = new IrCurve(dPillars, dRates, buildDate, "zeroDiscount", Interpolator1DType.LinearFlatExtrap, usd);

            var s = new Calibrators.NewtonRaphsonAssetBasisCurveSolver();
            var curve = s.SolveCurve(instruments, pillars, discountCurve, brentCurve, buildDate, PriceCurveType.ICE);

            for (var i = 0; i < instruments.Count; i++)
            {
                var resultPV = Calibrators.NewtonRaphsonAssetBasisCurveSolver.BasisSwapPv(curve, instruments[i], discountCurve, brentCurve);
                Assert.Equal(0, resultPV, 6);
            }
        }

        [Fact]
        public void StripCrackedCurveSimple()
        {
            var buildDate = new DateTime(2018, 07, 28);
            string[] periods = { "AUG18" };
            double[] strikes = { -10 };

            var cal = CalendarProvider.Collection["LON"];
            var usd = new Currency("USD", DayCountBasis.Act365F, cal);

            var bPillars = new[] { buildDate, buildDate.AddDays(100) };
            var brentCurve = new PriceCurve(buildDate, bPillars, new[] { 100.0, 100.0 }, PriceCurveType.Linear)
            {
                AssetId = "Brent"
            };

            var instruments = periods.Select((p, ix) =>
            AssetProductFactory.CreateTermAsianBasisSwap(p, strikes[ix], "Brent", "Sing180", cal, cal, cal, 0.Bd(), usd, 0.Bd(), 0.Bd(), 1000, 1000 / 6.35))
            .ToList();
            var pillars = instruments.Select(x => x.RecSwaplets.Max(sq => sq.AverageEndDate)).ToList();

            DateTime[] dPillars = { buildDate, buildDate.AddDays(1000) };
            double[] dRates = { 0, 0 };
            var discountCurve = new IrCurve(dPillars, dRates, buildDate, "zeroDiscount", Interpolator1DType.LinearFlatExtrap, usd);

            var s = new Calibrators.NewtonRaphsonAssetBasisCurveSolver();
            var curve = s.SolveCurve(instruments, pillars, discountCurve, brentCurve, buildDate, PriceCurveType.Linear);

            Assert.Equal((100.0 - 10.0) * 6.35, curve.GetPriceForDate(buildDate));
            Assert.Equal((100.0 - 10.0) * 6.35, curve.GetPriceForDate(buildDate.AddDays(50)));
        }

    }
}
