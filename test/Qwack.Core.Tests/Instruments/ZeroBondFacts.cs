using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Curves;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Core.Basic;
using Qwack.Models;
using Qwack.Dates;
using static System.Math;
using Qwack.Futures;
using Qwack.Providers.Json;
using Microsoft.Extensions.PlatformAbstractions;

namespace Qwack.Core.Tests.Instruments
{
    public class ZeroBondFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly string JsonFuturesPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "futuresettings.json");
        public static readonly string JsonCurrencyPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Currencies.json");
        public static readonly ICurrencyProvider CurrencyProvider = new CurrenciesFromJson(CalendarProvider, JsonCurrencyPath);
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        public static readonly IFutureSettingsProvider futureSettingsProvider = new FutureSettingsFromJson(CalendarProvider, JsonFuturesPath);

        [Fact]
        public void ZeroBond()
        {
            var bd = DateTime.Today;
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRate = 0.05;
            var rates = pillars.Select(p => flatRate).ToArray();
            var usd = CurrencyProvider["USD"];
            var discoCurve = new IrCurve(pillars, rates, bd, "USD.BLAH", Interpolator1DType.Linear, usd);
            var fModel = new FundingModel(bd, new[] { discoCurve }, TestProviderHelper.CurrencyProvider);
            var price = 0.93;
            var notional = 100e6;
            var maturity = bd.AddDays(365);
            var b = new ZeroBond(price, bd.AddDays(365), "USD.BLAH") { Notional = notional };
            var t = bd.CalculateYearFraction(maturity, DayCountBasis.Act365F);

            var pv = b.Pv(fModel, false);
            var expectedPv = (Exp(-flatRate * t) - price) * notional;
            Assert.Equal(expectedPv, pv);

            var s = b.Sensitivities(fModel);
            Assert.True(s.Count == 1 && s.Keys.Single() == "USD.BLAH");
            Assert.True(s["USD.BLAH"].Count == 1 && s["USD.BLAH"].Single().Key == maturity);
            Assert.Equal(-t*notional*Exp(-flatRate*t), s["USD.BLAH"][maturity]);
        }

    }
}
