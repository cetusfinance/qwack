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
    public class StirFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void STIR()
        {
            var bd = DateTime.Today;
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRate = 0.05;
            var rates = pillars.Select(p => flatRate).ToArray();
            CalendarProvider.Collection.TryGetCalendar("LON", out var cal);
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var discoCurve = new IrCurve(pillars, rates, bd, "USD.BLAH", Interpolator1DType.Linear, usd);
            var fModel = new FundingModel(bd, new[] { discoCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var price = 93.0;
            var ix = new FloatRateIndex
            {
                Currency = usd,
                DayCountBasis = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                HolidayCalendars = cal,
                ResetTenor = 3.Months(),
                RollConvention = RollType.MF
            };

            var maturity = bd.AddDays(365);
            var accrualStart = maturity.AddPeriod(RollType.F, ix.HolidayCalendars, ix.FixingOffset);
            var accrualEnd = accrualStart.AddPeriod(ix.RollConvention, ix.HolidayCalendars, ix.ResetTenor);
            var dcf = maturity.CalculateYearFraction(accrualEnd, DayCountBasis.ACT360);
            var s = new STIRFuture
            {
                Currency = usd,
                ContractSize = 1e6,
                Position = 1,
                DCF = dcf,
                Expiry = maturity,
                ForecastCurve = "USD.BLAH",
                Price = price,
                Index = ix
            };

            var pv = s.Pv(fModel, false);
            var rateEst = discoCurve.GetForwardRate(accrualStart, accrualEnd, RateType.Linear, ix.DayCountBasis);
            var fairPrice = 100.0 - rateEst * 100;

            var expectedPv = -(price - fairPrice) * 1e6 * dcf / 100.0;

            Assert.Equal(expectedPv, pv, 8);

            var ss = s.Sensitivities(fModel);
            Assert.True(ss.Count == 1 && ss.Keys.Single() == "USD.BLAH");
            Assert.True(ss["USD.BLAH"].Count == 2 && ss["USD.BLAH"].Keys.Contains(accrualStart) && ss["USD.BLAH"].Keys.Contains(accrualEnd));

            Assert.Empty(s.Dependencies(null));
        }

    }
}
