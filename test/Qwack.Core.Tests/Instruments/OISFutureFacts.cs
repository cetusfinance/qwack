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
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Tests.Instruments
{
    public class OISFutureFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void OISFuture()
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
            var s = new OISFuture
            {
                Currency = usd,
                ContractSize = 1e6,
                Position = 1,
                DCF = dcf,
                AverageStartDate = accrualStart,
                AverageEndDate = accrualEnd,
                ForecastCurve = "USD.BLAH",
                Price = price,
                Index = ix
            };

            var pv = s.Pv(fModel, false);
            var rateEst = discoCurve.GetForwardRate(accrualStart, accrualEnd, RateType.Linear, ix.DayCountBasis);
            var fairPrice = 100.0 - rateEst * 100;

            var expectedPv = (price - fairPrice) * 1e6 * dcf;

            Assert.Equal(expectedPv, pv);

            var ss = s.Sensitivities(fModel);
            Assert.True(ss.Count == 1 && ss.Keys.Single() == "USD.BLAH");
            Assert.True(ss["USD.BLAH"].Count == 2 && ss["USD.BLAH"].Keys.Contains(accrualStart) && ss["USD.BLAH"].Keys.Contains(accrualEnd));

            Assert.Equal(accrualEnd, s.LastSensitivityDate);
            Assert.Empty(s.Dependencies(null));

            var s2 = (OISFuture)s.SetParRate(97);
            Assert.Equal(97, s2.Price);
        }


        [Fact]
        public void OISFuturePastFixings()
        {
            var bd = new DateTime(2023, 04, 19);
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRate = 0.05;
            var rates = pillars.Select(p => flatRate).ToArray();
            CalendarProvider.Collection.TryGetCalendar("LON", out var cal);
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            
            var curve = new IrCurve(pillars, rates, bd, "USD.BLAH", Interpolator1DType.Linear, usd);

            var ix = new FloatRateIndex
            {
                Currency = usd,
                DayCountBasis = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                HolidayCalendars = cal,
                ResetTenor = 3.Months(),
                RollConvention = RollType.MF
            };

            var maturity = bd.AddDays(-1);
            var accrualStart = maturity.SubtractPeriod(RollType.F, ix.HolidayCalendars, 3.Months());
            var accrualEnd = maturity;
            var dcf = accrualStart.CalculateYearFraction(accrualEnd, DayCountBasis.ACT360);
            var fModel = new FundingModel(bd, new[] { curve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var sut = new OISFuture()
            {
                Currency = usd,
                ContractSize = 1e6,
                Position = 1,
                DCF = dcf,
                AverageStartDate = accrualStart,
                AverageEndDate = accrualEnd,
                ForecastCurve = "USD.BLAH",
                Price = 100,
                Index = ix
            };

            var fixRate = 0.025;
            var bizDates = accrualStart.BusinessDaysInPeriod(accrualEnd, cal);
            var fixings = bizDates.ToDictionary(x => x, x => fixRate);
            curve.Fixings = fixings;

            var par = sut.CalculateParRate(fModel);
            var nDays = (accrualEnd - accrualStart).TotalDays;
            var index = 1.0;
            for (var i = 0; i < bizDates.Count -1; i++)
            {
                var d = (bizDates[i+1] - bizDates[i]).TotalDays;
                index *= (1 + fixRate * d / 360);
            }
                
            var expectedPar = 100.0 -  (index - 1) * 360 / nDays * 100.0;
            Assert.Equal(expectedPar, par, 8);
        }

    }
}
