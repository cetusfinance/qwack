using System;
using System.Collections.Generic;
using Xunit;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Models.MCModels;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Options.VolSurfaces;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math.Extensions;
using Qwack.Options;

namespace Qwack.Models.Tests.MCModels
{
    public class LocalCorrelationFacts
    {
        private double[]  _correls = new[] { 0.50, 0.48, 0.46, 0.40 };

        private AssetFxMCModel GetSut()
        {
            var buildDate = DateTime.Parse("2018-10-04");
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var zar = TestProviderHelper.CurrencyProvider["ZAR"];
            TestProviderHelper.CalendarProvider.Collection.TryGetCalendar("NYC", out var usdCal);
            var pair = new FxPair() { Domestic = zar, Foreign = usd, SettlementCalendar = usdCal, SpotLag = 2.Bd() };

            var dfCurve = new IrCurve(new[] { buildDate, buildDate.AddDays(1000) }, new[] { 0.0, 0.0 }, buildDate, "disco", Math.Interpolation.Interpolator1DType.Linear, usd, "DISCO");

            var dates = new[] { buildDate, buildDate.AddDays(32), buildDate.AddDays(60), buildDate.AddDays(90) };
            var times = dates.Select(d => buildDate.CalculateYearFraction(d, DayCountBasis.Act365F)).ToArray();
            var vols = new[] { 0.32, 0.30, 0.29, 0.28 };
            var comCurve = new PriceCurve(buildDate, dates, new[] { 100.0, 100.0, 100.0, 100.0 }, PriceCurveType.NYMEX, TestProviderHelper.CurrencyProvider)
            {
                Name = "CL",
                AssetId = "CL"
            };
            var comSurface = new GridVolSurface(buildDate, new[] { 0.5 }, dates, vols.Select(x => new double[] { x }).ToArray(), StrikeType.ForwardDelta, Math.Interpolation.Interpolator1DType.Linear, Math.Interpolation.Interpolator1DType.LinearInVariance, DayCountBasis.Act365F) { AssetId = "CL" };
            var fxSurface = new ConstantVolSurface(buildDate, 0.16) { AssetId = "USD/ZAR" }; 
            var correlVector = new CorrelationTimeVector("CL", "USD/ZAR", _correls, times);
            var fModel = new FundingModel(buildDate, new Dictionary<string, IrCurve> { { "DISCO", dfCurve } }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var fxM = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxM.Init(usd, buildDate, new Dictionary<Currency, double>() { { zar, 14.0 } }, new List<FxPair>() { pair }, new Dictionary<Currency, string> { { usd, "DISCO" }, { zar, "DISCO" } });
            fModel.SetupFx(fxM);
            fModel.VolSurfaces.Add("ZAR/USD", fxSurface);
            fModel.VolSurfaces.Add("USD/ZAR", fxSurface);

            var aModel = new AssetFxModel(buildDate, fModel);
            aModel.AddVolSurface("CL", comSurface);
            aModel.AddPriceCurve("CL", comCurve);
            aModel.CorrelationMatrix = correlVector;

            var product1 = AssetProductFactory.CreateAsianOption(dates[1], dates[1], 1400, "CL", OptionType.Call, usdCal, dates[1], zar);
            product1.TradeId = "P1";
            product1.DiscountCurve = "DISCO";
            product1.FxConversionType = FxConversionType.AverageThenConvert;
            var product2 = AssetProductFactory.CreateAsianOption(dates[2], dates[2], 1400, "CL", OptionType.Call, usdCal, dates[2], zar);
            product2.TradeId = "P2";
            product2.DiscountCurve = "DISCO";
            product2.FxConversionType = FxConversionType.AverageThenConvert;
            var product3 = AssetProductFactory.CreateAsianOption(dates[3], dates[3], 1400, "CL", OptionType.Call, usdCal, dates[3], zar);
            product3.TradeId = "P3";
            product3.DiscountCurve = "DISCO";
            product3.FxConversionType = FxConversionType.AverageThenConvert;

            var pfolio = new Portfolio { Instruments = new List<IInstrument> { product1, product2, product3 } };
            var settings = new McSettings
            {
                Generator = RandomGeneratorType.MersenneTwister,
                NumberOfPaths = (int)2.0.IntPow(15),
                NumberOfTimesteps = 1,
                ReportingCurrency = zar,
                Parallelize = false,
                LocalCorrelation = true, 
            };
            var sut = new AssetFxMCModel(buildDate, pfolio, aModel, settings, TestProviderHelper.CurrencyProvider, TestProviderHelper.FutureSettingsProvider, TestProviderHelper.CalendarProvider);
            return sut;
        }

        [Fact]
        public void CanRunPV()
        {
            var sut = GetSut();
            var zar = TestProviderHelper.CurrencyProvider["ZAR"];
            var pvCube = sut.PV(zar);
            var expiryDates = sut.Portfolio.Instruments
                .Select(x => x as AsianOption)
                .OrderBy(x => x.AverageEndDate)
                .Select(x => x.AverageEndDate);
            var expiryTimes = expiryDates.Select(d => sut.OriginDate.CalculateYearFraction(d, DayCountBasis.Act365F)).ToArray();
            var pvs = pvCube.GetAllRows().Select(x => x.Value).ToArray();
            var vols = pvs.Select((x, ix) => BlackFunctions.BlackImpliedVol(1400, 1400, 0.0, expiryTimes[ix], x, OptionType.C)).ToArray();
            var volsCl = expiryDates.Select(d => sut.VanillaModel.GetVolSurface("CL").GetVolForDeltaStrike(0.5, d, 100)).ToArray();
            var volsFx = expiryDates.Select(d => sut.VanillaModel.GetVolSurface("USD/ZAR").GetVolForDeltaStrike(0.5, d, 100)).ToArray();
            var expectedVols = volsCl.Select((x, ix) => System.Math.Sqrt(x * x + volsFx[ix] * volsFx[ix] + 2 * _correls[ix+1] * volsFx[ix] * x)).ToArray();
            
            for(var i=0;i< expiryTimes.Length;i++)
            {
                Assert.Equal(expectedVols[i], vols[i],2);
            }
        }

       
    }
}
