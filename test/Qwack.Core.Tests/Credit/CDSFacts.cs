using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Credit;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Models.Calibrators;
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

            Assert.Equal(-sut.Notional * sut.Spread, pv);
        }

        [Fact]
        public void CDSBasicFacts_LinearApprox()
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

            var pv = sut.PV_LinearApprox(hz, df, 0.4, false);

            Assert.Equal(-sut.Notional * sut.Spread, pv);
        }

        [Fact]
        public void CDSStripperFacts()
        {
            var origin = new DateTime(2020, 06, 15);
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var df = new ConstantRateIrCurve(0.05, origin, "LIBOR", usd);

            var data = new Tuple<Frequency, double>[]
            {
                new Tuple<Frequency, double>(1.Years(), 0.01 ),
                new Tuple<Frequency, double>(2.Years(), 0.012 ),
                new Tuple<Frequency, double>(3.Years(), 0.013 ),
                new Tuple<Frequency, double>(4.Years(), 0.0135 ),
                new Tuple<Frequency, double>(5.Years(), 0.014 ),
            };

            var cdses = data.Select(d=> new CDS()
            {
                Basis = DayCountBasis.ACT365F,
                Currency = usd,
                OriginDate = origin,
                Tenor = d.Item1,
                Spread = d.Item2,
                Notional = 1e6
            }).ToList();
            
            foreach(var cds in cdses)
            {
                cds.Init();
            }

            var sut = new NewtonRaphsonCreditCurveSolver
            {
                UseSmallSteps = true
            };

            var hz = sut.Solve(cdses, 0.4, df, origin);
           
            var pz = cdses.Select(c => hz.GetSurvivalProbability(c.FinalSensitivityDate));
            foreach(var p in pz)
                Assert.True(!double.IsNaN(p));
        }
    }
}
