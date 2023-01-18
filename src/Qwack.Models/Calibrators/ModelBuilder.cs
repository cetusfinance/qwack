using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;
using Qwack.Transport.TransportObjects.MarketData.Models;

namespace Qwack.Models.Calibrators
{
    public class ModelBuilder
    {
        private const string FilenameCme = "cme.settle.s.csv";
        private const string FilenameCmeFwdsXml = "cme.settle.fwd.s.xml";
        private const string FilenameCbot = "cbt.settle.s.csv";
        private const string FilenameNymexFuture = "nymex_future.csv";
        private const string FilenameNymexOption = "nymex_option.csv";
        private const string FilenameCmxFwdsXml = "comex.settle.fwd.s.xml";
        private const string FilenameCmxXml = "comex.settle.s.xml";

        private readonly string _filepath;
        private readonly DateTime _valDate;

        public ModelBuilder(string filepath, DateTime valDate)
        {
            _filepath = filepath;
            _valDate = valDate;

            UnzipIfNeeded(FilenameCme);
            UnzipIfNeeded(FilenameCmeFwdsXml);
            UnzipIfNeeded(FilenameCbot);
            UnzipIfNeeded(FilenameNymexFuture);
            UnzipIfNeeded(FilenameNymexOption);
            UnzipIfNeeded(FilenameCmxFwdsXml);
            UnzipIfNeeded(FilenameCmxXml);
        }

