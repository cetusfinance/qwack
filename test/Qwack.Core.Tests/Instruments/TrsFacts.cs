using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Curves;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Models;
using Qwack.Dates;
using static System.Math;
using Qwack.Providers.Json;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Basic;

namespace Qwack.Core.Tests.Instruments
{
    public class TrsFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void AssetTrs()
        {
            var bd = DateTime.Parse("2023-09-26");
            var flatRate = 0.05;
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var discoCurve = new ConstantRateIrCurve(flatRate, bd, "USD.DISCO", usd);
            var fModel = new FundingModel(bd, new[] { discoCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
           
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, bd, new Dictionary<Currency, double>(), new List<FxPair>(), new() { { usd, "USD.DISCO" } });
            fModel.SetupFx(fxMatrix);
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

            var aModel = new AssetFxModel(bd, fModel);

            var fixings = new FixingDictionary(new Dictionary<DateTime, double> { { bd.AddDays(-10), 99 } }) 
                { Name = "EQIX" };

            aModel.AddFixingDictionary("EQIX", fixings);

            //var ixCurve = new ConstantPriceCurve(100, bd, TestProviderHelper.CurrencyProvider)
            //{
            //    AssetId = "EQIX"
            //};
            var ixCurve = new EquityPriceCurve(bd, 100, usd, discoCurve, bd, TestProviderHelper.CurrencyProvider)
            {
                AssetId = "EQIX"
            };
            aModel.AddPriceCurve("EQIX", ixCurve);

            var startDate = bd.AddDays(-10);
            var maturity = startDate.AddDays(365);

            var ul = new EquityIndex()
            {
                AssetId = "EQIX",
                Currency = usd,
                FxConversionType = FxConversionType.None,
                Name = "EQIX",
            };

            var swp = new AssetTrs(ul, TrsLegType.Bullet, SwapLegType.Float, TrsLegType.Resetting, ix, 1e6, 0, FxConversionType.None, startDate, maturity, usd)
                { ForecastFundingCurve = "USD.DISCO", DiscountCurve="USD.DISCO"};
            var pv = swp.PV(aModel);
            Assert.Equal(9399.28384704169, pv, 8);

            
        }

    }
}
