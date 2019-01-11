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
using Qwack.Futures;

namespace Qwack.Core.Tests.Instruments
{
    public class LoanDepoFacts
    {
        [Fact]
        public void FixedRateLoanDepo()
        {
            var bd = DateTime.Today;
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRate = 0.05;
            var rates = pillars.Select(p => flatRate).ToArray();
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var discoCurve = new IrCurve(pillars, rates, bd, "USD.BLAH", Interpolator1DType.Linear, usd);
            var fModel = new FundingModel(bd, new[] { discoCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var maturity = bd.AddDays(365);
            var notional = 100e6;
            var iRate = 0.07;
            var depo = new FixedRateLoanDeposit(bd, maturity, iRate, usd, DayCountBasis.ACT360, notional, "USD.BLAH");
            
            var pv = depo.Pv(fModel, false);

            var dfEnd = discoCurve.GetDf(bd, maturity);
            var t = bd.CalculateYearFraction(maturity, DayCountBasis.ACT360);
            var expectedPv = notional; //initial notional
            expectedPv += -notional * dfEnd; //final notional
            expectedPv += -notional * ( iRate * t) * dfEnd; //final notional
            Assert.Equal(expectedPv, pv, 8);


            var loan = new FixedRateLoanDeposit(bd, maturity, iRate, usd, DayCountBasis.ACT360, -notional, "USD.BLAH");
            pv = loan.Pv(fModel, false);

            expectedPv = -notional; //initial notional
            expectedPv += notional * dfEnd; //final notional
            expectedPv += notional * (iRate * t) * dfEnd; //final notional
            Assert.Equal(expectedPv, pv, 8);
        }


        [Fact]
        public void PastStartingFixedRateLoanDepo()
        {
            var bd = DateTime.Today;
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRate = 0.05;
            var rates = pillars.Select(p => flatRate).ToArray();
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var discoCurve = new IrCurve(pillars, rates, bd, "USD.BLAH", Interpolator1DType.Linear, usd);
            var fModel = new FundingModel(bd, new[] { discoCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var start = bd.AddDays(-10);
            var maturity = bd.AddDays(365);
            var notional = 100e6;
            var iRate = 0.07;

            var depo = new FixedRateLoanDeposit(start, maturity, iRate, usd, DayCountBasis.ACT360, notional, "USD.BLAH");

            var pv = depo.Pv(fModel, false);

            var dfEnd = discoCurve.GetDf(bd, maturity);
            var t = start.CalculateYearFraction(maturity, DayCountBasis.ACT360);
            var expectedPv = 0.0; //initial notional is in the past
            expectedPv += -notional * dfEnd; //final notional
            expectedPv += -notional * (iRate * t) * dfEnd; //final notional
            Assert.Equal(expectedPv, pv, 8);


            var loan = new FixedRateLoanDeposit(start, maturity, iRate, usd, DayCountBasis.ACT360, -notional, "USD.BLAH");
            pv = loan.Pv(fModel, false);

            expectedPv = 0.0; //initial notional is in the past
            expectedPv += notional * dfEnd; //final notional
            expectedPv += notional * (iRate * t) * dfEnd; //final notional
            Assert.Equal(expectedPv, pv, 8);
        }
    }
}
