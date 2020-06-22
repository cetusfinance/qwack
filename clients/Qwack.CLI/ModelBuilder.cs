using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Compression;
using System.Data;
using System.IO;
using Newtonsoft.Json;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Basic;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Core.Curves;
using Qwack.Models.Calibrators;
using Qwack.Futures;
using Qwack.Dates;
using System.Linq;
using Qwack.Models;
using Qwack.Transport.TransportObjects.MarketData.Models;

namespace Qwack.CLI
{
    public class ModelBuilder
    {
        private const string FilenameCME = "cme.settle.s.csv";
        private const string FilenameCMEFwdsXml = "cme.settle.fwd.s.xml";
        private const string FilenameCBOT = "cbt.settle.s.csv";
        private const string FilenameNymexFuture = "nymex_future.csv";
        private const string FilenameNymexOption = "nymex_option.csv";
        
        private readonly string _filepath;
        private readonly DateTime _valDate;

        public ModelBuilder(string filepath, DateTime valDate)
        {
            _filepath = filepath;
            _valDate = valDate;

            UnzipIfNeeded(FilenameCME);
            UnzipIfNeeded(FilenameCMEFwdsXml);
            UnzipIfNeeded(FilenameCBOT);
            UnzipIfNeeded(FilenameNymexFuture);
            UnzipIfNeeded(FilenameNymexOption);
        }

        public void UnzipIfNeeded(string filename)
        {
            var fullpath = Path.Combine(_filepath, filename);
            var fullpathZip = Path.Combine(_filepath, filename+".zip");
            if (!File.Exists(fullpath) && File.Exists(fullpathZip))
            {
                ZipFile.ExtractToDirectory(fullpathZip, _filepath);
            }
        }

        public AssetFxModel BuildModel(DateTime valDate, ModelBuilderSpec spec, IFutureSettingsProvider futureSettingsProvider, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var indices = spec.RateIndices.ToDictionary(x => x.Key, x => new FloatRateIndex(x.Value, calendarProvider, currencyProvider));
            var fxPairs = spec.FxPairs.Select(x => new FxPair(x, currencyProvider, calendarProvider)).ToList();
            var priceCurves = new List<IPriceCurve>();
            var surfaces = new List<IVolSurface>();
            foreach(var c in spec.NymexSpecs)
            {
                var curve = NYMEXModelBuilder.GetCurveForCode(c.NymexCodeFuture, Path.Combine(_filepath,FilenameNymexFuture), c.QwackCode, futureSettingsProvider, currencyProvider);
                priceCurves.Add(curve);
                if(!string.IsNullOrWhiteSpace(c.NymexCodeOption))
                {
                    var surface = NYMEXModelBuilder.GetSurfaceForCode(c.NymexCodeOption, Path.Combine(_filepath, FilenameNymexOption), c.QwackCode, curve, calendarProvider, currencyProvider);
                    surfaces.Add(surface);
                }
            }
            var irCurves = new Dictionary<string, IrCurve>();
            foreach(var c in spec.CmeBaseCurveSpecs)
            {
                var curve = CMEModelBuilder.GetCurveForCode(c.CMECode, Path.Combine(_filepath, FilenameCME), c.QwackCode, c.CurveName, indices, 
                    new Dictionary<string, string>() { { c.QwackCode, c.CurveName } }, futureSettingsProvider, currencyProvider, calendarProvider);
                irCurves.Add(c.CurveName, curve);
            }
            foreach(var c in spec.CmeBasisCurveSpecs)
            {
                var curve = CMEModelBuilder.StripFxBasisCurve(Path.Combine(_filepath, FilenameCMEFwdsXml), c.FxPair, c.CmeFxPair, currencyProvider.GetCurrency(c.Currency),c.CurveName, valDate, irCurves[c.BaseCurveName], calendarProvider, currencyProvider);
                irCurves.Add(c.CurveName, curve);
            }
            var fm = new FundingModel(valDate, irCurves, currencyProvider, calendarProvider);
            //setup fx
            var pairMap = spec.CmeBasisCurveSpecs.ToDictionary(x => x.FxPair, x => x.CmeFxPair);
            var pairCcyMap = spec.CmeBasisCurveSpecs.ToDictionary(x => x.FxPair, x => currencyProvider.GetCurrency(x.Currency));
            var spotRates = CMEModelBuilder.GetSpotFxRatesFromFwdFile(Path.Combine(_filepath, FilenameCMEFwdsXml), valDate, pairMap, currencyProvider, calendarProvider);
            var spotRatesByCcy = spotRates.ToDictionary(x => pairCcyMap[x.Key], x => x.Value);
            var discountMap = spec.CmeBasisCurveSpecs.ToDictionary(x => pairCcyMap[x.Currency], x => x.CurveName);
            var fxMatrix = new FxMatrix(currencyProvider);
            fxMatrix.Init(
                baseCurrency: currencyProvider.GetCurrency("USD"),
                buildDate: valDate,
                spotRates: spotRatesByCcy,
                fXPairDefinitions: fxPairs,
                discountCurveMap: discountMap);
            fm.SetupFx(fxMatrix);
            var o = new AssetFxModel(valDate, fm);
            return o;
        }

