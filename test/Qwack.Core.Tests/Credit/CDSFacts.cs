using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Credit;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Core.Tests.Credit
{
    public class CDSFacts
    {
        [Fact]
        public void CDSBasicFacts_NoDefault()
        {
            var origin = new DateTime(2020, 06, 15);
            var hzi = new ConstantHazzardInterpolator(0.0);
            var hz = new HazzardCurve(origin, DayCountBasis.ACT365F, hzi);
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var df = new ConstantRateIrCurve(0.00, origin, "LIBOR", usd);

            var sut = new CDS()
            {
                Basis = DayCountBasis.ACT365F,
                Currency = usd,
                OriginDate = origin,
                Tenor = new Frequency("1y"),
                Spread = 0.01,
                Notional = 1e6
            };
            sut.Init();

            var pv = sut.PV_PiecewiseFlat(hz, df, 0.4, false);

            Assert.Equal(sut.Notional, pv);
        }
    }
}
