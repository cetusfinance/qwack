using System;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Models.Calibrators;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Core.Tests.CurveSolving
{
    public class AssetCoalCurveFact
    {
        bool IsCoverageOnly => bool.TryParse(Environment.GetEnvironmentVariable("CoverageOnly"), out var coverageOnly) && coverageOnly;

        [Fact]
        public void StripCoalSparse()
        {
            var startDate = new DateTime(2018, 07, 28);
            string[] periods = { "AUG18", "SEP18", "Q4-18", "Q1-19", "Q2-19", "H2-19", "CAL-20", "CAL-21" };
            double[] strikes = { 100, 99, 98, 97, 96, 95, 94, 93 };

            var cal = TestProviderHelper.CalendarProvider.Collection["LON"];
            
            var xaf = TestProviderHelper.CurrencyProvider["XAF"];
            

            var instruments = periods.Select((p, ix) =>
            AssetProductFactory.CreateMonthlyAsianSwap(p, strikes[ix], "coalXXX", cal, cal, 0.Bd(), xaf, TradeDirection.Long, 0.Bd(), 1, DateGenerationType.Fridays)
            ).ToList();
            var pillars = instruments.Select(x => x.Swaplets.Max(sq => sq.AverageEndDate)).ToList();

            DateTime[] dPillars = { startDate, startDate.AddDays(1000) };
            double[] dRates = { 0, 0 };
            var discountCurve = new IrCurve(dPillars, dRates, startDate, "zeroDiscount", Interpolator1DType.LinearFlatExtrap, xaf);

            var s = new NewtonRaphsonAssetCurveSolver()
            {
                Tollerance = IsCoverageOnly ? 1 : 0.00000001
            };
            var curve = s.Solve(instruments, pillars, discountCurve, startDate, TestProviderHelper.CurrencyProvider);

            if (!IsCoverageOnly)
            {
                for (var i = 0; i < instruments.Count; i++)
                {
                    var resultPV = NewtonRaphsonAssetCurveSolver.SwapPv(curve, instruments[i], discountCurve);
                    Assert.Equal(0, resultPV, 6);
                }
            }
        }

    }
}