        public static void BuildSampleSpec(string outputFileName)
        {
            var floatRate_Libor3m = new TO_FloatRateIndex()
            {
                Currency = "USD",
                DayCountBasis = DayCountBasis.Act360,
                FixingOffset = "2b",
                HolidayCalendars = "NYC+LON",
                ResetTenor = "3m",
                RollConvention = RollType.MF,
                ResetTenorFixed = "3m"
            };
            var floatRate_FedFunds = new TO_FloatRateIndex()
            {
                Currency = "USD",
                DayCountBasis = DayCountBasis.Act360,
                FixingOffset = "0b",
                HolidayCalendars = "NYC",
                ResetTenor = "1m",
                ResetTenorFixed = "1m",
                RollConvention = RollType.MF,
            };

            var o = new ModelBuilderSpec
            {
                RateIndices = new Dictionary<string, TO_FloatRateIndex>
                {
                    {"USD.LIBOR.3M",floatRate_Libor3m },
                    {"USD.OIS.1B",floatRate_FedFunds },
                },
                NymexSpecs = new List<ModelBuilderSpecNymex>
                {
                    new ModelBuilderSpecNymex {QwackCode="CL",NymexCodeFuture="CL",NymexCodeOption="LO"},
                    new ModelBuilderSpecNymex {QwackCode="CO",NymexCodeFuture="BB",NymexCodeOption="ODB"},
                    new ModelBuilderSpecNymex {QwackCode="NG",NymexCodeFuture="NG",NymexCodeOption="ON"},
                },
                CmeBaseCurveSpecs = new List<ModelBuilderSpecCmeBaseCurve>
                {
                    new ModelBuilderSpecCmeBaseCurve { CMECode="ED", QwackCode="ED", CurveName="USD.LIBOR.3M", FloatRateIndex="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBaseCurve { CMECode="41", QwackCode="FF", CurveName="USD.OIS.1B", FloatRateIndex="USD.OIS.1B"},
                },
                CmeBasisCurveSpecs = new List<ModelBuilderSpecCmeBasisCurve>
                { 
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDZRC", Currency="ZAR", CurveName = "ZAR.DISC.[USD.LIBOR.3M]", FxPair="USDZAR", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDJYC", Currency="JPY", CurveName = "JPY.DISC.[USD.LIBOR.3M]", FxPair="USDJPY", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="EURUSN", Currency="EUR", CurveName = "EUR.DISC.[USD.LIBOR.3M]", FxPair="EURUSD", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="GBPUSN", Currency="GPB", CurveName = "GPB.DISC.[USD.LIBOR.3M]", FxPair="GBPUSD", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDCAC", Currency="CAD", CurveName = "CAD.DISC.[USD.LIBOR.3M]", FxPair="USDCAD", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="AUDUSN", Currency="AUD", CurveName = "AUD.DISC.[USD.LIBOR.3M]", FxPair="AUDUSD", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="NZDUSC", Currency="NZD", CurveName = "NZD.DISC.[USD.LIBOR.3M]", FxPair="NZDUSD", BaseCurveName="USD.LIBOR.3M"}
                },
                FxPairs = new List<TO_FxPair>
                {
                    new TO_FxPair {Domestic="USD", Foreign="ZAR", PrimaryCalendar="ZAR", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="JPY", PrimaryCalendar="JPY", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="EUR", Foreign="USD", PrimaryCalendar="EUR", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="GBP", Foreign="USD", PrimaryCalendar="GBP", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="CAD", PrimaryCalendar="CAD", SecondaryCalendar = "USD", SpotLag="1b"},
                    new TO_FxPair {Domestic="AUD", Foreign="USD", PrimaryCalendar="AUD", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="NZD", Foreign="USD", PrimaryCalendar="NZD", SecondaryCalendar = "USD", SpotLag="2b"},
                }
            };

            var tw = new StringWriter();
            var js = JsonSerializer.Create();
            js.Serialize(tw, o);
            File.WriteAllText(outputFileName, tw.ToString());
        }

        public static ModelBuilderSpec SpecFromFile(string fileName)
        {
            var rawData = File.ReadAllText(fileName);
            var tr = new StringReader(rawData);
            var js = JsonSerializer.Create();
            var o = (ModelBuilderSpec)js.Deserialize(tr, typeof(ModelBuilderSpec));
            return o;
        }

        public static void WriteModelToFile(AssetFxModel model, string fileName)
        {
            var tw = new StringWriter();
            var js = JsonSerializer.Create();
            js.Serialize(tw, model);
            File.WriteAllText(fileName, tw.ToString());
        }
    }
}
