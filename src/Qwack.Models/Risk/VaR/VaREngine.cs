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
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Transport.Results;

namespace Qwack.Models.Risk
{
    public class VaREngine
    {
        private readonly ILogger _logger;
        private readonly IPvModel _model;
        private readonly Portfolio _portfolio;
        private readonly Dictionary<string, IPvModel> _bumpedModels = new();
        private readonly ConcurrentDictionary<string, ICube> _resultsCache = new();
        private ICube _basePvCube;

        public Dictionary<string, IPvModel> BumpedModels => _bumpedModels;

        public VaREngine(ILogger logger, IAssetFxModel baseModel, Portfolio portfolio, Dictionary<string, IAssetFxModel> bumpedModels)
        {
            _logger = logger;
            _model = baseModel;
            _portfolio = portfolio;
            _bumpedModels = bumpedModels.ToDictionary(x => x.Key, x => (IPvModel)x.Value);
        }

        public VaREngine(ILogger logger, IPvModel baseModel, Portfolio portfolio, Dictionary<string, IPvModel> bumpedModels)
        {
            _logger = logger;
            _model = baseModel;
            _portfolio = portfolio;
            _bumpedModels = bumpedModels;
        }

        public Dictionary<string, ICube> ResultsCache => _resultsCache.ToDictionary(x => x.Key, x => x.Value);
        public ICube BasePvCube {get { return _basePvCube; } set { _basePvCube = value; } }

        public void SeedResults(ICube basePvCube, Dictionary<string, ICube> bumpedPvCubes)
        {
            _basePvCube = basePvCube;
            foreach(var kv in bumpedPvCubes)
            {
                _resultsCache[kv.Key] = kv.Value;
            }
        }

