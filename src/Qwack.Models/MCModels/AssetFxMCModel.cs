using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;
using Qwack.Core.Instruments;
using Qwack.Math.Extensions;
using Qwack.Paths;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Paths.Processes;
using Qwack.Dates;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Options.VolSurfaces;
using System.IO;
using System.Reflection;
using Qwack.Paths.Regressors;
using Qwack.Futures;
using Qwack.Utils.Parallel;
using Qwack.Core.Descriptors;

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
        public PathEngine Engine { get; }
        public DateTime OriginDate { get; }
        public Portfolio Portfolio { get; }
        public IAssetFxModel Model { get; }
        public McSettings Settings { get; }

        public List<MarketDataDescriptor> Descriptors => throw new NotImplementedException();
        public List<MarketDataDescriptor> Dependencies => Model.Dependencies;
        public Dictionary<MarketDataDescriptor, object> DependentReferences => new Dictionary<MarketDataDescriptor, object>();

        public IAssetFxModel VanillaModel => Model;

        private Dictionary<string, AssetPathPayoff> _payoffs;
        private IPortfolioValueRegressor _regressor;
        private readonly ICurrencyProvider _currencyProvider;
        private readonly IFutureSettingsProvider _futureSettingsProvider;
        private readonly ICalendarProvider _calendarProvider;

        public AssetFxMCModel(DateTime originDate, Portfolio portfolio, IAssetFxModel model, McSettings settings, ICurrencyProvider currencyProvider, IFutureSettingsProvider futureSettingsProvider, ICalendarProvider calendarProvider)
        {
            _currencyProvider = currencyProvider;
            _futureSettingsProvider = futureSettingsProvider;
            _calendarProvider = calendarProvider;
            Engine = new PathEngine(settings.NumberOfPaths)
            {
                Parallelize = settings.Parallelize
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
                    Engine.AddPathProcess(new Random.Sobol.SobolPathGenerator(directionNumers, 1000)
                    {
                        UseNormalInverse = true
                    });
                    break;
            }
            Engine.IncrementDepth();

            if (model.CorrelationMatrix != null)
            {
                Engine.AddPathProcess(new Cholesky(model.CorrelationMatrix));
                Engine.IncrementDepth();
            }
                
            var lastDate = portfolio.LastSensitivityDate();
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
            foreach (var assetId in assetIds)
            {
                if (!(model.GetVolSurface(assetId) is IATMVolSurface surface))
                    throw new Exception($"Vol surface for asset {assetId} could not be cast to IATMVolSurface");
                var fixingDict = fixingsNeeded.ContainsKey(assetId) ? model.GetFixingDictionary(assetId) : null;
                var fixings = fixingDict != null ?
                    fixingsNeeded[assetId].ToDictionary(x => x, x => fixingDict[x])
                    : new Dictionary<DateTime, double>();
                var futuresSim = settings.ExpensiveFuturesSimulation &&
                   (model.GetPriceCurve(assetId).CurveType == Core.Curves.PriceCurveType.ICE || model.GetPriceCurve(assetId).CurveType == Core.Curves.PriceCurveType.NYMEX);
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
                        return model
                        .GetPriceCurve(assetId)
                        .GetPriceForDate(originDate.AddYearFraction(t, DayCountBasis.ACT365F));
                    });

                    if (settings.LocalVol)
                    {
                        if (settings.ReportingCurrency != model.GetPriceCurve(assetId).Currency)
                        {
                            var fxAdjPair = model.GetPriceCurve(assetId).Currency + "/" + settings.ReportingCurrency;
                            if (!(model.FundingModel.VolSurfaces[fxAdjPair] is IATMVolSurface adjSurface))
                                throw new Exception($"Vol surface for fx pair {fxAdjPair} could not be cast to IATMVolSurface");
                            var correlation = 0.0;
                            if (model.CorrelationMatrix != null)
                            {
                                if (model.CorrelationMatrix.TryGetCorrelation(fxAdjPair, assetId, out var correl))
                                    correlation = correl;
                            }

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
                        else
                        {
                            var asset = new LVSingleAsset
                            (
                                startDate: originDate,
                                expiryDate: lastDate,
                                volSurface: surface,
                                forwardCurve: fwdCurve,
                                nTimeSteps: settings.NumberOfTimesteps,
                                name: assetId,
                                pastFixings: fixings
                            );
                            Engine.AddPathProcess(asset);
                        }
                    }
                    else
                    {
                        if (settings.ReportingCurrency != model.GetPriceCurve(assetId).Currency)
                        {
                            var fxAdjPair = model.GetPriceCurve(assetId).Currency + "/" + settings.ReportingCurrency;
                            if (!(model.FundingModel.VolSurfaces[fxAdjPair] is IATMVolSurface adjSurface))
                                throw new Exception($"Vol surface for fx pair {fxAdjPair} could not be cast to IATMVolSurface");
                            var correlation = 0.0;
                            if (model.CorrelationMatrix != null)
                            {
                                if (model.CorrelationMatrix.TryGetCorrelation(fxAdjPair, assetId, out var correl))
                                    correlation = correl;
                            }

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
                               pastFixings: fixings
                            );
                            Engine.AddPathProcess(asset);
                        }
                    }
                }
            }

            var pairsAdded = new List<string>();
            var fxPairs = portfolio.FxPairs(model);
            foreach (var fxPair in fxPairs)
            {
                var fxPairName = fxPair;
                var pair = fxPairName.FxPairFromString(_currencyProvider, _calendarProvider);
   
                if (pairsAdded.Contains(pair.ToString()))
                    continue;

                if (!(model.FundingModel.VolSurfaces[fxPairName] is IATMVolSurface surface))
                    throw new Exception($"Vol surface for fx pair {fxPairName} could not be cast to IATMVolSurface");

                var fwdCurve = new Func<double, double>(t =>
                {
                    var date = originDate.AddYearFraction(t, DayCountBasis.ACT365F);
                    var spotDate = pair.SpotDate(date);
                    return model.FundingModel.GetFxRate(spotDate, fxPairName);
                });

                pairsAdded.Add(pair.ToString());

                if (settings.LocalVol)
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
                }
                else
                {
                    if (fxPairName.Substring(fxPairName.Length - 3, 3)!= settings.ReportingCurrency)
                    {//needs to be drift-adjusted
                        var fxAdjPair = settings.ReportingCurrency + "/" + fxPairName.Substring(fxPairName.Length - 3, 3);
                        if (!(model.FundingModel.VolSurfaces[fxAdjPair] is IATMVolSurface adjSurface))
                            throw new Exception($"Vol surface for fx pair {fxAdjPair} could not be cast to IATMVolSurface");
                        var correlation = fxPair == fxAdjPair ? -1.0 : 0.0;
                        if(correlation!=-1.0 && model.CorrelationMatrix!=null)
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
                    }
                }
            }

            Engine.IncrementDepth();
            _payoffs = assetInstruments.ToDictionary(x => x.TradeId, y => new AssetPathPayoff(y, _currencyProvider, _calendarProvider));
            foreach (var product in _payoffs)
            {
                Engine.AddPathProcess(product.Value);
            }

            //Need to calculate PFE
            if (settings.PfeExposureDates != null && settings.ReportingCurrency != null)//setup for PFE
            {
                Engine.IncrementDepth();

                switch (settings.PfeRegressorType)
                {
                    case PFERegressorType.MultiLinear:
                        _regressor = new LinearPortfolioValueRegressor(settings.PfeExposureDates,
                            _payoffs.Values.ToArray(), settings);
                        break;
                    case PFERegressorType.MonoLinear:
                        _regressor = new MonoIndexRegressor(settings.PfeExposureDates,
                            _payoffs.Values.ToArray(), settings, true);
                        break;
                }
                Engine.AddPathProcess(_regressor);
            }
            Engine.SetupFeatures();
        }
        public ICube PFE(double confidenceLevel) => PackResults(() => _regressor.PFE(Model, confidenceLevel));
        public ICube EPE() => PackResults(() => _regressor.EPE(Model));
        public ICube ENE() => PackResults(() => _regressor.ENE(Model));

        private ICube PackResults(Func<double[]> method)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "ExposureDate", typeof(DateTime) }
            };
            cube.Initialize(dataTypes);
            Engine.RunProcess();

            var e = method.Invoke();

            for (var i = 0; i < e.Length; i++)
            {
                cube.AddRow(new object[] { Settings.PfeExposureDates[i] }, e[i]);
            }
            return cube;
        }

        public ICube PV(Currency reportingCurrency)
        {
            var ccy = reportingCurrency?.ToString();
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Currency", typeof(string) }
            };
            cube.Initialize(dataTypes);
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
                switch (ins)
                {
                    case IAssetInstrument aIns:
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, aIns.Currency);
                        else
                            ccy = aIns.Currency.ToString();
                        break;
                    case FixedRateLoanDeposit loanDepo:
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, loanDepo.Currency);
                        else
                            ccy = loanDepo.Currency.ToString();
                        break;
                    case CashBalance cash:
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, cash.Currency);
                        else
                            ccy = cash.Currency.ToString();
                        break;
                    default:
                        throw new Exception($"Unabled to handle product of type {ins.GetType()}");
                }
                var row = new Dictionary<string, object>
                {
                    { "TradeId", tradeId },
                    { "Currency", ccy }
                };
                cube.AddRow(row, pv / fxRate);
            }
            return cube;
        }

        public IPvModel Rebuild(IAssetFxModel newVanillaModel, Portfolio portfolio) => new AssetFxMCModel(OriginDate, portfolio, newVanillaModel, Settings, _currencyProvider, _futureSettingsProvider, _calendarProvider);
    }
}
