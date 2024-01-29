using System;
using System.Buffers.Text;
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
using Qwack.Models.Models;
using Qwack.Models.Risk.Mutators;
using Qwack.Transport.Results;
using Qwack.Utils.Parallel;

namespace Qwack.Models.Risk.VaR
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
        private VaREngine _varEngine;

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
            var allAssetIds = _model.CurveNames.Where(x => !(x.Length == 7 && x[3] == '/')).ToArray(); // _portfolio.AssetIds().Where(x => !(x.Length == 7 && x[3] == '/')).ToArray();
            var allDatesSet = new HashSet<DateTime>();

            if (_spotTypeBumps.Any())
            {
                foreach (var d in _spotTypeBumps.First().Value.Bumps.Keys)
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

            _varEngine = new VaREngine(_logger, _model, _portfolio, _bumpedModels.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));
        }
        public (double VaR, string ScenarioId, double cVaR) CalculateVaR(double ci, Currency ccy, string[] excludeTradeIds)
            => _varEngine.CalculateVaR(ci, ccy, excludeTradeIds);

        public (double VaR, string ScenarioId, double cVaR) CalculateVaRInc(double ci, Currency ccy, string[] includeTradeIds)
            => _varEngine.CalculateVaRInc(ci, ccy, includeTradeIds);

        public double[] CalculateVaRRange(double[] cis) => _varEngine.CalculateVaRRange(cis);

        public BetaAnalysisResult ComputeBeta(double referenceNav, Dictionary<DateTime, double> benchmarkPrices, DateTime? earliestDate = null, bool computeTradeLevel = false)
        {
            if (!earliestDate.HasValue)
                earliestDate = DateTime.MinValue;

            var basePv = _varEngine.BasePvCube.SumOfAllRows;
            var results = _varEngine.ResultsCache.ToDictionary(x => DateTime.Parse(x.Key), x => x.Value.SumOfAllRows - basePv + referenceNav);
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

            var lrResult = benchmarkReturns.ToArray().LinearRegressionVector(pfReturns.ToArray());
            Dictionary<string, LinearRegressionResult> tradeBreakdown = null;

            if (computeTradeLevel)
            {
                var tidIx = _varEngine.ResultsCache.First().Value.GetColumnIndex("TradeId");
                if (tidIx >= 0)
                {
                    tradeBreakdown = new Dictionary<string, LinearRegressionResult>();
                    var tradeIds = _varEngine.ResultsCache.Values.SelectMany(x => x.KeysForField<string>("TradeId")).ToList().Distinct();

                    foreach (var tradeId in tradeIds)
                    {
                        var filterDict = new Dictionary<string, object> { { "TradeId", tradeId } };
                        basePv = _varEngine.BasePvCube.Filter(filterDict).SumOfAllRows;
                        var resultsForTrade = _varEngine.ResultsCache.ToDictionary(x => DateTime.Parse(x.Key), x => x.Value.SumOfAllRows - basePv + referenceNav);

                        var pfReturnsForTrade = new List<double>();
                        var benchmarkReturnsForTrade = new List<double>();
                        for (var t = 1; t < intersectingDates.Length; t++)
                        {
                            var y = intersectingDates[t - 1];
                            var d = intersectingDates[t];
                            var resultY = _varEngine.ResultsCache[intersectingDates[t - 1].ToString()].Filter(filterDict).SumOfAllRows - basePv + referenceNav;
                            var resultD = _varEngine.ResultsCache[intersectingDates[t].ToString()].Filter(filterDict).SumOfAllRows - basePv + referenceNav;
                            var returnForTrade = System.Math.Log(resultD / resultY);
                            if (!double.IsNaN(returnForTrade))
                            {
                                benchmarkReturnsForTrade.Add(benchmarkReturns[t - 1]);
                                pfReturnsForTrade.Add(returnForTrade);
                            }

                        }
                        var lrResultForTrade = benchmarkReturnsForTrade.ToArray().LinearRegressionVector(pfReturnsForTrade.ToArray());
                        tradeBreakdown[tradeId] = lrResultForTrade;
                    }
                }
            }

            return new BetaAnalysisResult
            {
                LrResult = lrResult,
                TradeBreakdown = tradeBreakdown,
                BenchmarkReturns = benchmarkReturns.ToArray(),
                PortfolioReturns = pfReturns.ToArray(),
                BenchmarkPrices = benchmarkPrices
            };
        }

        public Dictionary<string, double> GetBaseValuations() => _varEngine.GetBaseValuations();

        public Dictionary<string, double> GetContributions(DateTime scenarioDate) => _varEngine.GetContributions(scenarioDate.ToString());

        public decimal ComputeStress(string insId, decimal shockSize, int? nNearestSamples = null)
            => _varEngine.ComputeStress(insId, shockSize, nNearestSamples);

        public StressTestResult ComputeStressObject(string insId, decimal shockSize, int? nNearestSamples = null)
            => _varEngine.ComputeStressObject(insId, shockSize, nNearestSamples);

        public (double VaR, string ScenarioId, double cVaR) CalculateVaR(double ci, Currency ccy) => CalculateVaR(ci, ccy, _portfolio);

        public (double VaR, string ScenarioId, double cVaR) CalculateVaR(double ci, Currency ccy, Portfolio pf, bool parallelize = true)
            => _varEngine.CalculateVaR(ci, ccy, pf, parallelize);

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
