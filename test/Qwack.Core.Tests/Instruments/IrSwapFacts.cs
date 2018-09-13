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
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Providers.Json;

namespace Qwack.Core.Tests.Instruments
{
    public class IrSwapFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void IrSwap()
        {
            var bd = DateTime.Today;
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRate = 0.05;
            var rates = pillars.Select(p => flatRate).ToArray();
            var usd = new Currency("USD", Dates.DayCountBasis.Act365F, null);
            var discoCurve = new IrCurve(pillars, rates, bd, "USD.BLAH", Interpolator1DType.Linear, usd);
            var fModel = new FundingModel(bd, new[] { discoCurve });
            CalendarProvider.Collection.TryGetCalendar("LON", out var cal);

            var ix = new FloatRateIndex
            {
                Currency = usd,
                DayCountBasis = DayCountBasis.ACT360,
                DayCountBasisFixed = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                HolidayCalendars = cal,
                ResetTenor = 3.Months(),
                ResetTenorFixed = 3.Months(),
                RollConvention = RollType.MF
            };


            var parRate = 0.05;
            var notional = 100e6;
            var startDate = bd.AddPeriod(RollType.F, cal, 2.Bd());
            var maturity = startDate.AddDays(365);
            var swp = new IrSwap(startDate, 1.Years(), ix, parRate, SwapPayReceiveType.Pay, "USD.BLAH", "USD.BLAH") { Notional = notional };

            var pv = swp.Pv(fModel, true);
            Assert.Equal(-368.89651349, pv, 8);

            swp = new IrSwap(startDate, 1.Years(), ix, parRate+0.01, SwapPayReceiveType.Pay, "USD.BLAH", "USD.BLAH") { Notional = notional };
            pv = swp.Pv(fModel, true);
            Assert.Equal(-10217.8229952, pv, 8);

        }

    }
}
