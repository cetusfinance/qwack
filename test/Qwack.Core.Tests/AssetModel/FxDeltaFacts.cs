using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Cubes;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Models;
using Qwack.Models.Models;
using Qwack.Providers.Json;
using Xunit;

namespace Qwack.Core.Tests.AssetModel
{
    public class FxDeltaFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        
        [Fact]
        public void FxDeltaOnUSDTrade()
        {
            var startDate = new DateTime(2018, 07, 28);
            var cal = CalendarProvider.Collection["LON"];
            var zar = TestProviderHelper.CurrencyProvider["ZAR"];
            var usd = TestProviderHelper.CurrencyProvider["USD"];

            var curvePillars = new[] { "1W", "1M", "3M", "6M", "1Y" };
            var curvePillarDates = curvePillars.Select(l => startDate.AddPeriod(RollType.F, cal, new Frequency(l))).ToArray();
            var curvePoints = new[] { 100.0, 100, 100, 100, 100 };
            var curve = new PriceCurve(startDate, curvePillarDates, curvePoints, PriceCurveType.LME,TestProviderHelper.CurrencyProvider, curvePillars)
            {
                Currency = usd,
                CollateralSpec = "CURVE",
                Name = "Coconuts"
            };

            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            var fxSpot = 15;
            var rates = new Dictionary<Currency, double> { { zar, fxSpot } };
            var discoMap = new Dictionary<Currency, string> { { zar, "ZAR.CURVE" }, { usd, "USD.CURVE" } };
            var fxPair = new FxPair()
            {
                Domestic = usd,
                Foreign = zar,
                SettlementCalendar = cal,
                SpotLag = new Frequency("0b")
            };
            fxMatrix.Init(usd, startDate, rates, new List<FxPair> { fxPair }, discoMap);

            var irPillars = new[] { startDate, startDate.AddYears(10) };
            var zarRates = new[] { 0.0, 0.0 };
            var usdRates = new[] { 0.0, 0.0 };
            var zarCurve = new IrCurve(irPillars, zarRates, startDate, "ZAR.CURVE", Interpolator1DType.Linear, zar, "CURVE");
            var usdCurve = new IrCurve(irPillars, usdRates, startDate, "USD.CURVE", Interpolator1DType.Linear, usd, "CURVE");

            var fModel = new FundingModel(startDate, new[] { zarCurve, usdCurve }, TestProviderHelper.CurrencyProvider);
            fModel.SetupFx(fxMatrix);

            var aModel = new AssetFxModel(startDate, fModel);
            aModel.AddPriceCurve("Coconuts", curve);

            var periodCode = "SEP-18";
            var (Start, End) = periodCode.ParsePeriod();
            var fixingDates = Start.BusinessDaysInPeriod(End, cal).ToArray();
            var settleDate = fixingDates.Last().AddPeriod(RollType.F, cal, new Frequency("5b"));
            var fxFwd = aModel.FundingModel.GetFxAverage(fixingDates, usd, zar);
            var assetFwd = curve.GetAveragePriceForDates(fixingDates);
            var fairStrike = assetFwd;
            var strike = fairStrike - 10;
            var nominal = 1000;

            var asianSwap = AssetProductFactory.CreateMonthlyAsianSwap(periodCode, strike, "Coconuts", cal, cal, new Frequency("5b"), usd, TradeDirection.Long, new Frequency("0b"), nominal, DateGenerationType.BusinessDays);
            asianSwap.TradeId = "aLovelyBunch";
            foreach (var sw in asianSwap.Swaplets)
            {
                sw.DiscountCurve = "USD.CURVE";
                sw.FxConversionType = FxConversionType.None;
            }
            var pv = asianSwap.PV(aModel);
            var expectedPV = (fairStrike - strike) * nominal;
            Assert.Equal(expectedPV, pv, 8);

            var portfolio = new Portfolio() { Instruments = new List<IInstrument> { asianSwap } };
            var pfPvCube = portfolio.PV(aModel, usd);
            var pfPv = (double)pfPvCube.GetAllRows().First().Value;
            Assert.Equal(expectedPV, pfPv, 8);

            //expected fx delta is just PV in USD
            var deltaCube = portfolio.FxDelta(aModel);
            var dAgg = deltaCube.Pivot("TradeId", Cubes.AggregationAction.Sum);
            var delta = (double)dAgg.GetAllRows().First().Value;
            Assert.Equal(expectedPV, delta, 8);


        }

