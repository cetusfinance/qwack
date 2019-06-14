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

namespace Qwack.Core.Tests.Instruments
{
    public class CashBalanceFacts
    {
        [Fact]
        public void CashBalanceFact()
        {
            var bd = DateTime.Today;
            var pillars = new[] { bd, bd.AddDays(1000) };
            var flatRate = 0.05;
            var rates = pillars.Select(p => flatRate).ToArray();
            var usd = TestProviderHelper.CurrencyProvider["USD"];
            var discoCurve = new IrCurve(pillars, rates, bd, "USD.BLAH", Interpolator1DType.Linear, usd);
            var fModel = new FundingModel(bd, new[] { discoCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var notional = 100e6;
            var maturity = bd.AddDays(365);
            var b = new CashBalance(usd, notional, null);

            var t = bd.CalculateYearFraction(maturity, DayCountBasis.Act365F);

            var pv = b.Pv(fModel, false);
            Assert.Equal(notional, pv);

            Assert.Empty(b.Dependencies(null));
            Assert.Empty(b.AssetIds);
            Assert.Empty(b.PastFixingDates(bd));
            Assert.Null(b.ExpectedCashFlows(null).Flows); 
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
            Assert.Equal(b, b.SetParRate(0.0));
        }

    }
}
