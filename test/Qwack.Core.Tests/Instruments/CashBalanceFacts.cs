using System;
using Xunit;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Curves;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Models;
using Qwack.Dates;
using static System.Math;
using Qwack.Core.Basic;
using System.Collections.Generic;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Tests.Instruments
{
    public class CashBalanceFacts
    {
        [Fact]
        public void CashBalanceFact()
        {
            var bd = new DateTime(2019, 06, 14);
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRate = 0.05;
            var rates = pillars.Select(p => flatRate).ToArray();
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var zar = TestProviderHelper.CurrencyProvider["ZAR"];
            var discoCurve = new IrCurve(pillars, rates, bd, "USD.BLAH", Interpolator1DType.Linear, usd);
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, bd, new Dictionary<Currency, double>(), new List<FxPair>(), new Dictionary<Currency, string> { { usd, "X" }, { zar, "Y" } });
            var fModel = new FundingModel(bd, new[] { discoCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            fModel.SetupFx(fxMatrix);
            var aModel = new AssetFxModel(bd, fModel);

            var notional = 100e6;
            var maturity = bd.AddDays(365);
            var b = new CashBalance(usd, notional, null);

            var t = bd.CalculateYearFraction(maturity, DayCountBasis.Act365F);

            var pv = b.Pv(fModel, false);
            Assert.Equal(notional, pv);

            Assert.Empty(b.Dependencies(null));
            Assert.Empty(b.AssetIds);
            Assert.Empty(b.PastFixingDates(bd));
            Assert.Equal(0.0, b.CalculateParRate(null));
            Assert.Equal(0.0, b.FlowsT0(null));
            Assert.Equal(string.Empty, b.FxPair(null));
            Assert.Equal(FxConversionType.None, b.FxType(null));
            Assert.Equal(usd, b.Currency);
            Assert.Equal(DateTime.MinValue, b.LastSensitivityDate);

            var y = (CashBalance)b.Clone();
            Assert.True(b.Equals(y));
            y.TradeId = "xxx";
            Assert.False(b.Equals(y));

            Assert.Throws<NotImplementedException>(() => b.Sensitivities(fModel));
            Assert.Throws<NotImplementedException>(() => b.SetStrike(0.0));
            Assert.Equal(b, b.SetParRate(0.0));

            Assert.Single(b.IrCurves(aModel));
        }

    }
}
