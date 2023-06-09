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
using Newtonsoft.Json;
using System.IO;
using Qwack.Providers.CSV;

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
                Currency = TestProviderHelper.CurrencyProvider.GetCurrency("USD"),
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
            var zarCurve = CMEModelBuilder.StripFxBasisCurve(FilenameCMEFwdsXml, "USDZAR", zar, "ZAR.BASIS", new DateTime(2020, 06, 18), usdCurve);

            Assert.Equal(1.0, zarCurve.GetDf(new DateTime(2020, 12, 18), new DateTime(2020, 12, 18)));
        }

        [Fact]
        public void CanStripCurve_Sofr()
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
            var indices = new Dictionary<string, FloatRateIndex> { { "SR3", i } };
            var curves = new Dictionary<string, string> { { "SR3", "USD.SOFR.1B" } };
            var js = JsonSerializer.CreateDefault();
            var parsed = js.Deserialize<List<CMEFileRecord>>(new JsonTextReader(new StringReader(SofrRawData)));
            var fixings = SofrFixings.ToDictionary(x=>DateTime.Parse(x.Key), x => x.Value);
            var curve = CMEModelBuilder.GetCurveForCode(parsed, "SR3", "USD.SOFR.1B", indices, curves, TestProviderHelper.FutureSettingsProvider, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider, fixings);

            Assert.Equal(1.0, curve.GetDf(new DateTime(2020, 12, 18), new DateTime(2020, 12, 18)));
        }

        const string SofrRawData = "[{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202306\",\"matDt\":\"2023-09-20\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2023-09-19\",\"settlePrice\":94.745,\"prevDayVol\":368850,\"prevDayOI\":1280216,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202307\",\"matDt\":\"2023-10-18\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2023-10-17\",\"settlePrice\":94.71,\"prevDayVol\":101,\"prevDayOI\":2390,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202308\",\"matDt\":\"2023-11-15\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2023-11-14\",\"settlePrice\":94.725,\"prevDayVol\":40,\"prevDayOI\":4240,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202309\",\"matDt\":\"2023-12-20\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2023-12-19\",\"settlePrice\":94.78,\"prevDayVol\":303058,\"prevDayOI\":1090492,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202310\",\"matDt\":\"2024-01-17\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2024-01-16\",\"settlePrice\":94.845,\"prevDayVol\":0,\"prevDayOI\":259,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202311\",\"matDt\":\"2024-02-21\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2024-02-20\",\"settlePrice\":94.935,\"prevDayVol\":0,\"prevDayOI\":1,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202312\",\"matDt\":\"2024-03-20\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2024-03-19\",\"settlePrice\":95.015,\"prevDayVol\":290646,\"prevDayOI\":1192470,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202403\",\"matDt\":\"2024-06-20\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2024-06-18\",\"settlePrice\":95.405,\"prevDayVol\":187055,\"prevDayOI\":610290,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202406\",\"matDt\":\"2024-09-18\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2024-09-17\",\"settlePrice\":95.835,\"prevDayVol\":202838,\"prevDayOI\":699597,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202409\",\"matDt\":\"2024-12-18\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2024-12-17\",\"settlePrice\":96.215,\"prevDayVol\":139460,\"prevDayOI\":722688,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202412\",\"matDt\":\"2025-03-19\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2025-03-18\",\"settlePrice\":96.495,\"prevDayVol\":176199,\"prevDayOI\":649254,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202503\",\"matDt\":\"2025-06-18\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2025-06-17\",\"settlePrice\":96.67,\"prevDayVol\":113979,\"prevDayOI\":487914,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202506\",\"matDt\":\"2025-09-17\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2025-09-16\",\"settlePrice\":96.755,\"prevDayVol\":82055,\"prevDayOI\":403642,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202509\",\"matDt\":\"2025-12-17\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2025-12-16\",\"settlePrice\":96.785,\"prevDayVol\":76229,\"prevDayOI\":341927,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202512\",\"matDt\":\"2026-03-18\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2026-03-17\",\"settlePrice\":96.805,\"prevDayVol\":78544,\"prevDayOI\":278076,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202603\",\"matDt\":\"2026-06-17\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2026-06-16\",\"settlePrice\":96.81,\"prevDayVol\":51680,\"prevDayOI\":201117,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202606\",\"matDt\":\"2026-09-16\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2026-09-15\",\"settlePrice\":96.805,\"prevDayVol\":27382,\"prevDayOI\":129436,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202609\",\"matDt\":\"2026-12-16\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2026-12-15\",\"settlePrice\":96.795,\"prevDayVol\":26054,\"prevDayOI\":116312,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202612\",\"matDt\":\"2027-03-17\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2027-03-16\",\"settlePrice\":96.78,\"prevDayVol\":28676,\"prevDayOI\":135248,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202703\",\"matDt\":\"2027-06-16\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2027-06-15\",\"settlePrice\":96.76,\"prevDayVol\":21882,\"prevDayOI\":89844,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202706\",\"matDt\":\"2027-09-15\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2027-09-14\",\"settlePrice\":96.735,\"prevDayVol\":16109,\"prevDayOI\":101363,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202709\",\"matDt\":\"2027-12-15\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2027-12-14\",\"settlePrice\":96.705,\"prevDayVol\":17512,\"prevDayOI\":85186,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202712\",\"matDt\":\"2028-03-15\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2028-03-14\",\"settlePrice\":96.66,\"prevDayVol\":27272,\"prevDayOI\":102449,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202803\",\"matDt\":\"2028-06-21\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2028-06-20\",\"settlePrice\":96.62,\"prevDayVol\":17367,\"prevDayOI\":36622,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202806\",\"matDt\":\"2028-09-20\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2028-09-19\",\"settlePrice\":96.58,\"prevDayVol\":839,\"prevDayOI\":7881,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202809\",\"matDt\":\"2028-12-20\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2028-12-19\",\"settlePrice\":96.54,\"prevDayVol\":121,\"prevDayOI\":2589,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202812\",\"matDt\":\"2029-03-21\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2029-03-20\",\"settlePrice\":96.505,\"prevDayVol\":202,\"prevDayOI\":2287,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202903\",\"matDt\":\"2029-06-20\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2029-06-18\",\"settlePrice\":96.475,\"prevDayVol\":50,\"prevDayOI\":836,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202906\",\"matDt\":\"2029-09-19\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2029-09-18\",\"settlePrice\":96.445,\"prevDayVol\":50,\"prevDayOI\":297,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202909\",\"matDt\":\"2029-12-19\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2029-12-18\",\"settlePrice\":96.425,\"prevDayVol\":16,\"prevDayOI\":94,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"202912\",\"matDt\":\"2030-03-20\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2030-03-19\",\"settlePrice\":96.405,\"prevDayVol\":0,\"prevDayOI\":195,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203003\",\"matDt\":\"2030-06-20\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2030-06-18\",\"settlePrice\":96.39,\"prevDayVol\":0,\"prevDayOI\":80,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203006\",\"matDt\":\"2030-09-18\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2030-09-17\",\"settlePrice\":96.375,\"prevDayVol\":0,\"prevDayOI\":29,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203009\",\"matDt\":\"2030-12-18\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2030-12-17\",\"settlePrice\":96.35,\"prevDayVol\":1,\"prevDayOI\":7,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203012\",\"matDt\":\"2031-03-19\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2031-03-18\",\"settlePrice\":96.33,\"prevDayVol\":0,\"prevDayOI\":5,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203103\",\"matDt\":\"2031-06-18\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2031-06-17\",\"settlePrice\":96.32,\"prevDayVol\":0,\"prevDayOI\":10,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203106\",\"matDt\":\"2031-09-17\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2031-09-16\",\"settlePrice\":96.305,\"prevDayVol\":0,\"prevDayOI\":0,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203109\",\"matDt\":\"2031-12-17\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2031-12-16\",\"settlePrice\":96.28,\"prevDayVol\":0,\"prevDayOI\":0,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203112\",\"matDt\":\"2032-03-17\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2032-03-16\",\"settlePrice\":96.27,\"prevDayVol\":0,\"prevDayOI\":0,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203203\",\"matDt\":\"2032-06-16\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2032-06-15\",\"settlePrice\":96.26,\"prevDayVol\":0,\"prevDayOI\":0,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203206\",\"matDt\":\"2032-09-15\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2032-09-14\",\"settlePrice\":96.25,\"prevDayVol\":0,\"prevDayOI\":0,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203209\",\"matDt\":\"2032-12-15\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2032-12-14\",\"settlePrice\":96.225,\"prevDayVol\":0,\"prevDayOI\":0,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203212\",\"matDt\":\"2033-03-16\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2033-03-15\",\"settlePrice\":96.215,\"prevDayVol\":0,\"prevDayOI\":2,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"},{\"bizDt\":\"2023-06-07\",\"sym\":\"SR3\",\"id\":\"SR3\",\"secTyp\":\"FUT\",\"mmy\":\"203303\",\"matDt\":\"2033-06-15\",\"exch\":\"CME\",\"desc\":\"\",\"lastTrdDt\":\"2033-06-14\",\"settlePrice\":96.205,\"prevDayVol\":0,\"prevDayOI\":0,\"undlyExch\":\"\",\"undlyID\":\"\",\"undlySecTyp\":\"\",\"undlyMMY\":\"\"}]";
        readonly Dictionary<string, double> SofrFixings = new()
        {
            {"2023-06-06",0.0505},
            {"2023-06-05",0.0506},
            {"2023-06-02",0.0507},
            {"2023-06-01",0.0508},
            {"2023-05-31",0.0508},
            {"2023-05-30",0.0506},
            {"2023-05-26",0.0506},
            {"2023-05-25",0.0506},
            {"2023-05-24",0.0505},
            {"2023-05-23",0.0505},
            {"2023-05-22",0.0505},
            {"2023-05-19",0.0505},
            {"2023-05-18",0.0505},
            {"2023-05-17",0.0505},
            {"2023-05-16",0.0505},
            {"2023-05-15",0.0506},
            {"2023-05-12",0.0505},
            {"2023-05-11",0.0505},
            {"2023-05-10",0.0506},
            {"2023-05-09",0.0506},
            {"2023-05-08",0.0506},
            {"2023-05-05",0.0506},
            {"2023-05-04",0.0506},
            {"2023-05-03",0.0481},
            {"2023-05-02",0.0481},
            {"2023-05-01",0.0481},
            {"2023-04-28",0.0481},
            {"2023-04-27",0.0481},
            {"2023-04-26",0.048},
            {"2023-04-25",0.048},
            {"2023-04-24",0.048},
            {"2023-04-21",0.048},
            {"2023-04-20",0.048},
            {"2023-04-19",0.048},
            {"2023-04-18",0.048},
            {"2023-04-17",0.048},
            {"2023-04-14",0.048},
            {"2023-04-13",0.048},
            {"2023-04-12",0.048},
            {"2023-04-11",0.048},
            {"2023-04-10",0.0481},
            {"2023-04-06",0.0481},
            {"2023-04-05",0.0481},
            {"2023-04-04",0.0483},
            {"2023-04-03",0.0484},
            {"2023-03-31",0.0487},
            {"2023-03-30",0.0482},
            {"2023-03-29",0.0483},
            {"2023-03-28",0.0484},
            {"2023-03-27",0.0481},
            {"2023-03-24",0.048},
            {"2023-03-23",0.048},
            {"2023-03-22",0.0455},
            {"2023-03-21",0.0455},
            {"2023-03-20",0.0455},
            {"2023-03-17",0.0455},
            {"2023-03-16",0.0457},
            {"2023-03-15",0.0458},
            {"2023-03-14",0.0455},
            {"2023-03-13",0.0455},
            {"2023-03-10",0.0455},
            {"2023-03-09",0.0455},
            {"2023-03-08",0.0455},
            {"2023-03-07",0.0455},
            {"2023-03-06",0.0455},
            {"2023-03-03",0.0455},
            {"2023-03-02",0.0455},
            {"2023-03-01",0.0455},
            {"2023-02-28",0.0455},
            {"2023-02-27",0.0455},
            {"2023-02-24",0.0455},
            {"2023-02-23",0.0455},
            {"2023-02-22",0.0455},
            {"2023-02-21",0.0455},
            {"2023-02-17",0.0455},
            {"2023-02-16",0.0455},
            {"2023-02-15",0.0455},
            {"2023-02-14",0.0455},
            {"2023-02-13",0.0455},
            {"2023-02-10",0.0455},
            {"2023-02-09",0.0455},
            {"2023-02-08",0.0455},
            {"2023-02-07",0.0455},
            {"2023-02-06",0.0455},
            {"2023-02-03",0.0455},
            {"2023-02-02",0.0456},
            {"2023-02-01",0.0431},
            {"2023-01-31",0.0431},
            {"2023-01-30",0.043},
            {"2023-01-27",0.043},
            {"2023-01-26",0.043},
            {"2023-01-25",0.0431},
            {"2023-01-24",0.043},
            {"2023-01-23",0.043},
            {"2023-01-20",0.043},
            {"2023-01-19",0.0431},
            {"2023-01-18",0.043},
            {"2023-01-17",0.0431},
            {"2023-01-13",0.043},
        };
    }
}
