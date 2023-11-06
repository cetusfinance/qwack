using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Models.Calibrators;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Math.Interpolation;
using Qwack.Math.Utils;
using Qwack.Providers.Json;
using Xunit;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Tests.CurveSolving
{
    public class AssetBasisCurveFact
    {
        bool IsCoverageOnly => bool.TryParse(Environment.GetEnvironmentVariable("CoverageOnly"), out var coverageOnly) && coverageOnly;

        [Fact]
        public void StripCrackedCurve()
        {
            var buildDate = new DateTime(2018, 07, 28);
            string[] futures = { "COV8", "COX8", "COZ8", "COF9", "COG9" };
            double[] futuresPrices = { 77, 78, 79, 79.5, 79.75 };
            string[] periods = { "AUG18", "SEP18", "OCT18", "NOV18" };
            double[] strikes = { -10, -11, -12, -13 };

            var cal = TestProviderHelper.CalendarProvider.Collection["LON"];
            var usd = TestProviderHelper.CurrencyProvider["USD"];

            var bPillars = futures.Select(x => FutureCode.GetExpiryFromCode(x, TestProviderHelper.FutureSettingsProvider)).ToArray();
            var brentCurve = new BasicPriceCurve(buildDate, bPillars, futuresPrices, PriceCurveType.ICE, TestProviderHelper.CurrencyProvider, futures)
            {
                AssetId = "Brent"
            };


            var instruments = periods.Select((p, ix) =>
            (IAssetInstrument)AssetProductFactory.CreateTermAsianBasisSwap(p, strikes[ix], SwapPayReceiveType.Payer, "Brent", "Sing180", cal, cal, cal, 0.Bd(), usd, 0.Bd(), 0.Bd(), 1000, 1000 / 6.35))
            .ToList();
            var pillars = instruments.Select(x => ((AsianBasisSwap)x).RecSwaplets.Max(sq => sq.AverageEndDate)).ToList();

            DateTime[] dPillars = { buildDate, buildDate.AddDays(1000) };
            double[] dRates = { 0, 0 };
            var discountCurve = new IrCurve(dPillars, dRates, buildDate, "zeroDiscount", Interpolator1DType.LinearFlatExtrap, usd);

            var s = new NewtonRaphsonAssetBasisCurveSolver(TestProviderHelper.CurrencyProvider);
            if (IsCoverageOnly)
                s.Tollerance = 1.0;

            var curve = s.SolveCurve(instruments, pillars, discountCurve, brentCurve, buildDate, PriceCurveType.ICE);

            if (!IsCoverageOnly)
                for (var i = 0; i < instruments.Count; i++)
                {
                    var resultPV = NewtonRaphsonAssetBasisCurveSolver.BasisSwapPv(curve, instruments[i], discountCurve, brentCurve);
                    Assert.Equal(0, resultPV, 6);
                }
        }

        [Fact]
        public void StripCrackedCurveSimple()
        {
            var buildDate = new DateTime(2018, 07, 28);
            string[] periods = { "AUG18" };
            double[] strikes = { -10 };

            var cal = TestProviderHelper.CalendarProvider.Collection["LON"];
            var usd = TestProviderHelper.CurrencyProvider["USD"];

            var bPillars = new[] { buildDate, buildDate.AddDays(100) };
            var brentCurve = new BasicPriceCurve(buildDate, bPillars, new[] { 100.0, 100.0 }, PriceCurveType.Linear, TestProviderHelper.CurrencyProvider)
            {
                AssetId = "Brent"
            };

            var instruments = periods.Select((p, ix) =>
            (IAssetInstrument)AssetProductFactory.CreateTermAsianBasisSwap(p, strikes[ix], SwapPayReceiveType.Payer, "Brent", "Sing180", cal, cal, cal, 0.Bd(), usd, 0.Bd(), 0.Bd(), 1000, 1000 / 6.35))
            .ToList();
            var pillars = instruments.Select(x => ((AsianBasisSwap)x).RecSwaplets.Max(sq => sq.AverageEndDate)).ToList();

            DateTime[] dPillars = { buildDate, buildDate.AddDays(1000) };
            double[] dRates = { 0, 0 };
            var discountCurve = new IrCurve(dPillars, dRates, buildDate, "zeroDiscount", Interpolator1DType.LinearFlatExtrap, usd);

            var s = new NewtonRaphsonAssetBasisCurveSolver(TestProviderHelper.CurrencyProvider);
            var curve = s.SolveCurve(instruments, pillars, discountCurve, brentCurve, buildDate, PriceCurveType.Linear);

            Assert.Equal((100.0 - 10.0) * 6.35, curve.GetPriceForDate(buildDate));
            Assert.Equal((100.0 - 10.0) * 6.35, curve.GetPriceForDate(buildDate.AddDays(50)));
        }

        [Fact]
        public void StripCrackedCurveAsBasisCurve()
        {
            var buildDate = new DateTime(2018, 07, 28);
            string[] futures = { "COV8", "COX8", "COZ8", "COF9", "COG9" };
            double[] futuresPrices = { 77, 78, 79, 79.5, 79.75 };
            string[] periods = { "AUG18", "SEP18", "OCT18", "NOV18" };
            double[] strikes = { -10, -11, -12, -13 };

            var cal = TestProviderHelper.CalendarProvider.Collection["LON"];
            var usd = TestProviderHelper.CurrencyProvider["USD"];

            var bPillars = futures.Select(x => FutureCode.GetExpiryFromCode(x, TestProviderHelper.FutureSettingsProvider)).ToArray();
            var brentCurve = new BasicPriceCurve(buildDate, bPillars, futuresPrices, PriceCurveType.ICE, TestProviderHelper.CurrencyProvider, futures)
            {
                AssetId = "Brent"
            };


            var instruments = periods.Select((p, ix) =>
            (IAssetInstrument)AssetProductFactory.CreateTermAsianBasisSwap(p, strikes[ix], SwapPayReceiveType.Payer, "Brent", "Sing180", cal, cal, cal, 0.Bd(), usd, 0.Bd(), 0.Bd(), 1000, 1000 / 6.35))
            .ToList();
            var pillars = instruments.Select(x => ((AsianBasisSwap)x).RecSwaplets.Max(sq => sq.AverageEndDate)).ToList();

            DateTime[] dPillars = { buildDate, buildDate.AddDays(1000) };
            double[] dRates = { 0, 0 };
            var discountCurve = new IrCurve(dPillars, dRates, buildDate, "zeroDiscount", Interpolator1DType.LinearFlatExtrap, usd);

            var curve = new BasisPriceCurve(instruments, pillars, discountCurve, brentCurve, buildDate, PriceCurveType.NYMEX, new NewtonRaphsonAssetBasisCurveSolver(TestProviderHelper.CurrencyProvider));

            for (var i = 0; i < instruments.Count; i++)
            {
                var resultPV = NewtonRaphsonAssetBasisCurveSolver.BasisSwapPv(curve, instruments[i], discountCurve, brentCurve);
                Assert.Equal(0, resultPV, 6);
            }

            var curve2 = curve.Clone();
            for (var i = 0; i < instruments.Count; i++)
            {
                var resultPV = NewtonRaphsonAssetBasisCurveSolver.BasisSwapPv(curve2, instruments[i], discountCurve, brentCurve);
                Assert.Equal(0, resultPV, 6);
            }
        }
    }
}
