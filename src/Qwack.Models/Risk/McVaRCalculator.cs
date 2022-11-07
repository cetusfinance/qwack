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
using Qwack.Options.VolSurfaces;
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
        private readonly McModelType _modelType;
        private readonly List<IAssetFxModel> _bumpedModels = new();
        private readonly Dictionary<string, double> _spotFactors = new();
        private readonly Dictionary<string, double[]> _returns = new();
        private int _ciIx = 0;

        public int CIIX => _ciIx;

        public McVaRCalculator(IAssetFxModel model, Portfolio portfolio, ILogger logger, ICurrencyProvider currencyProvider, 
            ICalendarProvider calendarProvider, IFutureSettingsProvider futureSettingsProvider, McModelType modelType)
        {
            _model = model.Clone();
            _portfolio = portfolio;
            _logger = logger;
            _currencyProvider = currencyProvider;
            _calendarProvider = calendarProvider;
            _futureSettingsProvider = futureSettingsProvider;
            _modelType = modelType;
        }

        public void AddSpotFactor(string assetId, double vol)
        {
            _spotFactors[assetId] = vol;
        }

        public void AddReturns(string assetId, double[] returns)
        {
            _returns[assetId] = returns;
        }

        public string[] GetSpotFactors() => _spotFactors.Keys.OrderBy(x => x).ToArray();

        public void SetCorrelationMatrix(ICorrelationMatrix matrix) => _model.CorrelationMatrix = matrix;

        public void CalculateModels()
        {
            //var allAssetIds = _portfolio.AssetIds().Concat(_portfolio.Instruments.Select(x => x.Currency.Ccy).Where(x => x != "USD").Select(x =>$"USD/{x}")).ToArray();
            var allAssetIds = _model.CurveNames.Concat(_portfolio.Instruments.Select(x => x.Currency.Ccy).Where(x => x != "USD").Select(x => $"USD/{x}")).ToArray();
            var simulatedIds = allAssetIds.Intersect(_spotFactors.Keys).ToArray();

            foreach(var simulatedId in simulatedIds)
            {
                var surf = new ConstantVolSurface(_model.BuildDate, _spotFactors[simulatedId]) { AssetId = simulatedId };
                if (_returns.TryGetValue(simulatedId, out var returns))
                    surf.Returns = returns;
                _model.AddVolSurface(simulatedId, surf);
                if(simulatedId.Length==6 && simulatedId[3] == '/')
                {
                    _model.FundingModel.VolSurfaces.Add(simulatedId, surf);
                }   
            }

            _logger.LogInformation("Simulating {nFac} spot factors", simulatedIds.Length);

            var mcSettings = new McSettings
            {
                McModelType= _modelType,
                Generator=RandomGeneratorType.MersenneTwister,
                NumberOfPaths=2048,
                NumberOfTimesteps=2,
                ReportingCurrency=_currencyProvider.GetCurrencySafe("USD")
            };

            var vd = _model.BuildDate.AddDays(1);
            var fp = new FactorReturnPayoff(simulatedIds, new DateTime[] { _model.BuildDate, vd });
            
            var mcModel = new AssetFxMCModel(_model.BuildDate, fp, _model, mcSettings, _currencyProvider, _futureSettingsProvider, _calendarProvider);
            mcModel.Engine.RunProcess();

            var dix = fp.DateIndices[vd];
            for (var p = 0; p < mcSettings.NumberOfPaths; p++)
            {
                var pModel = _model.Clone();
                for (var a = 0; a < simulatedIds.Length; a++)
                {
                    var price0 = fp.ResultsByPath[a][p][0];
                    var price1 = fp.ResultsByPath[a][p][dix];
                    var bump = price1 / price0 - 1.0;

                    if (IsFx(simulatedIds[a]))
                        pModel = RelativeShiftMutator.FxSpotShift(_currencyProvider.GetCurrencySafe(simulatedIds[a].Split('/').Last()), bump, pModel);
                    else
                        pModel = RelativeShiftMutator.AssetCurveShift(simulatedIds[a], bump, pModel);
                }

                _bumpedModels.Add(pModel);
            }
        }

        private static bool IsFx(string assetId) => assetId.Length == 7 && assetId[3] == '/';

        public double CalculateVaR(double ci, Currency ccy, string[] excludeTradeIds)
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
                _ciIx = ixCi;
                var ciResult = sortedResults[System.Math.Min(System.Math.Max(ixCi, 0), sortedResults.Count - 1)];
                var basePvForSet = _basePvCube.Filter(filterDict, true).SumOfAllRows;
                return ciResult.Value - basePvForSet;
            }
        }

        public double CalculateVaRInc(double ci, Currency ccy, string[] includeTradeIds)
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
                return ciResult.Value - basePvForSet;
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

        public Dictionary<string, double> GetBaseValuations() => _basePvCube.Pivot("TradeId", AggregationAction.Sum).ToDictionary("TradeId").ToDictionary(x => x.Key as string, x => x.Value.Sum(r => r.Value));

        public Dictionary<string, double> GetContributions(int ix)
        {
            var cube = _resultsCache[ix];
            var diff = cube.Difference(_basePvCube);

            return diff.Pivot("TradeId", AggregationAction.Sum).ToDictionary("TradeId").ToDictionary(x => x.Key as string, x => x.Value.Sum(r => r.Value));
        }

        public decimal ComputeStress(string insId, decimal shockSize)
        {
            var basePv = _basePvCube.SumOfAllRows;
            var baseLevel = _model.GetPriceCurve(insId).GetPriceForFixingDate(_model.BuildDate);
            var shockedLevel = baseLevel * Convert.ToDouble(1 + shockSize);

            var allScenarios = _resultsCache
                .Select(x => (x.Value.SumOfAllRows, _bumpedModels[x.Key].GetPriceCurve(insId).GetPriceForFixingDate(_model.BuildDate)))
                .OrderBy(x => x.Item2)
                .ToList();

            var lr = LinearRegression.LinearRegressionNoVector(allScenarios.Select(x => x.Item2).ToArray(), allScenarios.Select(x => x.SumOfAllRows).ToArray(), false);

            var interp = lr.Alpha + lr.Beta * shockedLevel;

            return Convert.ToDecimal(interp - basePv);
        }

        public StressTestResult ComputeStressObject(string insId, decimal shockSize)
        {
            var basePv = _basePvCube.SumOfAllRows;
            var baseLevel = _model.GetPriceCurve(insId).GetPriceForFixingDate(_model.BuildDate);
            var shockedLevel = baseLevel * Convert.ToDouble(1 + shockSize);

            var allScenarios = _resultsCache
                .Select(x => (x.Value.SumOfAllRows, _bumpedModels[x.Key].GetPriceCurve(insId).GetPriceForFixingDate(_model.BuildDate)))
                .OrderBy(x => x.Item2)
                .ToList();

            var lr = LinearRegression.LinearRegressionNoVector(allScenarios.Select(x => x.Item2).ToArray(), allScenarios.Select(x => x.SumOfAllRows).ToArray(), false);

            var interp = lr.Alpha + lr.Beta * shockedLevel;

            var scenarioPoints = new Dictionary<double, double>();
            foreach (var kv in allScenarios)
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

        public double  CalculateVaR(double ci, Currency ccy) => CalculateVaR(ci, ccy, _portfolio);

        private readonly ConcurrentDictionary<int, ICube> _resultsCache = new();
        private ICube _basePvCube;
        public double CalculateVaR(double ci, Currency ccy, Portfolio pf, bool parallelize = true)
        {
            _basePvCube = pf.PV(_model, ccy);
            var basePv = _basePvCube.SumOfAllRows;
            _resultsCache.Clear();
            var results = new ConcurrentDictionary<int, double>();
            var varFunc = new Action<int, IAssetFxModel>((d, m) =>
            {
                var cube = pf.PV(m, ccy, false);
                var scenarioPv = cube.SumOfAllRows;
                _resultsCache[d] = cube;
                results[d] = scenarioPv - basePv;
            });
            if (parallelize)
            {
                Parallel.For(0, _bumpedModels.Count, ix => 
                {
                    varFunc(ix, _bumpedModels[ix]);
                });
            }
            else
            {
                for (var ix=0; ix<_bumpedModels.Count; ix++)
                {
                    varFunc(ix, _bumpedModels[ix]);
                }
            }

            var sortedResults = results.OrderBy(kv => kv.Value).ToList();
            var ixCi = (int)System.Math.Floor(sortedResults.Count() * (1.0 - ci));
            var ciResult = sortedResults[System.Math.Min(System.Math.Max(ixCi, 0), sortedResults.Count - 1)];
            return ciResult.Value;
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
