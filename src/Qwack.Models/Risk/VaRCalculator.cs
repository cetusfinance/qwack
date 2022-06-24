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
using Qwack.Models.Models;
using Qwack.Models.Risk.Mutators;
using Qwack.Utils.Parallel;

namespace Qwack.Models.Risk
{
    public class VaRCalculator
    {
        private readonly IAssetFxModel _model;
        private readonly Portfolio _portfolio;
        private readonly ILogger _logger;
        private readonly Dictionary<string, VaRSpotScenarios> _spotTypeBumps = new();
        private readonly Dictionary<string, VaRSpotScenarios> _spotFxTypeBumps = new();
        private readonly Dictionary<string, VaRCurveScenarios> _curveTypeBumps = new();
        private readonly Dictionary<string, VaRCurveScenarios> _surfaceTypeBumps = new();
        private readonly Dictionary<DateTime, IAssetFxModel> _bumpedModels = new();

        public VaRCalculator(IAssetFxModel model, Portfolio portfolio, ILogger logger)
        {
            _model = model;
            _portfolio = portfolio;
            _logger = logger;
        }

        public void AddSpotRelativeScenarioBumps(string assetId, Dictionary<DateTime, double> bumps)
        {
            _logger?.LogInformation($"Adding relative/spot bumps for {assetId}");
            _spotTypeBumps[assetId] = new VaRSpotScenarios
            {
                IsRelativeBump = true,
                AssetId = assetId,
                Bumps = bumps,
            };
        }

        public void AddSpotAbsoluteScenarioBumps(string assetId, Dictionary<DateTime, double> bumps)
        {
            _logger?.LogInformation($"Adding absolute/spot bumps for {assetId}");
            _spotTypeBumps[assetId] = new VaRSpotScenarios
            {
                IsRelativeBump = false,
                AssetId = assetId,
                Bumps = bumps,
            };
        }

        public void AddSpotFxRelativeScenarioBumps(string ccy, Dictionary<DateTime, double> bumps)
        {
            _logger?.LogInformation($"Adding relative/spot bumps for fx ccy {ccy}");
            _spotFxTypeBumps[ccy] = new VaRSpotScenarios
            {
                IsRelativeBump = true,
                AssetId = ccy,
                Bumps = bumps,
            };
        }

        public void AddSpotFxAbsoluteScenarioBumps(string ccy, Dictionary<DateTime, double> bumps)
        {
            _logger?.LogInformation($"Adding absolute/spot bumps for fx ccy {ccy}");
            _spotFxTypeBumps[ccy] = new VaRSpotScenarios
            {
                IsRelativeBump = false,
                AssetId = ccy,
                Bumps = bumps,
            };
        }

        public void AddCurveRelativeScenarioBumps(string assetId, Dictionary<DateTime, double[]> bumps)
        {
            _logger?.LogInformation($"Adding relative/curve bumps for {assetId}");
            _curveTypeBumps[assetId] = new VaRCurveScenarios
            {
                IsRelativeBump = true,
                AssetId = assetId,
                Bumps = bumps,
            };
        }

        public void AddCurveAbsoluteScenarioBumps(string assetId, Dictionary<DateTime, double[]> bumps)
        {
            _logger?.LogInformation($"Adding absolute/curve bumps for {assetId}");
            _curveTypeBumps[assetId] = new VaRCurveScenarios
            {
                IsRelativeBump = false,
                AssetId = assetId,
                Bumps = bumps,
            };
        }

        public void AddSurfaceAtmRelativeScenarioBumps(string assetId, Dictionary<DateTime, double[]> bumps)
        {
            _logger?.LogInformation($"Adding relative/surface atm bumps for {assetId}");
            _surfaceTypeBumps[assetId] = new VaRCurveScenarios
            {
                IsRelativeBump = true,
                AssetId = assetId,
                Bumps = bumps,
            };
        }