        public void UnzipIfNeeded(string filename)
        {
            var fullpath = Path.Combine(_filepath, filename);
            var fullpathZip = Path.Combine(_filepath, filename + ".zip");
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
            var fxSurfaces = new List<IVolSurface>();

            foreach (var c in spec.NymexSpecs)
            {
                BasicPriceCurve curve;
                try
                {
                    curve = NYMEXModelBuilder.GetCurveForCode(c.NymexCodeFuture, Path.Combine(_filepath, FilenameNymexFuture), c.QwackCode, futureSettingsProvider, currencyProvider, c.PriceCurveType);
                    if (curve != null)
                    {
                        curve.Units = c.Units;
                        priceCurves.Add(curve);
                    }
                }
                catch
                {
                    throw new Exception($"Error building NYMEX curve for {c.QwackCode}");
                }

                if (!string.IsNullOrWhiteSpace(c.NymexCodeOption))
                {
                    try
                    {
                        var surface = NYMEXModelBuilder.GetSurfaceForCode(c.NymexCodeOption, Path.Combine(_filepath, FilenameNymexOption), c.QwackCode, curve, calendarProvider, currencyProvider, futureSettingsProvider);
                        surface.AssetId = c.QwackCode;
                        surfaces.Add(surface);
                    }
                    catch
                    {
                        throw new Exception($"Error building NYMEX vol surface for {c.QwackCode}");
                    }
                }
            }
            foreach (var c in spec.ProxyVols)
            {
                var surfaceTo = (surfaces.Where(s => s.AssetId == c.QwackCodeProxy).First() as RiskyFlySurface).GetTransportObject();
                surfaceTo.AssetId = c.QwackCode;
                var surface = new RiskyFlySurface(surfaceTo, currencyProvider);
                surfaces.Add(surface);
            }
            var irCurves = new Dictionary<string, IIrCurve>();
            foreach (var c in spec.CmeBaseCurveSpecs)
            {
                var ixForThis = new Dictionary<string, FloatRateIndex> { { c.QwackCode, indices[c.FloatRateIndex] } };
                var curve = CMEModelBuilder.GetCurveForCode(c.CmeCode, Path.Combine(_filepath, c.IsCbot ? FilenameCbot : FilenameCme), c.QwackCode, c.CurveName, ixForThis,
                    new Dictionary<string, string>() { { c.QwackCode, c.CurveName } }, futureSettingsProvider, currencyProvider, calendarProvider);
                irCurves.Add(c.CurveName, curve);
            }
            foreach (var c in spec.CmeBasisCurveSpecs)
            {
                var fxPair = fxPairs.Single(x => $"{x.Domestic}{x.Foreign}" == c.FxPair);
                var curve = CMEModelBuilder.StripFxBasisCurve(Path.Combine(_filepath, FilenameCmeFwdsXml), fxPair, c.CmeFxPair, currencyProvider.GetCurrency(c.Currency), c.CurveName, valDate, irCurves[c.BaseCurveName] as IrCurve, currencyProvider, calendarProvider);
                irCurves.Add(c.CurveName, curve);
            }
            foreach (var c in spec.CmeFxFutureSpecs)
            {
                var curve = CMEModelBuilder.GetFuturesCurveForCode(c.CmeCodeFut, Path.Combine(_filepath, FilenameCme), currencyProvider);
                var surface = CMEModelBuilder.GetFxSurfaceForCode(c.CmeCodeOpt, Path.Combine(_filepath, FilenameCme), curve, currencyProvider);
                surface.AssetId = c.FxPair;
                fxSurfaces.Add(surface);
            }

            var pairMap = spec.CmeBasisCurveSpecs.ToDictionary(x => x.FxPair, x => x.CmeFxPair);
            var pairCcyMap = spec.CmeBasisCurveSpecs.ToDictionary(x => x.FxPair, x => currencyProvider.GetCurrency(x.Currency));
            var spotRates = CMEModelBuilder.GetSpotFxRatesFromFwdFile(Path.Combine(_filepath, FilenameCmeFwdsXml), valDate, pairMap, currencyProvider, calendarProvider);
            var discountMap = spec.CmeBasisCurveSpecs.ToDictionary(x => pairCcyMap[x.FxPair], x => x.CurveName);
            discountMap.Add(currencyProvider.GetCurrency("USD"), "USD.LIBOR.3M");

            foreach (var c in spec.CmxMetalCurves)
            {
                var fxPair = fxPairs.Single(x => $"{x.Domestic}{x.Foreign}" == c.MetalPair);
                var (curve, spotPrice) = COMEXModelBuilder.GetMetalCurveForCode(Path.Combine(_filepath, FilenameCmxFwdsXml), c.CmxSymbol, fxPair, c.CurveName, valDate, irCurves[c.BaseCurveName], currencyProvider, calendarProvider);
                irCurves.Add(c.CurveName, curve);
                spotRates.Add(c.MetalPair, spotPrice);
                discountMap.Add(currencyProvider.GetCurrency(c.Currency), c.CurveName);
                pairCcyMap.Add(c.MetalPair, currencyProvider.GetCurrency(c.Currency));
                if (!string.IsNullOrWhiteSpace(c.CmxOptCode))
                {
                    var surface = COMEXModelBuilder.GetMetalSurfaceForCode(c.CmxOptCode, Path.Combine(_filepath, FilenameCmxXml), currencyProvider);
                    surface.AssetId = c.MetalPair;
                    fxSurfaces.Add(surface);
                }
            }

            foreach (var c in spec.FundingCurves)
            {
                var baseCurve = (irCurves[c.BaseCurve] as IrCurve).Clone();
                baseCurve.SolveStage = -1;
                var floatIx = indices[c.FloatRateIndex];
                var fCurve = QuickFundingCurveStripper.StripFlatSpread(c.Name, c.Spread, floatIx, baseCurve, currencyProvider, calendarProvider);
                irCurves.Add(c.Name, fCurve);
            }

            var fm = new FundingModel(valDate, irCurves, currencyProvider, calendarProvider);

            var spotRatesByCcy = spotRates.ToDictionary(x => pairCcyMap[x.Key], x => x.Key.StartsWith("USD") ? x.Value : 1.0 / x.Value);

            var fxMatrix = new FxMatrix(currencyProvider);
            fxMatrix.Init(
                baseCurrency: currencyProvider.GetCurrency("USD"),
                buildDate: valDate,
                spotRates: spotRatesByCcy,
                fXPairDefinitions: fxPairs,
                discountCurveMap: discountMap);
            fm.SetupFx(fxMatrix);
            foreach (var fxs in fxSurfaces)
            {
                fm.VolSurfaces.Add(fxs.AssetId, fxs);
            }
            var o = new AssetFxModel(valDate, fm);
            o.AddVolSurfaces(surfaces.ToDictionary(s => s.AssetId, s => s));
            o.AddPriceCurves(priceCurves.ToDictionary(c => c.AssetId, c => c));
            InjectFakeLme(o, currencyProvider, calendarProvider, valDate);
            return o;
        }

