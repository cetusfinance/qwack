using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Models.Models;
using Qwack.Models.Risk.Mutators;

namespace Qwack.Models.Risk
{
    public class VaRCalculator
    {
        private readonly IAssetFxModel _model;
        private readonly Portfolio _portfolio;
        private readonly ILogger _logger;
        private readonly Dictionary<string, VaRSpotScenarios> _spotTypeBumps = new();
        private readonly Dictionary<string, VaRCurveScenarios> _curveTypeBumps = new();
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

        public void CalculateModels()
        {
            var allAssetIds = _portfolio.AssetIds();
            var allDates = _spotTypeBumps.First().Value.Bumps.Keys.ToList();
            foreach(var kv in _spotTypeBumps)
            {
                allDates = allDates.Intersect(kv.Value.Bumps.Keys).ToList();
            }
            foreach (var kv in _curveTypeBumps)
            {
                allDates = allDates.Intersect(kv.Value.Bumps.Keys).ToList();
            }

            _logger?.LogInformation($"Total of {allDates.Count} dates");



            foreach (var d in allDates)
            {
                _logger?.LogInformation($"Computing scenarios for {d}");
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
                }

                _bumpedModels[d] = m;
            }
        }

        public (double VaR, DateTime ScenarioDate) CalculateVaR(double ci, Currency ccy)
        {
            var basePv = _portfolio.PV(_model, ccy).SumOfAllRows;

            var results = new Dictionary<DateTime, double>();
            foreach (var kv in _bumpedModels)
            {
                var scenarioPv = _portfolio.PV(kv.Value, ccy).SumOfAllRows;
                results[kv.Key] = scenarioPv - basePv;
            }

            var sortedResults = results.OrderBy(kv=>kv.Value).ToList();
            var ixCi = (int)System.Math.Floor(sortedResults.Count() * (1.0 - ci));
            var ciResult = sortedResults[ixCi];
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
