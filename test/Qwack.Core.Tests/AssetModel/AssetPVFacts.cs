using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Math.Utils;
using Qwack.Models;
using Qwack.Models.Models;
using Qwack.Providers.Json;
using Xunit;

namespace Qwack.Core.Tests.AssetModel
{
    public class AssetPVFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void AsianCompoSwap()
        {
            var startDate = new DateTime(2018, 07, 28);
            var cal = CalendarProvider.Collection["LON"];
            var xaf = new Currency("XAF", DayCountBasis.Act365F, cal);
            var usd = new Currency("USD", DayCountBasis.Act365F, cal);

            var curvePillars = new[] { "1W", "1M", "3M", "6M", "1Y" };
            var curvePillarDates = curvePillars.Select(l => startDate.AddPeriod(RollType.F, cal, new Frequency(l))).ToArray();
            var curvePoints = new[] { 100.0, 100, 100, 100, 100 };
            var curve = new PriceCurve(startDate, curvePillarDates, curvePoints, PriceCurveType.LME, curvePillars);
            curve.Currency = usd;
            curve.Name = "Coconuts";

            var fxMatrix = new FxMatrix();
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
            var xafCurve = new IrCurve(irPillars, xafRates, startDate, "XAF.CURVE", Interpolator1DType.Linear);
            var usdCurve = new IrCurve(irPillars, usdRates, startDate, "USD.CURVE", Interpolator1DType.Linear);

            var fModel = new FundingModel(startDate, new[] { xafCurve, usdCurve });
            fModel.SetupFx(fxMatrix);

            var aModel = new AssetFxModel(startDate, fModel);
            aModel.AddPriceCurve("Coconuts", curve);

            var periodCode = "SEP-18";
            var periodDates = periodCode.ParsePeriod();
            var fixingDates = periodDates.Start.BusinessDaysInPeriod(periodDates.End, cal).ToArray();
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
            var dAgg = deltaCube.Pivot("TradeId", Cubes.AggregationAction.Sum);
            var delta = (double)dAgg.GetAllRows().First().Value;
            var t0Spot = aModel.FundingModel.GetFxRate(startDate, usd, xaf);
            var df = xafCurve.GetDf(startDate, settleDate);
            Assert.Equal(1000 * df * fxFwd / t0Spot, delta,8);

            var fxDeltaCube = portfolio.FxDelta(aModel);
            var dfxAgg = fxDeltaCube.Pivot("TradeId", Cubes.AggregationAction.Sum);
            var fxDelta = (double)dfxAgg.GetAllRows().First().Value;
            Assert.Equal(1000 * df * fxFwd / fxSpot * 100, fxDelta, 8);
        }

    }
}