        private static void InjectFakeLme(AssetFxModel model, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider, DateTime valDate)
        {
            var periods = new[] { "2b", "3m", "12m", "36m" };
            var dates = periods.Select(p => valDate.AddPeriod(RollType.LME, calendarProvider.GetCalendar("LON"), new Frequency(p))).ToArray();

            var prices = new Dictionary<string, double[]>()
            {
                {"CA", new double [] {6000,6100,6200,6300} },
                {"AH", new double [] {1700,1750,1800,1850} },
                {"NI", new double [] {14000,14250,14500,14700} },
                {"ZS", new double [] {2300,2325,2350,2375} },
                {"PB", new double [] {1920,1930,1940,1950} },
                {"SN", new double [] {17500,18000,18250,18500} }
            };

            var nContracts = dates.Length;

            var vols = dates.Select(x => new[] { 0.32 }).ToArray();

            foreach (var p in prices)
            {
                var codes = periods.Select(x => p.Key + "-" + x).ToArray();
                var curve = new BasicPriceCurve(valDate, dates, p.Value, PriceCurveType.LME, currencyProvider, periods)
                {
                    AssetId = p.Key,
                    Currency = currencyProvider.GetCurrency("USD"),
                    Name = p.Key
                };
                var volSurface = new GridVolSurface(valDate, new[] { 0.5 }, dates, vols, StrikeType.ForwardDelta, Interpolator1DType.DummyPoint, Interpolator1DType.LinearInVariance, DayCountBasis.Act365F)
                {
                    AssetId = p.Key,
                    Currency = currencyProvider.GetCurrency("USD"),
                    Name = p.Key
                };
                model.AddPriceCurve(p.Key, curve);
                model.AddVolSurface(p.Key, volSurface);
            }
        }

        public static void SaveSampleSpec(string outputFileName)
        {
            var spec = BuildSampleSpec();
            var tw = new StringWriter();
            var js = JsonSerializer.Create();
            js.Serialize(tw, spec);
            File.WriteAllText(outputFileName, tw.ToString());
        }

        public static ModelBuilderSpec BuildSampleSpec()
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
                    new ModelBuilderSpecNymex {QwackCode="CL",NymexCodeFuture="CL",NymexCodeOption="LO", Units=CommodityUnits.bbl, PriceCurveType=PriceCurveType.NYMEX}, //WTI
                    new ModelBuilderSpecNymex {QwackCode="CO",NymexCodeFuture="BB",NymexCodeOption="BZO", Units=CommodityUnits.bbl, PriceCurveType=PriceCurveType.ICE},//Brent
                    new ModelBuilderSpecNymex {QwackCode="DCO",NymexCodeFuture="DC", Units=CommodityUnits.bbl, PriceCurveType=PriceCurveType.NYMEX},//Dubai Crude
                    //new ModelBuilderSpecNymex {QwackCode="Dated",NymexCodeFuture="UB"},//Dated Brent

                    new ModelBuilderSpecNymex {QwackCode="NG",NymexCodeFuture="NG",NymexCodeOption="ON", Units=CommodityUnits.mmbtu, PriceCurveType=PriceCurveType.NYMEX}, //HH
                    new ModelBuilderSpecNymex {QwackCode="UkNbp",NymexCodeFuture="UKG", Units=CommodityUnits.mmbtu, PriceCurveType=PriceCurveType.NYMEX}, //UK Gas

                    new ModelBuilderSpecNymex {QwackCode="HO",NymexCodeFuture="HO",NymexCodeOption="OH", Units=CommodityUnits.gal, PriceCurveType=PriceCurveType.NYMEX}, //Heat
                    new ModelBuilderSpecNymex {QwackCode="XB",NymexCodeFuture="RB",NymexCodeOption="OB", Units=CommodityUnits.gal, PriceCurveType=PriceCurveType.NYMEX}, //RBOB
                    new ModelBuilderSpecNymex {QwackCode="QS",NymexCodeFuture="7F", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.ICE},                       //ICE Gasoil
                    new ModelBuilderSpecNymex {QwackCode="GO.1FOB",NymexCodeFuture="M1B", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.NYMEX},                 //Gasoil 0.1% FOB ARA
                    new ModelBuilderSpecNymex {QwackCode="SingGO",NymexCodeFuture="SG", Units=CommodityUnits.bbl, PriceCurveType=PriceCurveType.NYMEX},                  //Singapore Gasoil