        public (double VaR, string ScenarioId, double cVaR) CalculateVaR(double ci, Currency ccy, string[] excludeTradeIds)
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
                var ciResult = sortedResults[ixCi];
                var cVaR = sortedResults.Take(System.Math.Max(ixCi - 1, 0)).Average(x => x.Value);
                var basePvForSet = _basePvCube.Filter(filterDict, true).SumOfAllRows;
                return (ciResult.Value - basePvForSet, ciResult.Key, cVaR - basePvForSet);
            }
        }

        public (double VaR, string ScenarioId, double cVaR) CalculateVaRInc(double ci, Currency ccy, string[] includeTradeIds)
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
                ixCi = System.Math.Min(System.Math.Max(ixCi, 0), sortedResults.Count - 1);
                var ciResult = sortedResults[ixCi];
                var cVaR = sortedResults.Take(System.Math.Max(ixCi - 1, 0)).Average(x => x.Value);
                var basePvForSet = _basePvCube.Filter(filterDict, false).SumOfAllRows;
                return (ciResult.Value - basePvForSet, ciResult.Key, cVaR - basePvForSet);
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

        public decimal ComputeStress(string insId, decimal shockSize, int? nNearestSamples = null)
        {
            var basePv = _basePvCube.SumOfAllRows;
            var baseLevel = _model.VanillaModel.GetPriceCurve(insId).GetPriceForFixingDate(_model.VanillaModel.BuildDate);
            var shockedLevel = baseLevel * Convert.ToDouble(1 + shockSize);

            var allScenarios = _resultsCache
                .Select(x => (x.Value.SumOfAllRows, _bumpedModels[x.Key].VanillaModel.GetPriceCurve(insId).GetPriceForFixingDate(_model.VanillaModel.BuildDate)))
                .OrderBy(x=>x.Item2)
                .ToList();

            LinearRegressionResult lr;

            if (nNearestSamples.HasValue)
            {
                var shockDbl = Convert.ToDouble(shockSize);
                var subset = allScenarios
                    .OrderBy(x => System.Math.Abs(x.Item2 - shockDbl))
                    .Take(nNearestSamples.Value)
                    .OrderBy(x => x.Item2)
                    .ToArray();
                lr = LinearRegression.LinearRegressionNoVector(subset.Select(x => x.Item2).ToArray(), subset.Select(x => x.SumOfAllRows).ToArray(), false);
            }
            else
                lr = LinearRegression.LinearRegressionNoVector(allScenarios.Select(x => x.Item2).ToArray(), allScenarios.Select(x => x.SumOfAllRows).ToArray(), false);

            var interp = lr.Alpha + lr.Beta * shockedLevel;

            return Convert.ToDecimal(interp - basePv);
        }

        public StressTestResult ComputeStressObject(string insId, decimal shockSize, int? nNearestSamples = null)
        {
            var basePv = _basePvCube.SumOfAllRows;
            var baseLevel = _model.VanillaModel.GetPriceCurve(insId).GetPriceForFixingDate(_model.VanillaModel.BuildDate);
            var shockedLevel = baseLevel * Convert.ToDouble(1 + shockSize);

            var allScenarios = _resultsCache
                .Select(x => (x.Value.SumOfAllRows, _bumpedModels[x.Key].VanillaModel.GetPriceCurve(insId).GetPriceForFixingDate(_model.VanillaModel.BuildDate)))
                .OrderBy(x => x.Item2)
                .ToList();

            LinearRegressionResult lr;

            if (nNearestSamples.HasValue)
            {
                var shockDbl = Convert.ToDouble(shockSize);
                var subset = allScenarios
                    .OrderBy(x => System.Math.Abs(x.Item2 - shockDbl))
                    .Take(nNearestSamples.Value)
                    .OrderBy(x => x.Item2)
                    .ToArray();
                lr = LinearRegression.LinearRegressionNoVector(subset.Select(x => x.Item2).ToArray(), subset.Select(x => x.SumOfAllRows).ToArray(), false);
            }
            else
                lr = LinearRegression.LinearRegressionNoVector(allScenarios.Select(x => x.Item2).ToArray(), allScenarios.Select(x => x.SumOfAllRows).ToArray(), false);

            var interp = lr.Alpha + lr.Beta * shockedLevel;

            var scenarioPoints = new Dictionary<double, double>();
            foreach(var kv in allScenarios)
            {
                scenarioPoints[kv.Item2] = kv.SumOfAllRows - basePv;
            }

            return new StressTestResult
            {
                Id = insId,
                StressSize = Convert.ToDouble(shockSize),
                LR = lr,
                ScenarioPoints = scenarioPoints,
                StressPvChange = Convert.ToDecimal(interp - basePv)
            };
        }

        public Dictionary<string, double> GetBaseValuations() => _basePvCube.Pivot("TradeId", AggregationAction.Sum).ToDictionary("TradeId").ToDictionary(x => x.Key as string, x => x.Value.Sum(r => r.Value));

        public Dictionary<string, double> GetContributions(string scenarioId)
        {
            var cube = _resultsCache[scenarioId];
            var diff = cube.Difference(_basePvCube);

            return diff.Pivot("TradeId", AggregationAction.Sum).ToDictionary("TradeId").ToDictionary(x => x.Key as string, x => x.Value.Sum(r => r.Value));
        }

        public (double VaR, string ScenarioId, double cVaR) CalculateVaR(double ci, Currency ccy) => CalculateVaR(ci, ccy, _portfolio);
        
        public (double VaR, string ScenarioId, double cVaR) CalculateVaR(double ci, Currency ccy, Portfolio pf, bool parallelize = true)
        {
            var m00 = _model.Rebuild(_model.VanillaModel, pf);
            _basePvCube = m00.PV(ccy);
            var basePv = _basePvCube.SumOfAllRows;
            _resultsCache.Clear();
            var results = new ConcurrentDictionary<string, double>();
            var varFunc = new Action<string, IPvModel>((d, m) =>
            {
                var m0 = _model.Rebuild(m.VanillaModel, pf);
                var cube = m0.PV(ccy);
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

            if (results.IsEmpty)
            {
                _logger.LogWarning("Zero results from VaR calculation");
                return (0, "ERROR", 0);
            }

            var sortedResults = results.OrderBy(kv => kv.Value).ToList();
            var ixCi = (int)System.Math.Floor(sortedResults.Count() * (1.0 - ci));
            ixCi = System.Math.Min(System.Math.Max(ixCi, 0), sortedResults.Count - 1);
            
            var ciResult = sortedResults[ixCi];
            var cVaR = sortedResults.Take(System.Math.Max(ixCi - 1, 0)).Average(x => x.Value);
            return (ciResult.Value, ciResult.Key, cVaR);
        }

        public (double VaR, string ScenarioId, double cVaR) CalculateVaRFromResults(double ci, Currency ccy, Portfolio pf, bool parallelize = true)
        {
            var basePv = _basePvCube.SumOfAllRows;
            var results = _resultsCache.ToDictionary(x => x.Key, x => x.Value.SumOfAllRows - basePv);
        
            if (results.Count == 0)
            {
                _logger.LogWarning("Zero results from VaR calculation");
                return (0, "ERROR", 0);
            }

            var sortedResults = results.OrderBy(kv => kv.Value).ToList();
            var ixCi = (int)System.Math.Floor(sortedResults.Count() * (1.0 - ci));
            ixCi = System.Math.Min(System.Math.Max(ixCi, 0), sortedResults.Count - 1);

            var ciResult = sortedResults[ixCi];
            var cVaRPortion = sortedResults.Take(System.Math.Max(ixCi - 1, 0)).ToList();
            var cVaR = cVaRPortion.Count == 0 ? ciResult.Value : cVaRPortion.Average(x => x.Value);
            return (ciResult.Value, ciResult.Key, cVaR);
        }
    }
}
