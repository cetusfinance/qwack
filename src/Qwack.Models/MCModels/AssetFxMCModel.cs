using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Models.Models;
using Qwack.Models.Risk;
using Qwack.Options.VolSurfaces;
using Qwack.Paths;
using Qwack.Paths.Processes;
using Qwack.Paths.Regressors;
using Qwack.Random;
using Qwack.Transport.BasicTypes;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.MCModels
{
    public class AssetFxMCModel : IPvModel
    {
        private readonly string _num = "USD";

        private static string GetSobolFilename() => Path.Combine(GetRunningDirectory(), "SobolDirectionNumbers.txt");
        private static string GetRunningDirectory()
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            return dirPath;
        }
        public PathEngine Engine { get; private set; }
        public DateTime OriginDate { get; }
        public Portfolio Portfolio { get; }
        public IAssetFxModel Model { get; }
        public McSettings Settings { get; }
        public SchwartzSmithTwoFactorModelParameters TwoFactorModelParameters { get; }

        public IAssetFxModel VanillaModel => Model;

        private Dictionary<string, AssetPathPayoff> _payoffs;
        private IPortfolioValueRegressor _regressor;
        private readonly ICurrencyProvider _currencyProvider;
        private readonly IFutureSettingsProvider _futureSettingsProvider;
        private readonly ICalendarProvider _calendarProvider;
        private ExpectedCapitalCalculator _capitalCalc;
        private IDisposable? _cachedRandom;
        private bool _initialized = false;

        private int _priceFactorDepth;

        public AssetFxMCModel(DateTime originDate, Portfolio portfolio, IAssetFxModel model, McSettings settings, ICurrencyProvider currencyProvider, IFutureSettingsProvider futureSettingsProvider, ICalendarProvider calendarProvider, bool deferInitialization = true, SchwartzSmithTwoFactorModelParameters twoFactorModelParameters = null)
        {
            if (settings.CompactMemoryMode && settings.AveragePathCorrection)
                throw new Exception("Can't use both CompactMemoryMode and PathCorrection");

            _currencyProvider = currencyProvider;
            _futureSettingsProvider = futureSettingsProvider;
            _calendarProvider = calendarProvider;
            OriginDate = originDate;
            Portfolio = portfolio;
            Model = model;
            Settings = settings;
            TwoFactorModelParameters = twoFactorModelParameters;

            if (!deferInitialization)
            {
                Initialize();
                _initialized = true;
            }
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            Engine = new PathEngine(Settings.NumberOfPaths)
            {
                Parallelize = Settings.Parallelize,
                CompactMemoryMode = Settings.CompactMemoryMode
            };

            switch (Settings.Generator)
            {
                case RandomGeneratorType.MersenneTwister:
                    Engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                    {
                        UseNormalInverse = true,
                        UseAnthithetic = false
                    });
                    break;
                case RandomGeneratorType.Sobol:
                    if (Settings.GeneratorKey == null)
                    {
                        var directionNumers = new Random.Sobol.SobolDirectionNumbers();
                        Engine.AddPathProcess(new Random.Sobol.SobolPathGenerator(directionNumers, 1)
                        {
                            UseNormalInverse = true
                        });
                    }
                    else
                    {
                        var rand = RandomCache.RegisterForRandom(Settings.GeneratorKey);
                        _cachedRandom = rand;
                        Engine.AddPathProcess((IPathProcess)rand);
                    }
                    break;
                case RandomGeneratorType.Constant:
                    Engine.AddPathProcess(new Random.Constant.Constant()
                    {
                        UseNormalInverse = true,
                    });
                    break;
                case RandomGeneratorType.FlipFlop:
                    Engine.AddPathProcess(new Random.Constant.FlipFlop(Settings.CreditSettings.ConfidenceInterval, true));
                    break;
            }
            Engine.IncrementDepth();

            TimeDependentLmeCorrelations lmeCorrelations = null;
            if (Settings.McModelType == McModelType.LMEForward)
            {
                lmeCorrelations = new TimeDependentLmeCorrelations(Model, _calendarProvider);
                Engine.AddPathProcess(lmeCorrelations);
                Engine.IncrementDepth();
            }
            else if (Model.CorrelationMatrix != null)
            {
                if (Settings.LocalCorrelation)
                    Engine.AddPathProcess(new CholeskyWithTime(Model.CorrelationMatrix, Model));
                else
                    Engine.AddPathProcess(new Cholesky(Model.CorrelationMatrix));
                Engine.IncrementDepth();
            }
           

            var lastDate = Portfolio.LastSensitivityDate;
            var assetIds = Portfolio.AssetIds();
            var assetInstruments = Portfolio.Instruments
               .Where(x => x is IAssetInstrument)
               .Select(x => x as IAssetInstrument);
            var fixingsNeeded = new Dictionary<string, List<DateTime>>();
            foreach (var ins in assetInstruments)
            {
                var fixingsForIns = ins.PastFixingDates(OriginDate);
                if (fixingsForIns.Any())
                {
                    foreach (var kv in fixingsForIns)
                    {
                        if (!fixingsNeeded.ContainsKey(kv.Key))
                            fixingsNeeded.Add(kv.Key, []);
                        fixingsNeeded[kv.Key] = fixingsNeeded[kv.Key].Concat(kv.Value).Distinct().ToList();
                    }
                }

                var fxFixingsForIns = ins.PastFixingDatesFx(Model, OriginDate);
                if (fxFixingsForIns.Any())
                {
                    foreach (var kv in fxFixingsForIns)
                    {
                        if (!fixingsNeeded.ContainsKey(kv.Key))
                            fixingsNeeded.Add(kv.Key, []);
                        fixingsNeeded[kv.Key] = fixingsNeeded[kv.Key].Concat(kv.Value).Distinct().ToList();
                    }
                }
            }

            //asset processes
            _priceFactorDepth = Engine.CurrentDepth;
            var fxAssetsToAdd = new List<string>();
            var corrections = new Dictionary<string, SimpleAveragePathCorrector>();
            foreach (var assetId in assetIds)
            {
                if (assetId.Length == 7 && assetId[3] == '/')
                {
                    fxAssetsToAdd.Add(assetId);
                    continue;
                }

                if (Model.GetVolSurface(assetId) is not IATMVolSurface surface)
                    throw new Exception($"Vol surface for asset {assetId} could not be cast to IATMVolSurface");
                var fixingDict = fixingsNeeded.ContainsKey(assetId) ? Model.GetFixingDictionary(assetId) : null;
                var fixings = fixingDict != null ? fixingDict.ToDictionary(x=>x.Key,x=>x.Value) : new Dictionary<DateTime, double>(); /*fixingDict != null ?
                    fixingsNeeded[assetId].ToDictionary(x => x, x => fixingDict.GetFixing(x))
                    */
                var futuresSim = Settings.ExpensiveFuturesSimulation &&
                   (Model.GetPriceCurve(assetId).CurveType == PriceCurveType.ICE || Model.GetPriceCurve(assetId).CurveType == PriceCurveType.NYMEX);
                if (futuresSim)
                {
                    var fwdCurve = new Func<DateTime, double>(t =>
                    {
                        return Model.GetPriceCurve(assetId).GetPriceForDate(t);
                    });
                    var asset = new BlackFuturesCurve
                    (
                       startDate: OriginDate,
                       expiryDate: lastDate,
                       volSurface: surface,
                       forwardCurve: fwdCurve,
                       nTimeSteps: Settings.NumberOfTimesteps,
                       name: Settings.FuturesMappingTable[assetId],
                       pastFixings: fixings,
                       futureSettingsProvider: _futureSettingsProvider
                    );
                    Engine.AddPathProcess(asset);
                }
                else
                {
                    var fwdCurve = new Func<double, double>(t =>
                    {
                        var c = Model.GetPriceCurve(assetId);
                        var d = OriginDate.AddYearFraction(t, DayCountBasis.ACT365F);
                        if (c is BasicPriceCurve pc)
                            d = d.AddPeriod(RollType.F, pc.SpotCalendar, pc.SpotLag);
                        else if (c is ContangoPriceCurve cc)
                            d = d.AddPeriod(RollType.F, cc.SpotCalendar, cc.SpotLag);
                        return c.GetPriceForDate(d);
                    });

                    IATMVolSurface adjSurface = null;
                    var correlation = 0.0;

                    if (Settings.SimulationCurrency != Model.GetPriceCurve(assetId).Currency)
                    {
                        var fxAdjPair = Settings.SimulationCurrency + "/" + Model.GetPriceCurve(assetId).Currency;
                        var fxAdjPairInv = Model.GetPriceCurve(assetId).Currency + "/" + Settings.SimulationCurrency;
                        if (Model.FundingModel.GetVolSurface(fxAdjPair) is not IATMVolSurface adjSurface2)
                            throw new Exception($"Vol surface for fx pair {fxAdjPair} could not be cast to IATMVolSurface");
                        adjSurface = adjSurface2;
                        if (Model.CorrelationMatrix != null)
                        {
                            if (Model.CorrelationMatrix.TryGetCorrelation(fxAdjPair, assetId, out var correl))
                                correlation = correl;
                            else if (Model.CorrelationMatrix.TryGetCorrelation(fxAdjPairInv, assetId, out var correl2))
                                correlation = -correl2;
                        }
                    }

                    if (Settings.McModelType == McModelType.LocalVol)
                    {
                        var asset = new LVSingleAsset
                        (
                            startDate: OriginDate,
                            expiryDate: lastDate,
                            volSurface: surface,
                            forwardCurve: fwdCurve,
                            nTimeSteps: Settings.NumberOfTimesteps,
                            name: assetId,
                            pastFixings: fixings,
                            fxAdjustSurface: adjSurface,
                            fxAssetCorrelation: correlation
                        );
                        Engine.AddPathProcess(asset);
                    }
                    else if (Settings.McModelType == McModelType.TurboSkew)
                    {
                        var asset = new TurboSkewSingleAsset
                        (
                            startDate: OriginDate,
                            expiryDate: lastDate,
                            volSurface: surface,
                            forwardCurve: fwdCurve,
                            nTimeSteps: Settings.NumberOfTimesteps,
                            name: assetId,
                            pastFixings: fixings,
                            fxAdjustSurface: adjSurface,
                            fxAssetCorrelation: correlation
                        );
                        Engine.AddPathProcess(asset);
                    }
                    else if (Settings.McModelType == McModelType.Commodity2Factor)
                    {
                        var asset = new SchwartzSmithTwoFactorModel
                        (
                            ssParams: TwoFactorModelParameters,
                            startDate: OriginDate,
                            expiryDate: lastDate,
                            nTimeSteps: Settings.NumberOfTimesteps,
                            name: assetId,
                            pastFixings: fixings
                        );
                        Engine.AddPathProcess(asset);
                    }
                    else if (Settings.McModelType == McModelType.LMEForward)
                    {
                        var fwdCurve2 = new Func<DateTime, double>(t =>
                        {
                            return Model.GetPriceCurve(assetId).GetPriceForDate(t);
                        });

                        var asset = new LMEFuturesCurve
                        (
                            startDate: OriginDate,
                            expiryDate: lastDate.NextThirdWednesday(),
                            volSurface: surface,
                            forwardCurve: fwdCurve2,
                            nTimeSteps: Settings.NumberOfTimesteps,
                            name: assetId,
                            pastFixings: fixings,
                            calendarProvider: _calendarProvider,
                            correlations: lmeCorrelations
                        );
                        Engine.AddPathProcess(asset);
                    }
                    else
                    {
                        var asset = new BlackSingleAsset
                        (
                            startDate: OriginDate,
                            expiryDate: lastDate,
                            volSurface: surface,
                            forwardCurve: fwdCurve,
                            nTimeSteps: Settings.NumberOfTimesteps,
                            name: assetId,
                            pastFixings: fixings,
                            fxAdjustSurface: adjSurface,
                            fxAssetCorrelation: correlation
                        );
                        Engine.AddPathProcess(asset);
                    }
                    if (Settings.AveragePathCorrection)
                        corrections.Add(assetId, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(assetId) { CompactMode = Settings.CompactMemoryMode }, surface, fwdCurve, assetId, fixings, adjSurface, correlation));
                }
            }

            //fx pairs
            var pairsAdded = new List<string>();
            var fxPairs = Portfolio.FxPairs(Model).Concat(fxAssetsToAdd);
            var payoutCcys = Portfolio.Instruments.Select(i => i.Currency);
            if (payoutCcys.Any(p => p != Settings.SimulationCurrency))
            {
                var ccysToAdd = payoutCcys.Where(p => p != Settings.SimulationCurrency).Distinct();
                var pairsToAdd = ccysToAdd.Select(c => $"{c.Ccy}/{Settings.SimulationCurrency}");
                fxPairs = fxPairs.Concat(pairsToAdd).Distinct();
            }
            foreach (var fxPair in fxPairs)
            {
                var fxPairName = fxPair;
                var pair = fxPairName.FxPairFromString(_currencyProvider, _calendarProvider);

                if (pairsAdded.Contains(pair.ToString()))
                    continue;

                if (Model.FundingModel.GetVolSurface(fxPairName) is not IATMVolSurface surface)
                    throw new Exception($"Vol surface for fx pair {fxPairName} could not be cast to IATMVolSurface");

                var fixingDict = fixingsNeeded.ContainsKey(fxPairName) ? Model.GetFixingDictionary(fxPairName) : null;
                var fixings = fixingDict != null ? fixingDict.ToDictionary(x => x.Key, x => x.Value) : []; 
           

                var fwdCurve = new Func<double, double>(t =>
                {
                    var date = OriginDate.AddYearFraction(t, DayCountBasis.ACT365F);
                    var spotDate = pair.SpotDate(date);
                    return Model.FundingModel.GetFxRate(spotDate, fxPairName);
                });

                pairsAdded.Add(pair.ToString());


                if (Settings.McModelType == McModelType.LocalVol)
                {

                    if (fxPairName.Substring(fxPairName.Length - 3, 3) != Settings.SimulationCurrency)
                    {
                        var fxAdjPair = Settings.SimulationCurrency + "/" + fxPairName.Substring(fxPairName.Length - 3, 3);
                        if (Model.FundingModel.VolSurfaces[fxAdjPair] is not IATMVolSurface adjSurface)
                            throw new Exception($"Vol surface for fx pair {fxAdjPair} could not be cast to IATMVolSurface");
                        var correlation = fxPair == fxAdjPair ? -1.0 : 0.0;
                        if (correlation != -1.0 && Model.CorrelationMatrix != null)
                        {
                            if (Model.CorrelationMatrix.TryGetCorrelation(fxAdjPair, fxPair, out var correl))
                                correlation = correl;
                        }

                        var asset = new LVSingleAsset
                        (
                            startDate: OriginDate,
                            expiryDate: lastDate,
                            volSurface: surface,
                            forwardCurve: fwdCurve,
                            nTimeSteps: Settings.NumberOfTimesteps,
                            name: fxPairName,
                            fxAdjustSurface: adjSurface,
                            fxAssetCorrelation: correlation,
                            pastFixings: fixings
                        );
                        Engine.AddPathProcess(asset);

                        if (Settings.AveragePathCorrection)
                            corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = Settings.CompactMemoryMode }, surface, fwdCurve, fxPairName, null, adjSurface, correlation));
                    }
                    else
                    {
                        var asset = new LVSingleAsset
                        (
                            startDate: OriginDate,
                            expiryDate: lastDate,
                            volSurface: surface,
                            forwardCurve: fwdCurve,
                            nTimeSteps: Settings.NumberOfTimesteps,
                            name: fxPairName,
                            pastFixings: fixings
                        );
                        Engine.AddPathProcess(asset);

                        if (Settings.AveragePathCorrection)
                            corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = Settings.CompactMemoryMode }, surface, fwdCurve, fxPairName));
                    }
                }
                else if (Settings.McModelType == McModelType.TurboSkew)
                {
                    if (fxPairName.Substring(fxPairName.Length - 3, 3) != Settings.SimulationCurrency)
                    {//needs to be drift-adjusted

                        var fxAdjPair = Settings.SimulationCurrency + "/" + fxPairName.Substring(fxPairName.Length - 3, 3);
                        if (Model.FundingModel.VolSurfaces[fxAdjPair] is not IATMVolSurface adjSurface)
                            throw new Exception($"Vol surface for fx pair {fxAdjPair} could not be cast to IATMVolSurface");
                        var correlation = fxPair == fxAdjPair ? -1.0 : 0.0;
                        if (correlation != -1.0 && Model.CorrelationMatrix != null)
                        {
                            if (Model.CorrelationMatrix.TryGetCorrelation(fxAdjPair, fxPair, out var correl))
                                correlation = correl;
                        }

                        var asset = new TurboSkewSingleAsset
                        (
                           startDate: OriginDate,
                           expiryDate: lastDate,
                           volSurface: surface,
                           forwardCurve: fwdCurve,
                           nTimeSteps: Settings.NumberOfTimesteps,
                           name: fxPairName,
                           fxAdjustSurface: adjSurface,
                           fxAssetCorrelation: correlation,
                           pastFixings: fixings
                        );
                        Engine.AddPathProcess(asset);

                        if (Settings.AveragePathCorrection)
                            corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = Settings.CompactMemoryMode }, surface, fwdCurve, fxPairName, null, adjSurface, correlation));

                    }
                    else
                    {
                        var asset = new TurboSkewSingleAsset
                        (
                           startDate: OriginDate,
                           expiryDate: lastDate,
                           volSurface: surface,
                           forwardCurve: fwdCurve,
                           nTimeSteps: Settings.NumberOfTimesteps,
                           name: fxPairName,
                           pastFixings: fixings
                        );
                        Engine.AddPathProcess(asset);

                        if (Settings.AveragePathCorrection)
                            corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = Settings.CompactMemoryMode }, surface, fwdCurve, fxPairName));

                    }
                }
                else
                {
                    if (fxPairName.Substring(fxPairName.Length - 3, 3) != Settings.SimulationCurrency)
                    {//needs to be drift-adjusted
                        var fxAdjPair = Settings.SimulationCurrency + "/" + fxPairName.Substring(fxPairName.Length - 3, 3);
                        if (Model.FundingModel.VolSurfaces[fxAdjPair] is not IATMVolSurface adjSurface)
                            throw new Exception($"Vol surface for fx pair {fxAdjPair} could not be cast to IATMVolSurface");
                        var correlation = fxPair == fxAdjPair ? -1.0 : 0.0;
                        if (correlation != -1.0 && Model.CorrelationMatrix != null)
                        {
                            if (Model.CorrelationMatrix.TryGetCorrelation(fxAdjPair, fxPair, out var correl))
                                correlation = correl;
                        }

                        var asset = new BlackSingleAsset
                        (
                           startDate: OriginDate,
                           expiryDate: lastDate,
                           volSurface: surface,
                           forwardCurve: fwdCurve,
                           nTimeSteps: Settings.NumberOfTimesteps,
                           name: fxPairName,
                           fxAdjustSurface: adjSurface,
                           fxAssetCorrelation: correlation,
                           pastFixings: fixings
                        );
                        Engine.AddPathProcess(asset);

                        if (Settings.AveragePathCorrection)
                            corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = Settings.CompactMemoryMode }, surface, fwdCurve, fxPairName, null, adjSurface, correlation));

                    }
                    else
                    {
                        var asset = new BlackSingleAsset
                        (
                           startDate: OriginDate,
                           expiryDate: lastDate,
                           volSurface: surface,
                           forwardCurve: fwdCurve,
                           nTimeSteps: Settings.NumberOfTimesteps,
                           name: fxPairName
                        );
                        Engine.AddPathProcess(asset);

                        if (Settings.AveragePathCorrection)
                            corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = Settings.CompactMemoryMode }, surface, fwdCurve, fxPairName));

                    }
                }
            }
            //apply path correction
            if (Settings.AveragePathCorrection && corrections.Any())
            {
                Engine.IncrementDepth();
                foreach (var pc in corrections)
                {
                    Engine.AddPathProcess(pc.Value.PathCalc);
                }
                Engine.IncrementDepth();
                foreach (var pc in corrections)
                {
                    Engine.AddPathProcess(pc.Value);
                }
            }

            //payoffs
            Engine.IncrementDepth();
            _payoffs = assetInstruments.ToDictionary(x => x.TradeId, y => new AssetPathPayoff(y, _currencyProvider, _calendarProvider, Settings.SimulationCurrency));


            //forward price estimators
            var estimatorSpecs = new List<ForwardPriceEstimatorSpec>();
            foreach (var payoff in _payoffs)
            {
                var e = payoff.Value.GetRequiredEstimators(VanillaModel);
                estimatorSpecs.AddRange(e);
            }
            estimatorSpecs = [..estimatorSpecs.Distinct()];
            var specsAdded = false;
            foreach(var spec in estimatorSpecs)
            {
                if(Settings.AvoidRegressionForBackPricing) //use fixed spread
                {
                    var estimator = new FixedSpreadEstimator(spec.AssetId, VanillaModel, spec.ValDate, spec.AverageDates);
                    Engine.AddPathProcess(estimator);
                    spec.Estimator = estimator;
                    Engine.Features.AddPriceEstimator(spec, estimator);
                }
                else
                {
                    var regressionKey = spec.AssetId; //TODO fix for FX
                    var estimator = new LinearAveragePriceRegressor(spec.ValDate, spec.AverageDates, regressionKey);
                    Engine.AddPathProcess(estimator);
                    spec.Estimator = estimator;
                    Engine.Features.AddPriceEstimator(spec, estimator);
                }
                specsAdded = true;
            }
            if (specsAdded)
                Engine.IncrementDepth();

            //adding payoffs to engine
            foreach (var product in _payoffs)
            {
                if (Settings.AvoidRegressionForBackPricing && (product.Value.AssetInstrument is BackPricingOption || product.Value.AssetInstrument is MultiPeriodBackpricingOption || product.Value.AssetInstrument is AsianLookbackOption))
                    product.Value.VanillaModel = VanillaModel;

                Engine.AddPathProcess(product.Value);
            }

            var metricsNeedRegression = new[] { BaseMetric.PFE, BaseMetric.KVA, BaseMetric.CVA, BaseMetric.FVA, BaseMetric.EPE };
            //Need to calculate PFE
            if (Settings.CreditSettings != null && Settings.CreditSettings.ExposureDates != null && Settings.SimulationCurrency != null && metricsNeedRegression.Contains(Settings.CreditSettings.Metric))//setup for PFE, etc
            {
                Engine.IncrementDepth();

                switch (Settings.CreditSettings.PfeRegressorType)
                {
                    case PFERegressorType.MultiLinear:
                        _regressor = new LinearPortfolioValueRegressor(Settings.CreditSettings.ExposureDates,
                            _payoffs.Values.ToArray(), Settings);
                        break;
                    case PFERegressorType.MonoLinear:
                        _regressor = new MonoIndexRegressor(Settings.CreditSettings.ExposureDates,
                            _payoffs.Values.ToArray(), Settings, true);
                        break;
                }
                Engine.AddPathProcess(_regressor);
            }

            //Need to calculate expected capital
            if (Settings.CreditSettings != null && Settings.CreditSettings.ExposureDates != null && Settings.SimulationCurrency != null && Settings.CreditSettings.Metric == BaseMetric.ExpectedCapital)
            {
                Engine.IncrementDepth();
                _capitalCalc = new ExpectedCapitalCalculator(Portfolio, Settings.CreditSettings.CounterpartyRiskWeighting, Settings.CreditSettings.AssetIdToHedgeGroupMap, Settings.SimulationCurrency, VanillaModel, Settings.CreditSettings.ExposureDates);
                Engine.AddPathProcess(_capitalCalc);
            }

            //Engine.AddPathProcess(new OutputPaths(@"C:\Temp\pathz.csv", 0));

            Engine.SetupFeatures();
            _initialized = true;
        }

        public AssetFxMCModel(DateTime originDate, FactorReturnPayoff fp, IAssetFxModel model, McSettings settings, ICurrencyProvider currencyProvider, IFutureSettingsProvider futureSettingsProvider, ICalendarProvider calendarProvider)
        {
            if (settings.CompactMemoryMode && settings.AveragePathCorrection)
                throw new Exception("Can't use both CompactMemoryMode and PathCorrection");

            _currencyProvider = currencyProvider;
            _futureSettingsProvider = futureSettingsProvider;
            _calendarProvider = calendarProvider;
            Engine = new PathEngine(settings.NumberOfPaths)
            {
                Parallelize = settings.Parallelize,
                CompactMemoryMode = settings.CompactMemoryMode
            };
            OriginDate = originDate;
            Model = model;
            Settings = settings;
            switch (settings.Generator)
            {
                case RandomGeneratorType.MersenneTwister:
                    Engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                    {
                        UseNormalInverse = true,
                        UseAnthithetic = false
                    });
                    break;
                case RandomGeneratorType.Sobol:
                    var directionNumers = new Random.Sobol.SobolDirectionNumbers();
                    Engine.AddPathProcess(new Random.Sobol.SobolPathGenerator(directionNumers, 1)
                    {
                        UseNormalInverse = true
                    });
                    break;
                case RandomGeneratorType.Constant:
                    Engine.AddPathProcess(new Random.Constant.Constant()
                    {
                        UseNormalInverse = true,
                    });
                    break;
                case RandomGeneratorType.FlipFlop:
                    Engine.AddPathProcess(new Random.Constant.FlipFlop(settings.CreditSettings.ConfidenceInterval, true));
                    break;
            }
            Engine.IncrementDepth();

            if (model.CorrelationMatrix != null)
            {
                if (settings.LocalCorrelation)
                    Engine.AddPathProcess(new CholeskyWithTime(model.CorrelationMatrix, model));
                else
                    Engine.AddPathProcess(new Cholesky(model.CorrelationMatrix));
                Engine.IncrementDepth();
            }

            var lastDate = fp.SimDates.Max();
            var assetIds = fp.AssetIds;

            //asset processes
            _priceFactorDepth = Engine.CurrentDepth;
            foreach (var assetId in assetIds)
            {
                if (model.GetVolSurface(assetId) is not IATMVolSurface surface)
                    throw new Exception($"Vol surface for asset {assetId} could not be cast to IATMVolSurface");
              
                var futuresSim = settings.ExpensiveFuturesSimulation &&
                   (model.GetPriceCurve(assetId).CurveType == PriceCurveType.ICE || model.GetPriceCurve(assetId).CurveType == PriceCurveType.NYMEX);
                if (futuresSim)
                {
                    var fwdCurve = new Func<DateTime, double>(t =>
                    {
                        return model.GetPriceCurve(assetId).GetPriceForDate(t);
                    });
                    var asset = new BlackFuturesCurve
                    (
                       startDate: originDate,
                       expiryDate: lastDate,
                       volSurface: surface,
                       forwardCurve: fwdCurve,
                       nTimeSteps: settings.NumberOfTimesteps,
                       name: Settings.FuturesMappingTable[assetId],
                       futureSettingsProvider: _futureSettingsProvider
                    );
                    Engine.AddPathProcess(asset);
                }
                else
                {
                    var fwdCurve = new Func<double, double>(t =>
                    {
                        var c = model.GetPriceCurve(assetId);
                        var d = originDate.AddYearFraction(t, DayCountBasis.ACT365F);
                        if (c is BasicPriceCurve pc)
                            d = d.AddPeriod(RollType.F, pc.SpotCalendar, pc.SpotLag);
                        else if (c is ContangoPriceCurve cc)
                            d = d.AddPeriod(RollType.F, cc.SpotCalendar, cc.SpotLag);
                        return c.GetPriceForDate(d);
                    });

                    IATMVolSurface adjSurface = null;
                    var correlation = 0.0;

                    if (settings.SimulationCurrency != model.GetPriceCurve(assetId).Currency)
                    {
                        var fxAdjPair = settings.SimulationCurrency + "/" + model.GetPriceCurve(assetId).Currency;
                        var fxAdjPairInv = model.GetPriceCurve(assetId).Currency + "/" + settings.SimulationCurrency;
                        if (model.FundingModel.GetVolSurface(fxAdjPair) is not IATMVolSurface adjSurface2)
                            throw new Exception($"Vol surface for fx pair {fxAdjPair} could not be cast to IATMVolSurface");
                        adjSurface = adjSurface2;
                        if (model.CorrelationMatrix != null)
                        {
                            if (model.CorrelationMatrix.TryGetCorrelation(fxAdjPair, assetId, out var correl))
                                correlation = correl;
                            else if (model.CorrelationMatrix.TryGetCorrelation(fxAdjPairInv, assetId, out var correl2))
                                correlation = -correl2;
                        }
                    }

                    if (settings.McModelType == McModelType.LocalVol)
                    {
                        var asset = new LVSingleAsset
                        (
                            startDate: originDate,
                            expiryDate: lastDate,
                            volSurface: surface,
                            forwardCurve: fwdCurve,
                            nTimeSteps: settings.NumberOfTimesteps,
                            name: assetId,
                            fxAdjustSurface: adjSurface,
                            fxAssetCorrelation: correlation
                        );
                        Engine.AddPathProcess(asset);
                    }
                    else if (settings.McModelType == McModelType.TurboSkew)
                    {
                        var asset = new TurboSkewSingleAsset
                        (
                            startDate: originDate,
                            expiryDate: lastDate,
                            volSurface: surface,
                            forwardCurve: fwdCurve,
                            nTimeSteps: settings.NumberOfTimesteps,
                            name: assetId,
                            fxAdjustSurface: adjSurface,
                            fxAssetCorrelation: correlation
                        );
                        Engine.AddPathProcess(asset);
                    }
                    else
                    {
                        var asset = new BlackSingleAsset
                        (
                            startDate: originDate,
                            expiryDate: lastDate,
                            volSurface: surface,
                            forwardCurve: fwdCurve,
                            nTimeSteps: settings.NumberOfTimesteps,
                            name: assetId,
                            fxAdjustSurface: adjSurface,
                            fxAssetCorrelation: correlation
                        );
                        Engine.AddPathProcess(asset);
                    }
                }
            }

            //payoff
            Engine.IncrementDepth();
            Engine.AddPathProcess(fp);

            Engine.SetupFeatures();
        }

        public ICube PFE(double confidenceLevel) => PackResults(() => FudgePFE(confidenceLevel), "PFE");

        private double[] FudgePFE(double confidenceLevel)
        {
            var pfe = _regressor.PFE(Model, confidenceLevel);
            if (Settings.CreditSettings.ExposureDates.First() == Model.BuildDate)
            {
                var pv = CleanPV(Settings.SimulationCurrency, null);
                pfe[0] = pv.SumOfAllRows;
            }
            return pfe;
        }

        public ICube EPE() => PackResults(() => _regressor.EPE(Model), "EPE");
        public ICube ENE() => PackResults(() => _regressor.ENE(Model), "ENE");
        public ICube ExpectedCapital() => PackResults(() => _capitalCalc.ExpectedCapital, "ExpCapital");
        public double CVA() => XVACalculator.CVA(Model.BuildDate, EPE(), Settings.CreditSettings.CreditCurve, Settings.CreditSettings.FundingCurve, Settings.CreditSettings.LGD);
        public (double FBA, double FCA) FVA() => XVACalculator.FVA(Model.BuildDate, EPE(), ENE(), Settings.CreditSettings.CreditCurve, Settings.CreditSettings.BaseDiscountCurve, Settings.CreditSettings.FundingCurve);
        public double KVA() => XVACalculator.KVA(Model.BuildDate, ExpectedCapital(), Settings.CreditSettings.FundingCurve);

        public ICube FullPack(double confidenceLevel)
        {
            if (!_initialized)
            {
                Initialize();
            }
            var pfe = PFE(confidenceLevel);
            var epe = EPE();
            var cvaDouble = CVA();
            var (FBA, FCA) = FVA();
            var pv = PV(Settings.SimulationCurrency);
            var cube = pfe.Merge(epe);

            cube.AddRow(new Dictionary<string, object>
            {
                {ExposureDate, DateTime.Today},
                {Metric, "CVA"},
            }, cvaDouble);

            cube.AddRow(new Dictionary<string, object>
            {
                {ExposureDate, DateTime.Today},
                {Metric, "FVA"},
            }, FBA + FCA);

            cube.AddRow(new Dictionary<string, object>
            {
                {ExposureDate, DateTime.Today},
                {Metric, "PV"},
            }, pv.SumOfAllRows);

            return cube;
        }

        private ICube PackResults(Func<double[]> method, string metric)
        {
            if (!_initialized)
            {
                Initialize();
            }

            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { ExposureDate, typeof(DateTime) },
                { Metric, typeof(string) }
            };
            cube.Initialize(dataTypes);
            Engine.RunProcess();

            var e = method.Invoke();

            for (var i = 0; i < e.Length; i++)
            {
                cube.AddRow(new object[] { Settings.CreditSettings.ExposureDates[i], metric }, e[i]);
            }
            return cube;
        }

        private ICube PackResults(Func<Dictionary<DateTime, double>> method, string metric)
        {
            if (!_initialized)
            {
                Initialize();
            }
            Engine.RunProcess();
            var e = method.Invoke();
            return PackResults(e, metric);
        }

        public static ICube PackResults(Dictionary<DateTime, double> data, string metric)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { ExposureDate, typeof(DateTime) },
                { Metric, typeof(string) }
            };
            cube.Initialize(dataTypes);
            foreach (var kv in data)
            {
                cube.AddRow(new object[] { kv.Key, metric }, kv.Value);
            }
            return cube;
        }

        public ICube ExpectedExercise()
        {
            if (!_initialized)
            {
                Initialize();
            }

            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType,  typeof(string) },
                { AssetId, typeof(string) },
                { "PeriodStart", typeof(string) },
                { "PeriodEnd", typeof(string) },
                { Metric, typeof(string) },
                { Consts.Cubes.Portfolio, typeof(string) },
                { "Average", typeof(string) },
            };
        
            cube.Initialize(dataTypes);

            //prime PV
            _ = CleanPV(null, null);

            foreach (var product in _payoffs)
            {
                var expectedEx = product.Value.ExerciseProbabilities;
                var expectedExDates = product.Value.ExercisePeriods;
                var expectedExAverages = product.Value.ExerciseAverages;

                if (expectedEx.Length>0)
                {
                    for (var i = 0; i < expectedEx.Length; i++)
                    {
                        if (product.Value.AssetInstrument is MultiPeriodBackpricingOption bpo)
                        {
                            cube.AddRow(new Dictionary<string, object>
                            {
                                { TradeId, product.Key },
                                { TradeType, bpo.TradeType() },
                                { AssetId, bpo.AssetIds[0] },
                                { "PeriodStart", bpo.PeriodDates[i].Item1.ToString("yyyy-MM-dd") },
                                { "PeriodEnd", bpo.PeriodDates[i].Item2.ToString("yyyy-MM-dd") },
                                { Metric, "ExpectedExercise" },
                                { Consts.Cubes.Portfolio, string.Empty },
                                { "Average", string.Empty },
                            }, expectedEx[i]);
                        }
                        else if(product.Value.AssetInstrument is AsianLookbackOption lbo)
                        {
                           
                            cube.AddRow(new Dictionary<string, object>
                            {
                                { TradeId, product.Key },
                                { TradeType, lbo.TradeType() },
                                { AssetId, lbo.AssetIds[0] },
                                { "PeriodStart",expectedExDates[i].Item1.ToString("yyyy-MM-dd") },
                                { "PeriodEnd", expectedExDates[i].Item2.ToString("yyyy-MM-dd")  },
                                { Metric, "ExpectedExercise" },
                                { Consts.Cubes.Portfolio, string.Empty },
                                { "Average", expectedExAverages[i].ToString() },
                            }, expectedEx[i]);
                        }
                    }
                }
            }
            return cube;
        }



        public ICube PV(Currency reportingCurrency, bool returnFv = false)
        {
            if(!_initialized)
            {
                Initialize();
            }    

            var ccy = reportingCurrency?.ToString();
            ICube cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { Consts.Cubes.Currency, typeof(string) },
                { TradeType, typeof(string) },
                { Consts.Cubes.Portfolio, typeof(string) }
            };
            var insDict = Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            var metaKeys = Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }


            cube.Initialize(dataTypes);
            Engine.RunProcess();
            switch (Settings.CreditSettings.Metric)
            {
                case BaseMetric.CVA:
                    var row = new Dictionary<string, object>
                    {
                        { TradeId, null },
                        { Consts.Cubes.Currency, ccy },
                        { TradeType, null },
                        { Consts.Cubes.Portfolio, null },
                    };
                    cube.AddRow(row, CVA());
                    return cube;
                case BaseMetric.FVA:
                    var rowFVA = new Dictionary<string, object>
                    {
                        { TradeId, null },
                        { Consts.Cubes.Currency, ccy },
                        { TradeType, null },
                        { Consts.Cubes.Portfolio, null },
                    };
                    var (FBA, FCA) = FVA();
                    cube.AddRow(rowFVA, FBA + FCA);
                    return cube;
                default:
                    break;
            }

            cube = CleanPV(reportingCurrency, cube, returnFv);

            return cube;
        }

        public ICube FV(Currency reportingCurrency) => PV(reportingCurrency, true);

        public ICube PV(Currency reportingCurrency) => PV(reportingCurrency, false);

        private ICube CleanPV(Currency reportingCurrency, ICube cube, bool returnFv = false)
        {
            if (!_initialized)
            {
                Initialize();
            }

            var ccy = reportingCurrency?.ToString();

            var insDict = Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            var metaKeys = Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();

            if (cube == null)
            {
                cube = new ResultCube();
                var dataTypes = new Dictionary<string, Type>                
                {
                { TradeId, typeof(string) },
                { Consts.Cubes.Currency, typeof(string) },
                { TradeType, typeof(string) },
                { Consts.Cubes.Portfolio, typeof(string) }
                };
                foreach (var key in metaKeys)
                {
                    dataTypes[key] = typeof(string);
                }

                cube.Initialize(dataTypes);
            }
            Engine.RunProcess();

            foreach (var kv in _payoffs)
            {
                var insQuery = Portfolio.Instruments.Where(x => x.TradeId == kv.Key);
                if (!insQuery.Any())
                    throw new Exception($"Trade with id {kv.Key} not found");
                if (insQuery.Count() != 1)
                    throw new Exception($"Trade with id {kv.Key} has multiple results");
                var ins = insQuery.First();
                var flows = kv.Value.ExpectedFlows(Model);
                var pv = flows.Flows.Sum(x => returnFv ? x.Fv : x.Pv);
                var fxRate = 1.0;
                var tradeId = ins.TradeId;
                string tradeType = null;
                switch (ins)
                {
                    case IAssetInstrument aIns:
                        tradeType = aIns.TradeType();
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, Settings.SimulationCurrency);
                        else
                            ccy = aIns.Currency.ToString();
                        break;
                    default:
                        throw new Exception($"Unable to handle product of type {ins.GetType()}");
                }
                var row = new Dictionary<string, object>
                {
                    { TradeId, tradeId },
                    { Consts.Cubes.Currency, ccy },
                    { TradeType, tradeType },
                    { Consts.Cubes.Portfolio, ins.PortfolioName??string.Empty  },
                };
                if (insDict.TryGetValue(tradeId, out var trade))
                {
                    foreach (var key in metaKeys)
                    {
                        if (trade.MetaData.TryGetValue(key, out var metaData))
                            row[key] = metaData;
                    }
                }

                cube.AddRow(row, pv * fxRate);
            }
            return cube;
        }

        public IPvModel Rebuild(IAssetFxModel newVanillaModel, Portfolio portfolio) => new AssetFxMCModel(OriginDate, portfolio, newVanillaModel, Settings, _currencyProvider, _futureSettingsProvider, _calendarProvider);

        public Dictionary<double, Dictionary<string, double[]>> GetFactorValues()
        {
            var factorValues = new Dictionary<double, Dictionary<string, double[]>>();
            Engine.RunProcess();

            var pathProcesses = Engine.GetProcessesForDepth(_priceFactorDepth);

            foreach (var pathProcess in pathProcesses)
            {
                //pathProcess.
            }

            return factorValues;
        }

        public void Dispose()
        {
            Engine?.Dispose();
            Engine = null;
            _cachedRandom?.Dispose();
            _cachedRandom = null;
            GC.SuppressFinalize(this);
        }

        ~AssetFxMCModel() => Dispose();
    }
}