                    new ModelBuilderSpecNymex {QwackCode="Sing0.5",NymexCodeFuture="S5M", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.NYMEX},//0.5% Sing
                    new ModelBuilderSpecNymex {QwackCode="Sing180",NymexCodeFuture="0F", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.NYMEX},//Sing180
                    new ModelBuilderSpecNymex {QwackCode="Sing380",NymexCodeFuture="SE", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.NYMEX},//Sing380
                    new ModelBuilderSpecNymex {QwackCode="NWE3.5",NymexCodeFuture="0D", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.NYMEX},//3.5% NWE
                    new ModelBuilderSpecNymex {QwackCode="NWE1.0",NymexCodeFuture="0B", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.NYMEX},//1.0% NWE
                    new ModelBuilderSpecNymex {QwackCode="NWE0.5",NymexCodeFuture="R5M", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.NYMEX},//0.5% NWE

                    new ModelBuilderSpecNymex {QwackCode="XO",NymexCodeFuture="MFF", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.NYMEX},//API4
                    new ModelBuilderSpecNymex {QwackCode="XA",NymexCodeFuture="MTF", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.NYMEX},//API2
                    new ModelBuilderSpecNymex {QwackCode="IronOre62",NymexCodeFuture="TIO", Units=CommodityUnits.mt, PriceCurveType=PriceCurveType.NYMEX},//62% Iron Ore TSI

                },
                ProxyVols = new List<ModelBuilderSpecProxyAssetVolSurface>
                {
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="UkNbp", QwackCodeProxy="NG"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="DCO", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="QS", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="GO.1FOB", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="SingGO", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="Sing0.5", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="Sing180", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="Sing380", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="NWE3.5", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="NWE1.0", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="NWE0.5", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="XO", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="XA", QwackCodeProxy="CO"},
                    new ModelBuilderSpecProxyAssetVolSurface {QwackCode="IronOre62", QwackCodeProxy="CO"},
                },
                CmeBaseCurveSpecs = new List<ModelBuilderSpecCmeBaseCurve>
                {
                    new ModelBuilderSpecCmeBaseCurve { CmeCode="ED", QwackCode="ED", CurveName="USD.LIBOR.3M", FloatRateIndex="USD.LIBOR.3M", IsCbot=false},
                    new ModelBuilderSpecCmeBaseCurve { CmeCode="41", QwackCode="FF", CurveName="USD.OIS.1B", FloatRateIndex="USD.OIS.1B", IsCbot=true},
                },
                CmeBasisCurveSpecs = new List<ModelBuilderSpecCmeBasisCurve>
                {
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDZRC", Currency="ZAR", CurveName = "ZAR.DISC.[USD.LIBOR.3M]", FxPair="USDZAR", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDJYC", Currency="JPY", CurveName = "JPY.DISC.[USD.LIBOR.3M]", FxPair="USDJPY", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="EURUSN", Currency="EUR", CurveName = "EUR.DISC.[USD.LIBOR.3M]", FxPair="EURUSD", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="GBPUSN", Currency="GBP", CurveName = "GPB.DISC.[USD.LIBOR.3M]", FxPair="GBPUSD", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDCAC", Currency="CAD", CurveName = "CAD.DISC.[USD.LIBOR.3M]", FxPair="USDCAD", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="AUDUSN", Currency="AUD", CurveName = "AUD.DISC.[USD.LIBOR.3M]", FxPair="AUDUSD", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="NZDUSC", Currency="NZD", CurveName = "NZD.DISC.[USD.LIBOR.3M]", FxPair="NZDUSD", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDDKC", Currency="DKK", CurveName = "DKK.DISC.[USD.LIBOR.3M]", FxPair="USDDKK", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDNKC", Currency="NOK", CurveName = "NOK.DISC.[USD.LIBOR.3M]", FxPair="USDNOK", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDSKC", Currency="SEK", CurveName = "SEK.DISC.[USD.LIBOR.3M]", FxPair="USDSEK", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDRUB", Currency="RUB", CurveName = "RUB.DISC.[USD.LIBOR.3M]", FxPair="USDRUB", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDBRL", Currency="BRL", CurveName = "BRL.DISC.[USD.LIBOR.3M]", FxPair="USDBRL", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDCNY", Currency="CNY", CurveName = "CNY.DISC.[USD.LIBOR.3M]", FxPair="USDCNY", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDKRW", Currency="KRW", CurveName = "KRW.DISC.[USD.LIBOR.3M]", FxPair="USDKRW", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDINR", Currency="INR", CurveName = "INR.DISC.[USD.LIBOR.3M]", FxPair="USDINR", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDPHP", Currency="PHP", CurveName = "PHP.DISC.[USD.LIBOR.3M]", FxPair="USDPHP", BaseCurveName="USD.LIBOR.3M"},
                    new ModelBuilderSpecCmeBasisCurve {CmeFxPair="USDTWD", Currency="TWD", CurveName = "TWD.DISC.[USD.LIBOR.3M]", FxPair="USDTWD", BaseCurveName="USD.LIBOR.3M"},
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
                    new TO_FxPair {Domestic="USD", Foreign="DKK", PrimaryCalendar="DKK", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="SEK", PrimaryCalendar="SEK", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="NOK", PrimaryCalendar="NOK", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="RUB", PrimaryCalendar="RUB", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="BRL", PrimaryCalendar="BRL", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="CNY", PrimaryCalendar="CNY", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="KRW", PrimaryCalendar="KRW", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="INR", PrimaryCalendar="INR", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="PHP", PrimaryCalendar="PHP", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="USD", Foreign="TWD", PrimaryCalendar="TWD", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="BTC", Foreign="USD", PrimaryCalendar="USD", SecondaryCalendar = "USD", SpotLag="0b"},
                    new TO_FxPair {Domestic="XAU", Foreign="USD", PrimaryCalendar="LON", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="XAG", Foreign="USD", PrimaryCalendar="LON", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="XPT", Foreign="USD", PrimaryCalendar="LON", SecondaryCalendar = "USD", SpotLag="2b"},
                    new TO_FxPair {Domestic="XPD", Foreign="USD", PrimaryCalendar="LON", SecondaryCalendar = "USD", SpotLag="2b"},
                },
                CmeFxFutureSpecs = new List<ModelBuilderSpecFxFuture>
                {
                    new ModelBuilderSpecFxFuture {CmeCodeFut="6E",CmeCodeOpt="EUU",Currency="USD",FxPair="EURUSD"},
                    new ModelBuilderSpecFxFuture {CmeCodeFut="6B",CmeCodeOpt="GBU",Currency="USD",FxPair="GBPUSD"},
                    new ModelBuilderSpecFxFuture {CmeCodeFut="6J",CmeCodeOpt="JPU",Currency="USD",FxPair="JPYUSD"},
                    new ModelBuilderSpecFxFuture {CmeCodeFut="6A",CmeCodeOpt="ADU",Currency="USD",FxPair="AUDUSD"},
                    new ModelBuilderSpecFxFuture {CmeCodeFut="6C",CmeCodeOpt="CAU",Currency="USD",FxPair="CADUSD"},
                    new ModelBuilderSpecFxFuture {CmeCodeFut="6L",CmeCodeOpt="BR",Currency="USD",FxPair="BRLUSD"},
                    new ModelBuilderSpecFxFuture {CmeCodeFut="BTC",CmeCodeOpt="BTC",Currency="USD",FxPair="BTCUSD"},
                },
                CmxMetalCurves = new List<ModelBuilderSpecCmxMetalCurve>
                {
                    new ModelBuilderSpecCmxMetalCurve { Currency="XAU", MetalPair="XAUUSD", CmxSymbol="GB", CurveName="XAU.DISC.[USD.LIBOR.3M]", BaseCurveName = "USD.LIBOR.3M", CmxFutCode="GC", CmxOptCode="OG" },
                    new ModelBuilderSpecCmxMetalCurve { Currency="XAG", MetalPair="XAGUSD", CmxSymbol="LSF", CurveName="XAG.DISC.[USD.LIBOR.3M]", BaseCurveName = "USD.LIBOR.3M", CmxFutCode="SI", CmxOptCode="SO" },
                },
                FundingCurves = new List<ModelBuilderSpecFundingCurve>
                {
                    new ModelBuilderSpecFundingCurve {Name="USD.FUNDING.3M", BaseCurve="USD.LIBOR.3M", FloatRateIndex="USD.LIBOR.3M", Spread=0.01}
                }
            };

            return o;
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
            var to = model.ToTransportObject();
            js.Serialize(tw, to);
            File.WriteAllText(fileName, tw.ToString());
        }
    }
}
