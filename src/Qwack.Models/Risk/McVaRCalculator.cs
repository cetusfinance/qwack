using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Math;
using Qwack.Models.MCModels;
using Qwack.Models.Models;
using Qwack.Models.Risk.Mutators;
using Qwack.Transport.Results;
using Qwack.Utils.Parallel;

namespace Qwack.Models.Risk
{
    public class McVaRCalculator
    {
        private readonly IAssetFxModel _model;
        private readonly Portfolio _portfolio;
        private readonly ILogger _logger;
        private readonly ICurrencyProvider _currencyProvider;
        private readonly ICalendarProvider _calendarProvider;
        private readonly IFutureSettingsProvider _futureSettingsProvider;
        private readonly Dictionary<DateTime, IAssetFxModel> _bumpedModels = new();
        private readonly Dictionary<string, double> _spotFactors = new();
        

        public McVaRCalculator(IAssetFxModel model, Portfolio portfolio, ILogger logger, ICurrencyProvider currencyProvider, 
            ICalendarProvider calendarProvider, IFutureSettingsProvider futureSettingsProvider)
        {
            _model = model;
            _portfolio = portfolio;
            _logger = logger;
            _currencyProvider = currencyProvider;
            _calendarProvider = calendarProvider;
            _futureSettingsProvider = futureSettingsProvider;
        }

        public void AddSpotFactor(string assetId, double vol)
        {
            _spotFactors[assetId] = vol;
        }

        public string[] GetSpotFactors() => _spotFactors.Keys.OrderBy(x => x).ToArray();

        public void SetCorrelationMatrix(ICorrelationMatrix matrix) => _model.CorrelationMatrix = matrix;

        public void CalculateModels()
        {
            var allAssetIds = _portfolio.AssetIds().Where(x => !(x.Length == 7 && x[3] == '/')).ToArray();
            var simulatedIds = allAssetIds.Intersect(_spotFactors.Keys).ToArray();

            _logger.LogInformation("Simulating {nFac} spot factors", simulatedIds.Length);

            var mcSettings = new McSettings
            {
                McModelType=McModelType.Black,
                Generator=RandomGeneratorType.MersenneTwister,
                NumberOfPaths=1000,
                NumberOfTimesteps=10
            };

            var fp = new FactorReturnPayoff(simulatedIds, new DateTime[] { _model.BuildDate.AddDays(1) });
            
            var mcModel = new AssetFxMCModel(_model.BuildDate, _portfolio, _model, mcSettings, _currencyProvider, _futureSettingsProvider, _calendarProvider); 
        }

        public (double VaR, DateTime ScenarioDate) CalculateVaR(double ci, Currency ccy, string[] excludeTradeIds)
        {
            if (!_resultsCache.Any())
            {
                var pf = _portfolio.Clone();
                pf.Instruments.RemoveAll(i => excludeTradeIds.Contains(i.TradeId));
                return CalculateVaR(ci, ccy, pf);
            }
            else
            {
                var filterDict = excludeTradeIds.Select(x => new KeyValuePair<string, object>("TradeId", (object)x)).ToList();
                var results = _resultsCache.ToDictionary(x => x.Key, x => x.Value.Filter(filterDict, true).SumOfAllRows);
                var sortedResults = results.OrderBy(kv => kv.Value).ToList();
                var ixCi = (int)System.Math.Floor(sortedResults.Count() * (1.0 - ci));
                var ciResult = sortedResults[System.Math.Min(System.Math.Max(ixCi, 0), sortedResults.Count - 1)];
                var basePvForSet = _basePvCube.Filter(filterDict, true).SumOfAllRows;
                return (ciResult.Value - basePvForSet, ciResult.Key);
            }
        }

        public (double VaR, DateTime ScenarioDate) CalculateVaRInc(double ci, Currency ccy, string[] includeTradeIds)
        {
            if (!_resultsCache.Any())
            {
                var pf = _portfolio.Clone();
                pf.Instruments.RemoveAll(i => !includeTradeIds.Contains(i.TradeId));
                return CalculateVaR(ci, ccy, pf);
            }
            else
            {
                var filterDict = includeTradeIds.Select(x => new KeyValuePair<string, object>("TradeId", (object)x)).ToList();
                var results = _resultsCache.ToDictionary(x => x.Key, x => x.Value.Filter(filterDict, false).SumOfAllRows);
                var sortedResults = results.OrderBy(kv => kv.Value).ToList();
                var ixCi = (int)System.Math.Floor(sortedResults.Count() * (1.0 - ci));
                var ciResult = sortedResults[System.Math.Min(System.Math.Max(ixCi, 0), sortedResults.Count - 1)];
                var basePvForSet = _basePvCube.Filter(filterDict, false).SumOfAllRows;
                return (ciResult.Value - basePvForSet, ciResult.Key);
            }
        }

