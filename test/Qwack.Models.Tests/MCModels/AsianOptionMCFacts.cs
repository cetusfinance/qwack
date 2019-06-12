using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Models.MCModels;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Options.VolSurfaces;
using System.Linq;
using Qwack.Dates;
using Qwack.Core.Basic;

namespace Qwack.Models.Tests.MCModels
{
    public class AsianOptionMCFacts
    {
        private AssetFxMCModel GetSut()
        {
            var buildDate = DateTime.Parse("2018-10-04");
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            TestProviderHelper.CalendarProvider.Collection.TryGetCalendar("NYC", out var usdCal);
            var dfCurve = new IrCurve(new[] { buildDate, buildDate.AddDays(1000) }, new[] { 0.0, 0.0 }, buildDate, "disco", Math.Interpolation.Interpolator1DType.Linear, usd, "DISCO");

            var comCurve = new PriceCurve(buildDate, new[] { buildDate, buildDate.AddDays(15), buildDate.AddDays(100) }, new[] { 100.0, 100.0, 100.0 }, PriceCurveType.NYMEX, TestProviderHelper.CurrencyProvider)
            {
                Name = "CL",
                AssetId = "CL"
            };
            var comSurface = new ConstantVolSurface(buildDate, 0.32) { AssetId = "CL" };
            var fModel = new FundingModel(buildDate, new Dictionary<string, IrCurve> { { "DISCO", dfCurve } }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var fxM = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxM.Init(usd, buildDate, new Dictionary<Currency, double>(), new List<FxPair>(), new Dictionary<Currency, string> { { usd, "DISCO" } });
            fModel.SetupFx(fxM);

            var aModel = new AssetFxModel(buildDate, fModel);
            aModel.AddVolSurface("CL", comSurface);
            aModel.AddPriceCurve("CL", comCurve);

            var product = AssetProductFactory.CreateAsianOption(buildDate.AddDays(10), buildDate.AddDays(20), 101, "CL", OptionType.Call, usdCal, buildDate.AddDays(21), usd);
            product.TradeId = "waaah";
            product.DiscountCurve = "DISCO";


            var pfolio = new Portfolio { Instruments = new List<IInstrument> { product } };
            var settings = new McSettings
            {
                Generator = RandomGeneratorType.MersenneTwister,
                NumberOfPaths = (int)System.Math.Pow(2, 13),
                NumberOfTimesteps = 1,
                ReportingCurrency = usd,
                Parallelize = false,
                ExposureDates = new DateTime[] { buildDate.AddDays(5), buildDate.AddDays(20), buildDate.AddDays(22) },
            };
            var sut = new AssetFxMCModel(buildDate, pfolio, aModel, settings, TestProviderHelper.CurrencyProvider, TestProviderHelper.FutureSettingsProvider, TestProviderHelper.CalendarProvider);
            return sut;
        }

        [Fact]
        public void CanRunPV()
        {
            var sut = GetSut();
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var pvCube = sut.PV(usd);

            var ins = sut.Portfolio.Instruments.First() as AsianOption;
            TestProviderHelper.CalendarProvider.Collection.TryGetCalendar("NYC", out var usdCal);

            var clewlowPV = Options.Asians.LME_Clewlow.PV(100, 0, 0.32, 101, sut.Model.BuildDate, ins.AverageStartDate, ins.AverageEndDate, 0.0, OptionType.C, usdCal);
            var tbPV = Options.Asians.TurnbullWakeman.PV(100, 0, 0.32, 101, sut.Model.BuildDate, ins.AverageStartDate, ins.AverageEndDate, 0.0, OptionType.C);
            var tbFutPV = Options.Asians.TurnbullWakeman.PV(ins.FixingDates.Select(x => 100.0).ToArray(), ins.FixingDates, sut.Model.BuildDate, ins.PaymentDate, ins.FixingDates.Select(x => 0.32).ToArray(), ins.Strike, 0.0, OptionType.C);

            Assert.Equal(tbFutPV, pvCube.GetAllRows().First().Value, 1);
        }


    }
}



