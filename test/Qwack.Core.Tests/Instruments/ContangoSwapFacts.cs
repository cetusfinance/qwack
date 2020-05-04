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
    public class ContangoSwapFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void ContangoSwap()
        {
            var bd = new DateTime(2019, 06, 14);
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRateUsd = 0.05;
            var flatRateXau = 0.01;
            var spotRate = 1200;
            var ratesUsd = pillars.Select(p => flatRateUsd).ToArray();
            var ratesXau = pillars.Select(p => flatRateXau).ToArray();

            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var xau = TestProviderHelper.CurrencyProvider["XAU"];
            CalendarProvider.Collection.TryGetCalendar("LON", out var cal);
            var pair = new FxPair() { Domestic = xau, Foreign = usd, PrimaryCalendar = cal, SpotLag = 2.Bd() };

            var discoCurveUsd = new IrCurve(pillars, ratesUsd, bd, "USD.BLAH", Interpolator1DType.Linear, usd);
            var discoCurveXau = new IrCurve(pillars, ratesXau, bd, "XAU.BLAH", Interpolator1DType.Linear, xau);

            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, bd, new Dictionary<Currency, double> { { xau, 1.0/spotRate } }, new List<FxPair> { pair }, new Dictionary<Currency, string> { { usd, "USD.BLAH" }, { xau, "XAU.BLAH" } });
            var fModel = new FundingModel(bd, new[] { discoCurveUsd, discoCurveXau }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            fModel.SetupFx(fxMatrix);


            var maturity = bd.AddDays(365);
            var spot = bd.SpotDate(2.Bd(), cal, cal);
            var c = 0.022;
            var b = new ContangoSwap
            {
                CashCCY = usd,
                MetalCCY = xau,
                CashDiscountCurve = "USD.BLAH",
                DeliveryDate = maturity,
                ContangoRate = c,
                MetalQuantity = 1,
                SpotDate = spot
            };

            var t = spot.CalculateYearFraction(maturity, DayCountBasis.Act360);

            var pv = b.Pv(fModel, false);

            var fwdA = (1.0 + c * t)*spotRate;
            var fwdB = fModel.GetFxRate(maturity, xau, usd);
            var df = discoCurveUsd.GetDf(bd, maturity);
            var expectedPv = (fwdB - fwdA) * b.MetalQuantity;
            expectedPv *= df;
            Assert.Equal(expectedPv, pv, 10);
            Assert.Equal(maturity, b.LastSensitivityDate);
            Assert.Equal(usd, b.Currency);

            var s = b.Sensitivities(fModel);
            Assert.True(s.Count == 2 && s.Keys.Contains("USD.BLAH") && s.Keys.Contains("XAU.BLAH"));

            var s2 = b.Dependencies(fModel.FxMatrix);
            Assert.True(s2.Count == 2 && s2.Contains("USD.BLAH") && s2.Contains("XAU.BLAH"));

            Assert.Equal(0.0402428426839156, b.CalculateParRate(fModel),8);

            var b2 = (ContangoSwap)b.SetParRate(0.05);
            Assert.Equal(0.05, b2.ContangoRate);
        }

    }
}