        public double[] CalculateVaRRange(double[] cis)
        {
            var results = _resultsCache.ToDictionary(x => x.Key, x => x.Value.SumOfAllRows);
            var sortedResults = results.OrderBy(kv => kv.Value).ToList();
            var ixCis = cis.Select(ci => (int)System.Math.Floor(sortedResults.Count() * (1.0 - ci))).ToArray();
            var ciResults = ixCis.Select(ixCi => sortedResults[System.Math.Min(System.Math.Max(ixCi, 0), sortedResults.Count - 1)]);
            var basePvForSet = _basePvCube.SumOfAllRows;
            return ciResults.Select(x => x.Value - basePvForSet).ToArray();
        }

        public BetaAnalysisResult ComputeBeta(double referenceNav, Dictionary<DateTime, double> benchmarkPrices, DateTime? earliestDate = null)
        {
            if (!earliestDate.HasValue)
                earliestDate = DateTime.MinValue;

            var basePv = _basePvCube.SumOfAllRows;
            var results = _resultsCache.ToDictionary(x => x.Key, x => x.Value.SumOfAllRows - basePv + referenceNav);
            var intersectingDates = results.Keys.Intersect(benchmarkPrices.Keys).Where(x => x > earliestDate).OrderBy(x => x).ToArray();
            var pfReturns = new List<double>();
            var benchmarkReturns = new List<double>();
            for (var t = 1; t < intersectingDates.Length; t++)
            {
                var y = intersectingDates[t - 1];
                var d = intersectingDates[t];
                pfReturns.Add(System.Math.Log(results[d] / results[y]));
                benchmarkReturns.Add(System.Math.Log(benchmarkPrices[d] / benchmarkPrices[y]));
            }

            var lrResult = LinearRegression.LinearRegressionVector(benchmarkReturns.ToArray(), pfReturns.ToArray());

            return new BetaAnalysisResult
            {
                LrResult = lrResult,
                BenchmarkReturns = benchmarkReturns.ToArray(),
                PortfolioReturns = pfReturns.ToArray(),
                BenchmarkPrices = benchmarkPrices
            };
        }

        public Dictionary<string, double> GetBaseValuations() => _basePvCube.Pivot("TradeId", AggregationAction.Sum).ToDictionary("TradeId").ToDictionary(x => x.Key as string, x => x.Value.Sum(r => r.Value));

        public Dictionary<string, double> GetContributions(DateTime scenarioDate)
        {
            var cube = _resultsCache[scenarioDate];
            var diff = cube.Difference(_basePvCube);

            return diff.Pivot("TradeId", AggregationAction.Sum).ToDictionary("TradeId").ToDictionary(x => x.Key as string, x => x.Value.Sum(r => r.Value));
        }

        public (double VaR, DateTime ScenarioDate) CalculateVaR(double ci, Currency ccy) => CalculateVaR(ci, ccy, _portfolio);

        private readonly ConcurrentDictionary<DateTime, ICube> _resultsCache = new();
        private ICube _basePvCube;
        public (double VaR, DateTime ScenarioDate) CalculateVaR(double ci, Currency ccy, Portfolio pf, bool parallelize = true)
        {
            _basePvCube = pf.PV(_model, ccy);
            var basePv = _basePvCube.SumOfAllRows;
            _resultsCache.Clear();
            var results = new ConcurrentDictionary<DateTime, double>();
            var varFunc = new Action<DateTime, IAssetFxModel>((d, m) =>
            {
                var cube = pf.PV(m, ccy, false);
                var scenarioPv = cube.SumOfAllRows;
                _resultsCache[d] = cube;
                results[d] = scenarioPv - basePv;
            });
            if (parallelize)
            {
                Parallel.ForEach(_bumpedModels, kv =>
                {
                    varFunc(kv.Key, kv.Value);
                });
            }
            else
            {
                foreach (var kv in _bumpedModels)
                {
                    varFunc(kv.Key, kv.Value);
                }
            }

            var sortedResults = results.OrderBy(kv => kv.Value).ToList();
            var ixCi = (int)System.Math.Floor(sortedResults.Count() * (1.0 - ci));
            var ciResult = sortedResults[System.Math.Min(System.Math.Max(ixCi, 0), sortedResults.Count - 1)];
            return (ciResult.Value, ciResult.Key);
        }

        internal class VaRSpotScenarios
        {
            public bool IsRelativeBump { get; set; }
            public string AssetId { get; set; }
            public Dictionary<DateTime, double> Bumps { get; set; }
        }

        internal class VaRCurveScenarios
        {
            public bool IsRelativeBump { get; set; }
            public string AssetId { get; set; }
            public Dictionary<DateTime, double[]> Bumps { get; set; }
        }
    }


}