        [Fact]
        public void FxDeltaOnCompoZARTrade()
        {
            var startDate = new DateTime(2018, 07, 28);
            var cal = CalendarProvider.Collection["LON"];
            var zar = TestProviderHelper.CurrencyProvider["ZAR"];
            var usd = TestProviderHelper.CurrencyProvider["USD"];

            var curvePillars = new[] { "1W", "1M", "3M", "6M", "1Y" };
            var curvePillarDates = curvePillars.Select(l => startDate.AddPeriod(RollType.F, cal, new Frequency(l))).ToArray();
            var curvePoints = new[] { 100.0, 100, 100, 100, 100 };
            var curve = new PriceCurve(startDate, curvePillarDates, curvePoints, PriceCurveType.LME,TestProviderHelper.CurrencyProvider, curvePillars)
            {
                Currency = usd,
                CollateralSpec = "CURVE",
                Name = "Coconuts"
            };

            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            var fxSpot = 15;
            var rates = new Dictionary<Currency, double> { { zar, fxSpot } };
            var discoMap = new Dictionary<Currency, string> { { zar, "ZAR.CURVE" }, { usd, "USD.CURVE" } };
            var fxPair = new FxPair()
            {
                Domestic = usd,
                Foreign = zar,
                SettlementCalendar = cal,
                SpotLag = new Frequency("0b")
            };
            fxMatrix.Init(usd, startDate, rates, new List<FxPair> { fxPair }, discoMap);

            var irPillars = new[] { startDate, startDate.AddYears(10) };
            var zarRates = new[] { 0.0, 0.0 };
            var usdRates = new[] { 0.0, 0.0 };
            var zarCurve = new IrCurve(irPillars, zarRates, startDate, "ZAR.CURVE", Interpolator1DType.Linear, zar, "CURVE");
            var usdCurve = new IrCurve(irPillars, usdRates, startDate, "USD.CURVE", Interpolator1DType.Linear, usd, "CURVE");

            var fModel = new FundingModel(startDate, new[] { zarCurve, usdCurve }, TestProviderHelper.CurrencyProvider);
            fModel.SetupFx(fxMatrix);

            var aModel = new AssetFxModel(startDate, fModel);
            aModel.AddPriceCurve("Coconuts", curve);

            var periodCode = "SEP-18";
            var (Start, End) = periodCode.ParsePeriod();
            var fixingDates = Start.BusinessDaysInPeriod(End, cal).ToArray();
            var settleDate = fixingDates.Last().AddPeriod(RollType.F, cal, new Frequency("5b"));
            var fxFwd = aModel.FundingModel.GetFxAverage(fixingDates, usd, zar);
            var assetFwd = curve.GetAveragePriceForDates(fixingDates);
            var fairStrike = assetFwd* fxFwd;
            var strike = fairStrike - 10;
            var nominal = 1000;

            var asianSwap = AssetProductFactory.CreateMonthlyAsianSwap(periodCode, strike, "Coconuts", cal, cal, new Frequency("5b"), zar, TradeDirection.Long, new Frequency("0b"), nominal, DateGenerationType.BusinessDays);
            asianSwap.TradeId = "aLovelyBunch";
            foreach (var sw in asianSwap.Swaplets)
            {
                sw.DiscountCurve = "USD.CURVE";
                sw.FxConversionType = FxConversionType.None;
            }
            var pv = asianSwap.PV(aModel);
            var expectedPV = (fairStrike - strike) * nominal;
            Assert.Equal(expectedPV, pv, 8);

            var portfolio = new Portfolio() { Instruments = new List<IInstrument> { asianSwap } };
            var pfPvCube = portfolio.PV(aModel, zar);
            var pfPv = (double)pfPvCube.GetAllRows().First().Value;
            Assert.Equal(expectedPV, pfPv, 8);

            //expected fx delta is just asset delta in ZAR
            var deltaCube = portfolio.FxDelta(aModel);
            var dAgg = deltaCube.Pivot("TradeId", Cubes.AggregationAction.Sum);
            var delta = (double)dAgg.GetAllRows().First().Value;
            Assert.Equal(nominal * assetFwd, delta, 8);

            //change intrinsic value, fx delta does not change as intrinsic is in ZAR
            strike = fairStrike - 20;
            asianSwap = AssetProductFactory.CreateMonthlyAsianSwap(periodCode, strike, "Coconuts", cal, cal, new Frequency("5b"), zar, TradeDirection.Long, new Frequency("0b"), nominal, DateGenerationType.BusinessDays);
            asianSwap.TradeId = "aLovelyBunch";
            foreach (var sw in asianSwap.Swaplets)
            {
                sw.DiscountCurve = "USD.CURVE";
                sw.FxConversionType = FxConversionType.None;
            }

            pv = asianSwap.PV(aModel);
            expectedPV = (fairStrike - strike) * nominal;
            Assert.Equal(expectedPV, pv, 8);

            deltaCube = portfolio.FxDelta(aModel);
            dAgg = deltaCube.Pivot("TradeId", Cubes.AggregationAction.Sum);
            delta = (double)dAgg.GetAllRows().First().Value;
            Assert.Equal(nominal * assetFwd, delta, 8);
        }
    }
}
