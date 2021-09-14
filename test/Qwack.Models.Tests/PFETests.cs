using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Options.VolSurfaces;
using Qwack.Models.Models;
using Qwack.Dates;
using Xunit;
using static System.Math;
using Qwack.Math;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Models.Risk;
using System.Linq;

namespace Qwack.Models.Tests
{

    public class PFETests
    {
        readonly DateTime valDate = DateTime.Parse("2020-05-14");
        readonly string assetId = "CL";
        readonly double assetVol = 0.32;
        readonly double assetPrice = 100;
        readonly Currency usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
        private AssetFxModel GetModel()
        {
            
            var vol = new ConstantVolSurface(valDate, assetVol)
            {
                AssetId = assetId,
                Name = assetId
            };

            var fwd = new ConstantPriceCurve(assetPrice, valDate, TestProviderHelper.CurrencyProvider)
            {
                AssetId = assetId,
                Name = assetId
            };
            var ir = new FlatIrCurve(0.0, usd, "USD");
            var fm = new FundingModel(valDate, new[] { ir }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var am = new AssetFxModel(valDate, fm);
            am.AddPriceCurve(assetId, fwd);
            am.AddVolSurface(assetId, vol);

            return am;
        }

        [Fact]
        public void TurboPFETest()
        {
            var CI = 0.95;
            var am = GetModel();
            var swap = AssetProductFactory.CreateTermAsianSwap(valDate.AddDays(100), valDate.AddDays(100), assetPrice, "CL", null, valDate.AddDays(101), usd);
            swap.DiscountCurve = "USD";
            swap.TradeId = "BLAH";
            var pfe = swap.QuickPFE(CI, am);

            var t = am.BuildDate.CalculateYearFraction(swap.AverageEndDate, Transport.BasicTypes.DayCountBasis.Act365F);
            var pfePrice = assetPrice * Exp(-assetVol * assetVol * t / 2.0 + assetVol * Sqrt(t) * Statistics.NormInv(CI));
            var expectedPfe = (pfePrice - swap.Strike) * swap.Notional;
            Assert.Equal(expectedPfe, pfe, 6);
        }

        [Fact]
        public void QuickPFETest()
        {
            var CI = 0.95;
            var am = GetModel();
            var swap = AssetProductFactory.CreateTermAsianSwap(valDate.AddDays(100), valDate.AddDays(100), assetPrice, "CL", null, valDate.AddDays(101), usd);
            swap.DiscountCurve = "USD";
            swap.TradeId = "BLAH";
            var pf = new Portfolio { Instruments = new List<IInstrument> { swap } };

            var pfeCube = QuickPFECalculator.Calculate(am, pf, CI, usd, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var pfe = pfeCube.GetAllRows().Max(x => x.Value);

            var t = am.BuildDate.CalculateYearFraction(swap.AverageEndDate, Transport.BasicTypes.DayCountBasis.Act365F);
            var pfePrice = assetPrice * Exp(-assetVol * assetVol * t / 2.0 + assetVol * Sqrt(t) * Statistics.NormInv(CI));
            var expectedPfe = (pfePrice - swap.Strike) * swap.Notional;
            Assert.Equal(expectedPfe, pfe, 6);
        }
    }
}
