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

namespace Qwack.Models.MCModels
{
    public class AssetFxBlackVolMC : IMcModel
    {
        private static string GetSobolFilename() => Path.Combine(GetRunningDirectory(), "SobolDirectionNumbers.txt");

        private static string GetRunningDirectory()
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            return dirPath;
        }

        public PathEngine Engine { get; }
        public Portfolio Portfolio { get; }
        public IAssetFxModel Model { get; }
        public McSettings Settings { get; }

        private Dictionary<string, AssetPathPayoff> _payoffs;
        private LinearPortfolioValueRegressor _regressor;
        private ICurrencyProvider _currencyProvider;

        public AssetFxBlackVolMC(DateTime originDate, Portfolio portfolio, IAssetFxModel model, McSettings settings, ICurrencyProvider currencyProvider)
        {
            _currencyProvider = currencyProvider;
            Engine = new PathEngine(settings.NumberOfPaths);
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

                var fwdCurve = new Func<double, double>(t => 
                {
                    return model
                    .GetPriceCurve(assetId)
                    .GetPriceForDate(originDate.AddYearFraction(t, DayCountBasis.ACT365F));
                });

                var fixingDict = fixingsNeeded.ContainsKey(assetId) ? model.GetFixingDictionary(assetId) : null;
                var fixings = fixingDict!=null ?
                    fixingsNeeded[assetId].ToDictionary(x => x, x => fixingDict[x])
                    : new Dictionary<DateTime, double>();

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

            var fxPairs = portfolio.FxPairs(model);
            foreach (var fxPair in fxPairs)
            {
                var pair = fxPair.FxPairFromString(_currencyProvider);
                string fxPairName;
                if (!model.FundingModel.VolSurfaces.ContainsKey(fxPair))
                {
                    var flippedPair = $"{pair.Foreign.Ccy}/{pair.Domestic.Ccy}";
                    if (!model.FundingModel.VolSurfaces.ContainsKey(fxPair))
                    {
                        throw new Exception($"Could not find Fx vol surface for {fxPair} or {flippedPair}");
                    }

                    pair = flippedPair.FxPairFromString(_currencyProvider);
                    fxPairName = flippedPair;
                }
                else
                {
                    fxPairName = fxPair;
                }

                if (!(model.FundingModel.VolSurfaces[fxPairName] is IATMVolSurface surface))
                    throw new Exception($"Vol surface for fx pair {fxPairName} could not be cast to IATMVolSurface");

                var fwdCurve = new Func<double, double>(t =>
                {
                    var date = originDate.AddYearFraction(t, DayCountBasis.ACT365F);
                    return model.FundingModel.GetFxRate(date, fxPairName);
                });
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

            _payoffs = assetInstruments.ToDictionary(x => x.TradeId, y => new AssetPathPayoff(y));
            foreach (var product in _payoffs)
            {
                Engine.AddPathProcess(product.Value);
            }

            if (settings.PfeExposureDates != null)//setup for PFE
            {
                _regressor = new LinearPortfolioValueRegressor(settings.PfeExposureDates,
                    _payoffs.Values.ToArray(), settings.NumberOfPaths);

                Engine.AddPathProcess(_regressor);
            }

            Engine.SetupFeatures();
        }

        public ICube PFE(double confidenceLevel)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "ExposureDate", typeof(DateTime) }
            };
            cube.Initialize(dataTypes);

            Engine.RunProcess();

            var pfe = _regressor.PFE(Model, confidenceLevel);

            for(var i=0;i<pfe.Length;i++)
            {
                cube.AddRow(new object[] { Settings.PfeExposureDates[i] }, pfe[i]);
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
                if(insQuery.Count()!=1)
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
                    case FxForward fxFwd:
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, fxFwd.ForeignCCY);
                        else
                            ccy = fxFwd.ForeignCCY.ToString();
                        break;
                    case FixedRateLoanDeposit loanDepo:
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, loanDepo.Ccy);
                        else
                            ccy = loanDepo.Ccy.ToString();
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

    }
}
