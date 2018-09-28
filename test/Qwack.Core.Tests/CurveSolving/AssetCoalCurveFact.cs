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
using Qwack.Math.Interpolation;
using Qwack.Math.Utils;
using Qwack.Providers.Json;
using Xunit;

namespace Qwack.Core.Tests.CurveSolving
{
    public class AssetCoalCurveFact
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly string JsonCurrencyPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Currencies.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        public static readonly ICurrencyProvider CurrencyProvider = ProvideCurrencies(CalendarProvider);

        private static ICurrencyProvider ProvideCurrencies(ICalendarProvider calendarProvider)
        {
            var currencyProvider = new CurrenciesFromJson(calendarProvider, JsonCurrencyPath);
            return currencyProvider;
        }

        [Fact]
        public void StripCoalSparse()
        {
            var startDate = new DateTime(2018, 07, 28);
            string[] periods = { "AUG18", "SEP18", "Q4-18", "Q1-19", "Q2-19", "H2-19", "CAL-20", "CAL-21" };
            double[] strikes = { 100, 99, 98, 97, 96, 95, 94, 93 };

            var cal = CalendarProvider.Collection["LON"];
            
            var xaf = CurrencyProvider["XAF"];
            

            var instruments = periods.Select((p, ix) =>
            AssetProductFactory.CreateMonthlyAsianSwap(p, strikes[ix], "coalXXX", cal, cal, 0.Bd(), xaf, TradeDirection.Long, 0.Bd(), 1, DateGenerationType.Fridays)
            ).ToList();
            var pillars = instruments.Select(x => x.Swaplets.Max(sq => sq.AverageEndDate)).ToList();

            DateTime[] dPillars = { startDate, startDate.AddDays(1000) };
            double[] dRates = { 0, 0 };
            var discountCurve = new IrCurve(dPillars, dRates, startDate, "zeroDiscount", Interpolator1DType.LinearFlatExtrap, xaf);

            var s = new Calibrators.NewtonRaphsonAssetCurveSolver();
            var curve = s.Solve(instruments, pillars, discountCurve, startDate, );

            for (var i = 0; i < instruments.Count; i++)
            {
                var resultPV = Calibrators.NewtonRaphsonAssetCurveSolver.SwapPv(curve, instruments[i], discountCurve);
                Assert.Equal(0, resultPV, 6);
            }
        }

    }
}
