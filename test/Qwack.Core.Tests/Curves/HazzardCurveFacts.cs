using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Curves;
using System.Linq;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Tests.Curves
{
    public class HazzardCurveFacts
    {
        [Fact]
        public void HazzardCurveFact()
        {
            var hzi = new Math.Interpolation.ConstantHazzardInterpolator(0.1);
            var origin = new DateTime(2019, 05, 28);
            var hz = new HazzardCurve(origin, DayCountBasis.Act365F, hzi);

            Assert.Equal(1.0, hz.GetSurvivalProbability(origin, origin));
            Assert.Equal(1.0, hz.GetSurvivalProbability(origin));
            Assert.Equal(hzi.Interpolate(1.0/365.0) , hz.GetSurvivalProbability(origin, origin.AddDays(1)));

            var dfCurve = new ConstantRateIrCurve(0.05, origin, "zzz", TestProviderHelper.CurrencyProvider.GetCurrency("USD"));

            var df = dfCurve.GetDf(origin, origin.AddDays(100));
            Assert.Equal(df, hz.RiskyDiscountFactor(origin, origin.AddDays(100), dfCurve, 0.0));
            Assert.Equal(df*(1.0-(1.0-hzi.Interpolate(100.0/365))*0.5), hz.RiskyDiscountFactor(origin, origin.AddDays(100), dfCurve, 0.5));
        }
    }
}
