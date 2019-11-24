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
using Qwack.Providers.Json;

namespace Qwack.Core.Tests.Instruments
{
    public class IrSwapFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void IrSwap()
        {
            var bd = DateTime.Parse("2018-09-13");
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRate = 0.05;
            var rates = pillars.Select(p => flatRate).ToArray();
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var discoCurve = new IrCurve(pillars, rates, bd, "USD.BLAH", Interpolator1DType.Linear, usd);
            var fModel = new FundingModel(bd, new[] { discoCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
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
            var swp = new IrSwap(startDate, 1.Years(), ix, parRate, SwapPayReceiveType.Pay, "USD.BLAH", "USD.BLAH") { Notional = notional, RateIndex = ix };

            var pv = swp.Pv(fModel, true);
            Assert.Equal(-368.89651349, pv, 8);

            swp = new IrSwap(startDate, 1.Years(), ix, parRate+0.01, SwapPayReceiveType.Pay, "USD.BLAH", "USD.BLAH") { Notional = notional, RateIndex = ix };
            pv = swp.Pv(fModel, true);
            Assert.Equal(-10217.8229952, pv, 8);

            Assert.Equal(swp.EndDate, swp.LastSensitivityDate);

            var d = swp.Dependencies(null);
            Assert.Single(d);

            Assert.Equal(0.0496254169169585, swp.CalculateParRate(fModel),10);
            Assert.Equal(0.09, (swp.SetParRate(0.09) as IrSwap).ParRate);

            Assert.Equal(1.0, swp.SupervisoryDelta(null));
            Assert.Equal(1.0, swp.MaturityBucket(startDate));

        }

    }
}
