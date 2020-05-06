using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Curves;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Tests.Curves
{
    public class IrCurveFacts
    {
        [Fact]
        public void IrCurveConversionFacts()
        {
            Assert.Equal(1.0, IrCurve.DFFromRate(0.0, 0.1, RateType.Annual));
            Assert.Equal(1.0, IrCurve.DFFromRate(0.0, 0.1, RateType.CC));
            Assert.Equal(1.0, IrCurve.DFFromRate(0.0, 0.1, RateType.Linear));
            Assert.Equal(1.0, IrCurve.DFFromRate(0.0, 0.1, RateType.SemiAnnual));
            Assert.Equal(1.0, IrCurve.DFFromRate(0.0, 0.1, RateType.Quarterly));
            Assert.Equal(1.0, IrCurve.DFFromRate(0.0, 0.1, RateType.Monthly));
            Assert.Equal(0.1, IrCurve.DFFromRate(0.0, 0.1, RateType.DiscountFactor));

            Assert.Equal(0.0, IrCurve.RateFromDF(1.0, 1.0, RateType.Annual));
            Assert.Equal(0.0, IrCurve.RateFromDF(1.0, 1.0, RateType.CC));
            Assert.Equal(0.0, IrCurve.RateFromDF(1.0, 1.0, RateType.Linear));
            Assert.Equal(0.0, IrCurve.RateFromDF(1.0, 1.0, RateType.SemiAnnual));
            Assert.Equal(0.0, IrCurve.RateFromDF(1.0, 1.0, RateType.Quarterly));
            Assert.Equal(0.0, IrCurve.RateFromDF(1.0, 1.0, RateType.Monthly));
            Assert.Equal(1.0, IrCurve.RateFromDF(1.0, 1.0, RateType.DiscountFactor));
        }

        [Fact]
        public void IrCurveFact()
        {
            var z = new ConstantRateIrCurve(0.1, DateTime.Today, "xxx", TestProviderHelper.CurrencyProvider.GetCurrency("USD"));

            Assert.Equal(0.1, z.GetForwardCCRate(DateTime.Today, DateTime.Today.AddDays(10)),12);
            Assert.Equal(0.1, z.GetRates()[0]);
            Assert.Equal(0.2, z.SetRate(0, 0.2, false).GetRate(0));
            Assert.Equal(0.25, z.BumpRateFlat(0.15, false).GetRate(0));
            Assert.Equal(0.25, z.BumpRateFlat(0.15, true).GetRate(0));

            z.SetCollateralSpec("cooo");
            Assert.Equal("cooo", z.CollateralSpec);

            z.SetRateIndex(new FloatRateIndex { ResetTenor = new Dates.Frequency("7b") });
            Assert.Equal(7,z.RateIndex.ResetTenor.PeriodCount);

            Assert.Equal(Interpolator1DType.DummyPoint, z.InterpolatorType);
        }
    }
}
