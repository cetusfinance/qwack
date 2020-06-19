using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Models.Calibrators;
using Qwack.Core.Instruments.Funding;
using Qwack.Transport.BasicTypes;

namespace Qwack.Models.Tests.Calibrators
{
    public class CmeCalibratorFacts
    {
        private const string FilenameCME = "cme.settle.s_test.csv";
        private const string FilenameCMEFwdsXml = "cme.settle.fwd.s_test.xml.gz";
        private const string FilenameCBOT = "cbt.settle.s_test.csv";

        [Fact]
        public void CanStripCurve_Eurodollar()
        {
            var i = new FloatRateIndex()
            {
                Currency=TestProviderHelper.CurrencyProvider.GetCurrency("USD"),
                DayCountBasis = DayCountBasis.Act360,
                FixingOffset = 2.Bd(),
                HolidayCalendars = TestProviderHelper.CalendarProvider.Collection["NYC+LON"],
                ResetTenor = 3.Months(),
                RollConvention = RollType.MF
            };
            var indices = new Dictionary<string, FloatRateIndex> { { "ED", i } };
            var curves = new Dictionary<string, string> { { "ED", "USD.LIBOR.3M" } };
            var curve = CMEModelBuilder.GetCurveForCode("ED", FilenameCME, "ED", "USD.LIBOR.3M", indices, curves, TestProviderHelper.FutureSettingsProvider, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            Assert.Equal(1.0, curve.GetDf(new DateTime(2020, 12, 18), new DateTime(2020, 12, 18)));
        }

        [Fact]
        public void CanStripCurve_FedFunds()
        {
            var i = new FloatRateIndex()
            {
                Currency = TestProviderHelper.CurrencyProvider.GetCurrency("USD"),
                DayCountBasis = DayCountBasis.Act360,
                FixingOffset = 0.Bd(),
                HolidayCalendars = TestProviderHelper.CalendarProvider.Collection["NYC+LON"],
                ResetTenor = 1.Months(),
                RollConvention = RollType.MF
            };
            var indices = new Dictionary<string, FloatRateIndex> { { "FF", i } };
            var curves = new Dictionary<string, string> { { "FF", "USD.OIS.1B" } };
            var curve = CMEModelBuilder.GetCurveForCode("41", FilenameCBOT, "FF", "USD.OIS.1B", indices, curves, TestProviderHelper.FutureSettingsProvider, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            Assert.Equal(1.0, curve.GetDf(new DateTime(2020, 12, 18), new DateTime(2020, 12, 18)));
        }

        [Fact]
        public void CanReadXmlFile()
        {
            var sut = CMEModelBuilder.GetSpotFxRatesFromFwdFile(FilenameCMEFwdsXml, new DateTime(2020, 06, 18), TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            Assert.Equal(17.454412, sut["USDZAR"]);
        }

        [Fact]
        public void CanStripCurve_ZarBasis()
        {
            var i = new FloatRateIndex()
            {
                Currency = TestProviderHelper.CurrencyProvider.GetCurrency("USD"),
                DayCountBasis = DayCountBasis.Act360,
                FixingOffset = 2.Bd(),
                HolidayCalendars = TestProviderHelper.CalendarProvider.Collection["NYC+LON"],
                ResetTenor = 3.Months(),
                RollConvention = RollType.MF
            };
            var indices = new Dictionary<string, FloatRateIndex> { { "ED", i } };
            var curves = new Dictionary<string, string> { { "ED", "USD.LIBOR.3M" } };
            var usdCurve = CMEModelBuilder.GetCurveForCode("ED", FilenameCME, "ED", "USD.LIBOR.3M", indices, curves, TestProviderHelper.FutureSettingsProvider, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var zar = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR");
            var zarCurve = CMEModelBuilder.StripFxBasisCurve(FilenameCMEFwdsXml, "USDZAR",zar, "ZAR.BASIS", new DateTime(2020, 06, 18), usdCurve, TestProviderHelper.CalendarProvider, TestProviderHelper.CurrencyProvider);

            Assert.Equal(1.0, zarCurve.GetDf(new DateTime(2020, 12, 18), new DateTime(2020, 12, 18)));
        }
    }
}
