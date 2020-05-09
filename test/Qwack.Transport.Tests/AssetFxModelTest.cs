using System;
using System.Collections.Generic;
using System.IO;
using Qwack.Core.Basic;
using Qwack.Core.Basic.Correlation;
using Qwack.Core.Curves;
using Qwack.Dates;
using Qwack.Models;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.TransportObjects.MarketData.Models;
using Xunit;

namespace Qwack.Transport.Tests
{
    public class AssetFxModelTest
    {
        private DateTime ValDate = new DateTime(2020, 05, 08);
        private Currency zar = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR");
        private Currency usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
        private AssetFxModel GetModel()
        {
            var irCurveZar = new ConstantRateIrCurve(0.07, ValDate, "ZAR-IR", zar);
            var irCurveUsd = new ConstantRateIrCurve(0.02, ValDate, "USD-IR", zar);
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            var fxPair = new FxPair()
            {
                Domestic = usd,
                Foreign = zar,
                PrimaryCalendar = TestProviderHelper.CalendarProvider.GetCalendar("ZAR"),
                SecondaryCalendar = TestProviderHelper.CalendarProvider.GetCalendar("USD"),
                SpotLag = new Frequency("2b")
            };
            fxMatrix.Init(usd, ValDate, new Dictionary<Currency, double> { { zar, 20.0 } }, new List<FxPair> { fxPair }, new Dictionary<Currency, string> { { zar, "ZAR-IR" }, { usd, "USD-IR" } });
            var fModel = new FundingModel(ValDate, new[] { irCurveUsd, irCurveZar }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            fModel.SetupFx(fxMatrix);
            var fxSurface = new ConstantVolSurface(ValDate, 0.16);
            fModel.VolSurfaces.Add("USD/ZAR", fxSurface);
            var crudeCurve = new ConstantPriceCurve(100, ValDate, TestProviderHelper.CurrencyProvider)
            {
                Name ="OIL",
                AssetId = "OIL",
                Currency = usd
            };
            var crudeSurface = new ConstantVolSurface(ValDate, 0.32)
            {
                Name = "OIL",
                AssetId = "OIL",
                Currency = usd
            };
            var aModel = new AssetFxModel(ValDate, fModel);
            aModel.AddPriceCurve("OIL", crudeCurve);
            aModel.AddVolSurface("OIL", crudeSurface);
            aModel.CorrelationMatrix = new CorrelationMatrix(new[] { "OIL" }, new[] { "USD/ZAR" }, new double[][] {  new [] { 0.5 } });
            return aModel;
        }
        [Fact]
        public void RoundTrip()
        {
            var aModel = GetModel();
            var to = aModel.ToTransportObject();
            var aModel2 = new AssetFxModel(to, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            Assert.Equal(aModel.GetPriceCurve("OIL").GetPriceForDate(ValDate.AddDays(100)), aModel2.GetPriceCurve("OIL").GetPriceForDate(ValDate.AddDays(100)));
            Assert.Equal(aModel.GetPriceCurve("OIL",zar).GetPriceForDate(ValDate.AddDays(100)), aModel2.GetPriceCurve("OIL",zar).GetPriceForDate(ValDate.AddDays(100)));
            Assert.Equal(aModel.GetCompositeVolForStrikeAndDate("OIL", ValDate.AddDays(100), 1000, zar), aModel2.GetCompositeVolForStrikeAndDate("OIL", ValDate.AddDays(100), 1000, zar));
        }

        [Fact]
        public void RoundTripViaProtoBuf()
        {
            var aModel = GetModel();
            var to = aModel.ToTransportObject();
            var ms = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ms, to);
            ms.Flush();
            var to2 = ProtoBuf.Serializer.Deserialize<TO_AssetFxModel>(ms);
            var aModel2 = new AssetFxModel(to2, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            Assert.Equal(aModel.GetPriceCurve("OIL").GetPriceForDate(ValDate.AddDays(100)), aModel2.GetPriceCurve("OIL").GetPriceForDate(ValDate.AddDays(100)));
            Assert.Equal(aModel.GetPriceCurve("OIL", zar).GetPriceForDate(ValDate.AddDays(100)), aModel2.GetPriceCurve("OIL", zar).GetPriceForDate(ValDate.AddDays(100)));
            Assert.Equal(aModel.GetCompositeVolForStrikeAndDate("OIL", ValDate.AddDays(100), 1000, zar), aModel2.GetCompositeVolForStrikeAndDate("OIL", ValDate.AddDays(100), 1000, zar));
        }
    }
}
