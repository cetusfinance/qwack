using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Models;
using Qwack.Models.Models;
using Qwack.Providers.Json;
using Xunit;
using Qwack.Futures;

namespace Qwack.Core.Tests.AssetModel
{
    public class AssetPVFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly string JsonFuturesPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "futuresettings.json");
        public static readonly string JsonCurrencyPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Currencies.json");
        public static readonly ICurrencyProvider CurrencyProvider = new CurrenciesFromJson(CalendarProvider, JsonCurrencyPath);
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        public static readonly IFutureSettingsProvider futureSettingsProvider = new FutureSettingsFromJson(CalendarProvider, JsonFuturesPath);

        [Fact]
        public void AsianCompoSwap()
        {
            var startDate = new DateTime(2018, 07, 28);
            var cal = CalendarProvider.Collection["LON"];
            var xaf = CurrencyProvider["XAF"];
            var usd = CurrencyProvider["USD"];

            var curvePillars = new[] { "1W", "1M", "3M", "6M", "1Y" };
            var curvePillarDates = curvePillars.Select(l => startDate.AddPeriod(RollType.F, cal, new Frequency(l))).ToArray();
            var curvePoints = new[] { 100.0, 100, 100, 100, 100 };
            var curve = new PriceCurve(startDate, curvePillarDates, curvePoints, PriceCurveType.LME, TestProviderHelper.CurrencyProvider, curvePillars)
            {
                Currency = usd,
                CollateralSpec = "CURVE",
                Name = "Coconuts",
                AssetId = "Coconuts"
            };

            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            var fxSpot = 7;
            var rates = new Dictionary<Currency, double> { { xaf, fxSpot } };
            var discoMap = new Dictionary<Currency, string> { { xaf, "XAF.CURVE" }, { usd, "USD.CURVE" } };
            var fxPair = new FxPair()
            {
                Domestic = usd,
                Foreign = xaf,
                SettlementCalendar = cal,
                SpotLag = new Frequency("0b")
            };
            fxMatrix.Init(usd, startDate, rates, new List<FxPair> { fxPair }, discoMap);

            var irPillars = new[] { startDate, startDate.AddYears(10) };
            var xafRates = new[] { 0.1, 0.1 };
            var usdRates = new[] { 0.01, 0.01 };
            var xafCurve = new IrCurve(irPillars, xafRates, startDate, "XAF.CURVE", Interpolator1DType.Linear, xaf, "CURVE");
            var usdCurve = new IrCurve(irPillars, usdRates, startDate, "USD.CURVE", Interpolator1DType.Linear, usd, "CURVE");

            var fModel = new FundingModel(startDate, new[] { xafCurve, usdCurve }, TestProviderHelper.CurrencyProvider);
            fModel.SetupFx(fxMatrix);

            var aModel = new AssetFxModel(startDate, fModel);
            aModel.AddPriceCurve("Coconuts", curve);

            var periodCode = "SEP-18";
            var (Start, End) = periodCode.ParsePeriod();
            var fixingDates = Start.BusinessDaysInPeriod(End, cal).ToArray();
            var settleDate = fixingDates.Last().AddPeriod(RollType.F, cal, new Frequency("5b"));
            var fxFwd = aModel.FundingModel.GetFxAverage(fixingDates, usd, xaf);
            var assetFwd = curve.GetAveragePriceForDates(fixingDates);
            var fairStrike = fxFwd * assetFwd;

            var asianSwap = AssetProductFactory.CreateMonthlyAsianSwap(periodCode, fairStrike, "Coconuts", cal, cal, new Frequency("5b"), xaf, TradeDirection.Long, new Frequency("0b"), 1000, DateGenerationType.BusinessDays);
            asianSwap.TradeId = "aLovelyBunch";
            foreach (var sw in asianSwap.Swaplets)
            {
                sw.DiscountCurve = "XAF.CURVE";
                sw.FxConversionType = FxConversionType.AverageThenConvert;
            }
            var pv = asianSwap.PV(aModel);
            Assert.Equal(0, pv, 8);

            var portfolio = new Portfolio() { Instruments = new List<IInstrument> { asianSwap } };
            var pfPvCube = portfolio.PV(aModel);
            var pfPv = (double)pfPvCube.GetAllRows().First().Value;
            Assert.Equal(0.0, pfPv, 8); 

            var deltaCube = portfolio.AssetDelta(aModel);
            var dAgg = deltaCube.Pivot("TradeId", AggregationAction.Sum);
            var delta = (double)dAgg.GetAllRows().First().Value;
            var t0Spot = aModel.FundingModel.GetFxRate(startDate, usd, xaf);
            var df = xafCurve.GetDf(startDate, settleDate);
            Assert.Equal(994.673992862018, delta,7);

            var fxDeltaCube = portfolio.FxDelta(aModel,usd, CurrencyProvider);
            var dfxAgg = fxDeltaCube.Pivot("TradeId", AggregationAction.Sum);
            var fxDelta = (double)dfxAgg.GetAllRows().First().Value;
            Assert.Equal(-1000 * df * fxFwd * 100 / (t0Spot / fxSpot), fxDelta, 4);
        }

    }
}
