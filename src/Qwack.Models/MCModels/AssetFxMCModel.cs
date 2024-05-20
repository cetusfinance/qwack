using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Qwack.Paths.Payoffs;
using Qwack.Paths.Processes;
using Qwack.Paths.Regressors;
using Qwack.Transport.BasicTypes;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.MCModels
{
    public class AssetFxMCModel : IPvModel, IDisposable
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
        public PathEngine Engine { get; }
        public DateTime OriginDate { get; }
        public Portfolio Portfolio { get; }
        public IAssetFxModel Model { get; }
        public McSettings Settings { get; }

        public IAssetFxModel VanillaModel => Model;

        private readonly Dictionary<string, AssetPathPayoff> _payoffs;
        private readonly IPortfolioValueRegressor _regressor;
        private readonly ICurrencyProvider _currencyProvider;
        private readonly IFutureSettingsProvider _futureSettingsProvider;
        private readonly ICalendarProvider _calendarProvider;
        private readonly ExpectedCapitalCalculator _capitalCalc;

        private int _priceFactorDepth;

        public AssetFxMCModel(DateTime originDate, Portfolio portfolio, IAssetFxModel model, McSettings settings, ICurrencyProvider currencyProvider, IFutureSettingsProvider futureSettingsProvider, ICalendarProvider calendarProvider)
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
            Portfolio = portfolio;
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
                    var directionNumers = new Random.Sobol.SobolDirectionNumbers(GetSobolFilename());
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

            var lastDate = portfolio.LastSensitivityDate;
            var assetIds = portfolio.AssetIds();
            var assetInstruments = portfolio.Instruments
               .Where(x => x is IAssetInstrument)
               .Select(x => x as IAssetInstrument);
            var fixingsNeeded = new Dictionary<string, List<DateTime>>();
            foreach (var ins in assetInstruments)
            {
                var fixingsForIns = ins.PastFixingDates(originDate);
                if (fixingsForIns.Any())
                {
                    foreach (var kv in fixingsForIns)
                    {
                        if (!fixingsNeeded.ContainsKey(kv.Key))
                            fixingsNeeded.Add(kv.Key, new List<DateTime>());
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

                if (model.GetVolSurface(assetId) is not IATMVolSurface surface)
                    throw new Exception($"Vol surface for asset {assetId} could not be cast to IATMVolSurface");
                var fixingDict = fixingsNeeded.ContainsKey(assetId) ? model.GetFixingDictionary(assetId) : null;
                var fixings = fixingDict != null ?
                    fixingsNeeded[assetId].ToDictionary(x => x, x => fixingDict.GetFixing(x))
                    : new Dictionary<DateTime, double>();
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
                       pastFixings: fixings,
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

                    if (settings.ReportingCurrency != model.GetPriceCurve(assetId).Currency)
                    {
                        var fxAdjPair = settings.ReportingCurrency + "/" + model.GetPriceCurve(assetId).Currency;
                        var fxAdjPairInv = model.GetPriceCurve(assetId).Currency + "/" + settings.ReportingCurrency;
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
                            pastFixings: fixings,
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
                            pastFixings: fixings,
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
                            pastFixings: fixings,
                            fxAdjustSurface: adjSurface,
                            fxAssetCorrelation: correlation
                        );
                        Engine.AddPathProcess(asset);
                    }
                    if (settings.AveragePathCorrection)
                        corrections.Add(assetId, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(assetId) { CompactMode = settings.CompactMemoryMode }, surface, fwdCurve, assetId, fixings, adjSurface, correlation));
                }
            }

            //fx pairs
            var pairsAdded = new List<string>();
            var fxPairs = portfolio.FxPairs(model).Concat(fxAssetsToAdd);
            var payoutCcys = portfolio.Instruments.Select(i => i.Currency);
            if (payoutCcys.Any(p => p != settings.ReportingCurrency))
            {
                var ccysToAdd = payoutCcys.Where(p => p != settings.ReportingCurrency).Distinct();
                var pairsToAdd = ccysToAdd.Select(c => $"{c.Ccy}/{settings.ReportingCurrency}");
                fxPairs = fxPairs.Concat(pairsToAdd).Distinct();
            }
            foreach (var fxPair in fxPairs)
            {
                var fxPairName = fxPair;
                var pair = fxPairName.FxPairFromString(_currencyProvider, _calendarProvider);

                if (pairsAdded.Contains(pair.ToString()))
                    continue;

                if (model.FundingModel.GetVolSurface(fxPairName) is not IATMVolSurface surface)
                    throw new Exception($"Vol surface for fx pair {fxPairName} could not be cast to IATMVolSurface");

                var fwdCurve = new Func<double, double>(t =>
                {
                    var date = originDate.AddYearFraction(t, DayCountBasis.ACT365F);
                    var spotDate = pair.SpotDate(date);
                    return model.FundingModel.GetFxRate(spotDate, fxPairName);
                });

                pairsAdded.Add(pair.ToString());

                if (settings.McModelType == McModelType.LocalVol)
                {
                    var asset = new LVSingleAsset
                    (
                        startDate: originDate,
                        expiryDate: lastDate,
                        volSurface: surface,
                        forwardCurve: fwdCurve,
                        nTimeSteps: settings.NumberOfTimesteps,
                        name: fxPairName
                    );
                    Engine.AddPathProcess(asset);

                    if (settings.AveragePathCorrection)
                        corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = settings.CompactMemoryMode }, surface, fwdCurve, fxPairName));

                }
                else if (settings.McModelType == McModelType.TurboSkew)
                {
                    if (fxPairName.Substring(fxPairName.Length - 3, 3) != settings.ReportingCurrency)
                    {//needs to be drift-adjusted
                        var fxAdjPair = settings.ReportingCurrency + "/" + fxPairName.Substring(fxPairName.Length - 3, 3);
                        if (model.FundingModel.VolSurfaces[fxAdjPair] is not IATMVolSurface adjSurface)
                            throw new Exception($"Vol surface for fx pair {fxAdjPair} could not be cast to IATMVolSurface");
                        var correlation = fxPair == fxAdjPair ? -1.0 : 0.0;
                        if (correlation != -1.0 && model.CorrelationMatrix != null)
                        {
                            if (model.CorrelationMatrix.TryGetCorrelation(fxAdjPair, fxPair, out var correl))
                                correlation = correl;
                        }

                        var asset = new TurboSkewSingleAsset
                        (
                           startDate: originDate,
                           expiryDate: lastDate,
                           volSurface: surface,
                           forwardCurve: fwdCurve,
                           nTimeSteps: settings.NumberOfTimesteps,
                           name: fxPairName,
                           fxAdjustSurface: adjSurface,
                           fxAssetCorrelation: correlation
                        );
                        Engine.AddPathProcess(asset);

                        if (settings.AveragePathCorrection)
                            corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = settings.CompactMemoryMode }, surface, fwdCurve, fxPairName, null, adjSurface, correlation));

                    }
                    else
                    {
                        var asset = new TurboSkewSingleAsset
                        (
                           startDate: originDate,
                           expiryDate: lastDate,
                           volSurface: surface,
                           forwardCurve: fwdCurve,
                           nTimeSteps: settings.NumberOfTimesteps,
                           name: fxPairName
                        );
                        Engine.AddPathProcess(asset);

                        if (settings.AveragePathCorrection)
                            corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = settings.CompactMemoryMode }, surface, fwdCurve, fxPairName));

                    }
                }
                else
                {
                    if (fxPairName.Substring(fxPairName.Length - 3, 3) != settings.ReportingCurrency)
                    {//needs to be drift-adjusted
                        var fxAdjPair = settings.ReportingCurrency + "/" + fxPairName.Substring(fxPairName.Length - 3, 3);
                        if (model.FundingModel.GetVolSurface(fxAdjPair) is not IATMVolSurface adjSurface)
                            throw new Exception($"Vol surface for fx pair {fxAdjPair} could not be cast to IATMVolSurface");
                        var correlation = fxPair == fxAdjPair ? -1.0 : 0.0;
                        if (correlation != -1.0 && model.CorrelationMatrix != null)
                        {
                            if (model.CorrelationMatrix.TryGetCorrelation(fxAdjPair, fxPair, out var correl))
                                correlation = correl;
                        }

                        var asset = new BlackSingleAsset
                        (
                           startDate: originDate,
                           expiryDate: lastDate,
                           volSurface: surface,
                           forwardCurve: fwdCurve,
                           nTimeSteps: settings.NumberOfTimesteps,
                           name: fxPairName,
                           fxAdjustSurface: adjSurface,
                           fxAssetCorrelation: correlation
                        );
                        Engine.AddPathProcess(asset);

                        if (settings.AveragePathCorrection)
                            corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = settings.CompactMemoryMode }, surface, fwdCurve, fxPairName, null, adjSurface, correlation));

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
                           name: fxPairName
                        );
                        Engine.AddPathProcess(asset);

                        if (settings.AveragePathCorrection)
                            corrections.Add(fxPairName, new SimpleAveragePathCorrector(new SimpleAveragePathCalculator(fxPairName) { CompactMode = settings.CompactMemoryMode }, surface, fwdCurve, fxPairName));

                    }
                }
            }
            //apply path correctin
            if (settings.AveragePathCorrection && corrections.Any())
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
            _payoffs = assetInstruments.ToDictionary(x => x.TradeId, y => new AssetPathPayoff(y, _currencyProvider, _calendarProvider, settings.ReportingCurrency));
            if (!settings.AvoidRegressionForBackPricing && _payoffs.Any(x => x.Value.Regressors != null))
            {
                //var regressorsToAdd = _payoffs.Where(x => x.Value.Regressors != null)
                //    .SelectMany(x => x.Value.Regressors)
                //    .Distinct();

                //foreach (var regressor in regressorsToAdd)
                //{
                //    Engine.AddPathProcess(regressor);
                //    foreach (var payoff in _payoffs.Where(x => x.Value.Regressors != null))
                //    {
                //        if (payoff.Value.Regressors.Any(x => x == regressor))
                //            payoff.Value.SetRegressor(regressor);
                //    }
                //}

                //Engine.IncrementDepth();



                foreach (var payoff in _payoffs.Where(x => x.Value.Regressors != null))
                {
                    foreach(var regressor in payoff.Value.Regressors)
                        Engine.AddPathProcess(regressor);
                    //foreach (var payoff in _payoffs.Where(x => x.Value.Regressors != null))
                    //{
                    //    if (payoff.Value.Regressors.Any(x => x == regressor))
                    //        payoff.Value.SetRegressor(regressor);
                    //}
                }

                Engine.IncrementDepth();
            }
            foreach (var product in _payoffs)
            {
                if (settings.AvoidRegressionForBackPricing && (product.Value.AssetInstrument is Core.Instruments.Asset.BackPricingOption || product.Value.AssetInstrument is MultiPeriodBackpricingOption))
                    product.Value.VanillaModel = VanillaModel;

                Engine.AddPathProcess(product.Value);
            }

            var metricsNeedRegression = new[] { BaseMetric.PFE, BaseMetric.KVA, BaseMetric.CVA, BaseMetric.FVA, BaseMetric.EPE };
            //Need to calculate PFE
            if (settings.CreditSettings != null && settings.CreditSettings.ExposureDates != null && settings.ReportingCurrency != null && metricsNeedRegression.Contains(settings.CreditSettings.Metric))//setup for PFE, etc
            {
                Engine.IncrementDepth();

                switch (settings.CreditSettings.PfeRegressorType)
                {
                    case PFERegressorType.MultiLinear:
                        _regressor = new LinearPortfolioValueRegressor(settings.CreditSettings.ExposureDates,
                            _payoffs.Values.ToArray(), settings);
                        break;
                    case PFERegressorType.MonoLinear:
                        _regressor = new MonoIndexRegressor(settings.CreditSettings.ExposureDates,
                            _payoffs.Values.ToArray(), settings, true);
                        break;
                }
                Engine.AddPathProcess(_regressor);
            }

            //Need to calculate expected capital
            if (settings.CreditSettings != null && settings.CreditSettings.ExposureDates != null && settings.ReportingCurrency != null && settings.CreditSettings.Metric == BaseMetric.ExpectedCapital)
            {
                Engine.IncrementDepth();
                _capitalCalc = new ExpectedCapitalCalculator(Portfolio, settings.CreditSettings.CounterpartyRiskWeighting, settings.CreditSettings.AssetIdToHedgeGroupMap, settings.ReportingCurrency, VanillaModel, settings.CreditSettings.ExposureDates);
                Engine.AddPathProcess(_capitalCalc);
            }

            //Engine.AddPathProcess(new OutputPaths(@"C:\Temp\pathz.csv", 0));

            Engine.SetupFeatures();
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
                    var directionNumers = new Random.Sobol.SobolDirectionNumbers(GetSobolFilename());
                    Engine.AddPathProcess(new Random.Sobol.SobolPathGenerator(directionNumers, 1000)
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

                    if (settings.ReportingCurrency != model.GetPriceCurve(assetId).Currency)
                    {
                        var fxAdjPair = settings.ReportingCurrency + "/" + model.GetPriceCurve(assetId).Currency;
                        var fxAdjPairInv = model.GetPriceCurve(assetId).Currency + "/" + settings.ReportingCurrency;
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
                var pv = CleanPV(Settings.ReportingCurrency, null);
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
            var pfe = PFE(confidenceLevel);
            var epe = EPE();
            var cvaDouble = CVA();
            var (FBA, FCA) = FVA();
            var pv = PV(Settings.ReportingCurrency);
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
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType,  typeof(string) },
                { AssetId, typeof(string) },
                { "PeriodStart", typeof(DateTime) },
                { "PeriodEnd", typeof(string) },
                { Metric, typeof(string) },
                { "RefPrice", typeof(double) },
                { Consts.Cubes.Portfolio, typeof(string) },
            };
        
            cube.Initialize(dataTypes);

            //prime PV
            _ = CleanPV(null, null);

            foreach (var product in _payoffs)
            {
                var expectedEx = product.Value.ExerciseProbabilities;
                if(expectedEx.Length>0)
                {
                    for (var i = 0; i < expectedEx.Length; i++)
                    {
                        var bpo = product.Value.AssetInstrument as MultiPeriodBackpricingOption;
                        cube.AddRow(new Dictionary<string, object>
                        {
                            { TradeId, product.Key },
                            { TradeType, bpo.TradeType() },
                            { AssetId, bpo.AssetIds[0] },
                            { "PeriodStart", bpo.PeriodDates[i].Item1 },
                            { "PeriodEnd", bpo.PeriodDates[i].Item2 },
                            { Metric, "ExpectedExercise" },
                        }, expectedEx[i]);
                    }
                }
            }
            return cube;
        }

        public ICube PV(Currency reportingCurrency)
        {
            var ccy = reportingCurrency?.ToString();
            ICube cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { Consts.Cubes.Currency, typeof(string) },
                { TradeType, typeof(string) },
                { Consts.Cubes.Portfolio, typeof(string) }
            };
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

            cube = CleanPV(reportingCurrency, cube);

            return cube;
        }

        private ICube CleanPV(Currency reportingCurrency, ICube cube)
        {
            var ccy = reportingCurrency?.ToString();


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
                var pv = flows.Flows.Sum(x => x.Pv);
                var fxRate = 1.0;
                var tradeId = ins.TradeId;
                string tradeType = null;
                switch (ins)
                {
                    case IAssetInstrument aIns:
                        tradeType = aIns.TradeType();
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, Settings.ReportingCurrency);
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
                cube.AddRow(row, pv / fxRate);
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
        }
    }
}
