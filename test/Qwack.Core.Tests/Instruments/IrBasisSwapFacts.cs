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
using Qwack.Futures;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Tests.Instruments
{
    public class IrBasisSwapFacts
    {

        [Fact]
        public void IrBasisSwap()
        {
            var bd = DateTime.Parse("2018-09-13");
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRate3m = 0.05;
            var flatRate6m = 0.06;
            var rates3m = pillars.Select(p => flatRate3m).ToArray();
            var rates6m = pillars.Select(p => flatRate6m).ToArray();
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var discoCurve3m = new IrCurve(pillars, rates3m, bd, "USD.BLAH.3M", Interpolator1DType.Linear, usd);
            var discoCurve6m = new IrCurve(pillars, rates6m, bd, "USD.BLAH.6M", Interpolator1DType.Linear, usd);
            var fModel = new FundingModel(bd, new[] { discoCurve3m, discoCurve6m }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            TestProviderHelper.CalendarProvider.Collection.TryGetCalendar("LON", out var cal);

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

            var ix2 = new FloatRateIndex
            {
                Currency = usd,
                DayCountBasis = DayCountBasis.ACT360,
                DayCountBasisFixed = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                HolidayCalendars = cal,
                ResetTenor = 6.Months(),
                ResetTenorFixed = 6.Months(),
                RollConvention = RollType.MF
            };


            var parSpread = 0.01;
            var notional = 100e6;
            var startDate = bd.AddPeriod(RollType.F, cal, 2.Bd());
            var maturity = startDate.AddDays(365);
            var swp = new IrBasisSwap(startDate, 1.Years(), parSpread, true, ix, ix2, "USD.BLAH.3M", "USD.BLAH.6M", "USD.BLAH.3M", (decimal) notional);

            var pv = swp.Pv(fModel, true);
            Assert.Equal(7211.8875428740866, pv, 8);

            Assert.Equal(swp.EndDate, swp.LastSensitivityDate);

            swp.SolveCurve = "USD.BLAH.6M";
            var d = swp.Dependencies(null);
            Assert.Single(d);

            Assert.Equal(-0.109324115016209, swp.CalculateParRate(fModel), 10);

            Assert.Equal(0.09, (swp.SetParRate(0.09) as IrBasisSwap).ParSpreadPay);

            swp = new IrBasisSwap(startDate, 1.Years(), parSpread, false, ix, ix2, "USD.BLAH.3M", "USD.BLAH.6M", "USD.BLAH.3M", (decimal)notional);
            Assert.Equal(0.077, (swp.SetParRate(0.077) as IrBasisSwap).ParSpreadRec);
            Assert.Equal(-0.110017199809182, swp.CalculateParRate(fModel), 10);

        }

    }
}
