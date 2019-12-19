using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;
using Qwack.Core.Basic;
using Qwack.Options.Calibrators;

namespace Qwack.Math.Tests.Options.Calibrators
{
    public class NymexCalibratorFacts
    {
        private const string FuturesFilename = "nymex_future_test.csv";
        private const string OptionsFilename = "nymex_option_test.csv";

        [Fact]
        public void CanStripCurve()
        {
            var curve = NYMEXModelBuilder.GetCurveForCode("CL", FuturesFilename, "CL", TestProviderHelper.FutureSettingsProvider, TestProviderHelper.CurrencyProvider);

            Assert.Equal(60.93, curve.GetPriceForDate(new DateTime(2019, 12, 18)));
        }

        [Fact]
        public void CanStripSurface()
        {
            var curve = NYMEXModelBuilder.GetCurveForCode("CL", FuturesFilename, "CL", TestProviderHelper.FutureSettingsProvider, TestProviderHelper.CurrencyProvider);
            var surface = NYMEXModelBuilder.GetSurfaceForCode("LO", OptionsFilename, "CL", curve, TestProviderHelper.CalendarProvider, TestProviderHelper.CurrencyProvider);

            Assert.Equal(0.237771169117785, surface.GetVolForAbsoluteStrike(60.0, new DateTime(2020, 01, 15), 60.85),4);
        }
    }
}