        public void AddSurfaceAtmAbsoluteScenarioBumps(string assetId, Dictionary<DateTime, double[]> bumps)
        {
            _logger?.LogInformation($"Adding absolute/surface atm bumps for {assetId}");
            _surfaceTypeBumps[assetId] = new VaRCurveScenarios
            {
                IsRelativeBump = false,
                AssetId = assetId,
                Bumps = bumps,
            };
        }

        public void CalculateModels()
        {
            var allAssetIds = _portfolio.AssetIds().Where(x => !(x.Length == 7 && x[3] == '/')).ToArray();
            var allDatesSet = new HashSet<DateTime>();

            if (_spotTypeBumps.Any())
            {
                foreach(var d in _spotTypeBumps.First().Value.Bumps.Keys)
                {
                    allDatesSet.Add(d);
                }
            }

            if (_curveTypeBumps.Any())
            {
                foreach (var d in _curveTypeBumps.First().Value.Bumps.Keys)
                {
                    allDatesSet.Add(d);
                }
            }

            if (_spotFxTypeBumps.Any())
            {
                foreach (var d in _spotFxTypeBumps.First().Value.Bumps.Keys)
                {
                    allDatesSet.Add(d);
                }
            }

            var allDates = allDatesSet.OrderBy(d => d).ToList();

            _logger?.LogInformation($"Total of {allDates.Count} dates");

            ConcurrentDictionary<DateTime, IAssetFxModel> bumpedModels = new();

            ParallelUtils.Instance.Foreach(allDates, d =>
            {
                //if (d != new DateTime(2021, 08, 02))
                //    return;

                _logger?.LogDebug($"Computing scenarios for {d}");
                var m = _model.Clone();

                foreach (var assetId in allAssetIds)
                {
                    if (_spotTypeBumps.TryGetValue(assetId, out var spotBumpRecord))
                    {
                        if (spotBumpRecord.IsRelativeBump)
                            m = RelativeShiftMutator.AssetCurveShift(assetId, spotBumpRecord.Bumps[d], m);
                        else
                            m = FlatShiftMutator.AssetCurveShift(assetId, spotBumpRecord.Bumps[d], m);
                    }
                    else if (_curveTypeBumps.TryGetValue(assetId, out var curveBumpRecord))
                    {
                        if (curveBumpRecord.IsRelativeBump)
                            m = CurveShiftMutator.AssetCurveShiftRelative(assetId, curveBumpRecord.Bumps[d], m);
                        else
                            m = CurveShiftMutator.AssetCurveShiftAbsolute(assetId, curveBumpRecord.Bumps[d], m);
                    }
                    else
                    {
                        _logger?.LogWarning($"No shift data available for {assetId} / {d}");
                    }

                    if (_surfaceTypeBumps.TryGetValue(assetId, out var surfaceBumpRecord))
                    {
                        if (surfaceBumpRecord.IsRelativeBump)
                            m = SurfaceShiftMutator.AssetSurfaceShiftRelative(assetId, surfaceBumpRecord.Bumps[d], m);
                        else
                            m = SurfaceShiftMutator.AssetSurfaceShiftAbsolute(assetId, surfaceBumpRecord.Bumps[d], m);
                    }
                }

                foreach (var ccy in m.FundingModel.FxMatrix.SpotRates.Keys)
                {
                    if (_spotFxTypeBumps.TryGetValue(ccy, out var spotBumpRecord))
                    {
                        if (spotBumpRecord.IsRelativeBump)
                            m = RelativeShiftMutator.SpotFxShift(ccy, spotBumpRecord.Bumps[d], m);
                        //else
                        //    m = FlatShiftMutator.AssetCurveShift(assetId, spotBumpRecord.Bumps[d], m);
                    }
                }
                bumpedModels[d] = m;
            }).Wait();

            foreach (var kv in bumpedModels)
                _bumpedModels[kv.Key] = kv.Value;
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
