using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Models.MCModels;
using Qwack.Models.Risk.Mutators;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.Results;

namespace Qwack.Models.Risk.VaR
{
    public class McVaRCalculator
    {
        private readonly IPvModel _model;
        private readonly Portfolio _portfolio;
        private ICube _basePvCube;
        private readonly ILogger _logger;
        private readonly ICurrencyProvider _currencyProvider;
        private readonly ICalendarProvider _calendarProvider;
        private readonly IFutureSettingsProvider _futureSettingsProvider;
        private readonly McModelType _modelType;
        private readonly List<IPvModel> _bumpedModels = new();
        private readonly Dictionary<string, double> _spotFactors = new();
        private readonly Dictionary<string, double[]> _returns = new();
        private int _ciIx = 0;
        private VaREngine _varEngine;

        public int CIIX => _ciIx;

        public McVaRCalculator(IPvModel model, Portfolio portfolio, ILogger logger, ICurrencyProvider currencyProvider,
            ICalendarProvider calendarProvider, IFutureSettingsProvider futureSettingsProvider, McModelType modelType)
        {
            _model = model.Rebuild(model.VanillaModel, portfolio);
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

        public void SetCorrelationMatrix(ICorrelationMatrix matrix) => _model.VanillaModel.CorrelationMatrix = matrix;

        public void CalculateModels()
        {
            //var allAssetIds = _portfolio.AssetIds().Concat(_portfolio.Instruments.Select(x => x.Currency.Ccy).Where(x => x != "USD").Select(x =>$"USD/{x}")).ToArray();
            var allAssetIds = _model.VanillaModel.CurveNames.Concat(_portfolio.Instruments.Select(x => x.Currency.Ccy).Where(x => x != "USD").Select(x => $"USD/{x}")).ToArray();
            var simulatedIds = allAssetIds.Intersect(_spotFactors.Keys).ToArray();

            foreach (var simulatedId in simulatedIds)
            {
                var surf = new ConstantVolSurface(_model.VanillaModel.BuildDate, _spotFactors[simulatedId]) { AssetId = simulatedId };
                if (_returns.TryGetValue(simulatedId, out var returns))
                    surf.Returns = returns;
                _model.VanillaModel.AddVolSurface(simulatedId, surf);
                if (simulatedId.Length == 6 && simulatedId[3] == '/')
                {
                    _model.VanillaModel.FundingModel.VolSurfaces.Add(simulatedId, surf);
                }
            }

            _logger.LogInformation("Simulating {nFac} spot factors", simulatedIds.Length);

            var mcSettings = new McSettings
            {
                McModelType = _modelType,
                Generator = RandomGeneratorType.MersenneTwister,
                NumberOfPaths = 2048,
                NumberOfTimesteps = 2,
                ReportingCurrency = _currencyProvider.GetCurrencySafe("USD")
            };

            var vd = _model.VanillaModel.BuildDate.AddDays(1);
            var fp = new FactorReturnPayoff(simulatedIds, new DateTime[] { _model.VanillaModel.BuildDate, vd });

            var mcModel = new AssetFxMCModel(_model.VanillaModel.BuildDate, fp, _model.VanillaModel, mcSettings, _currencyProvider, _futureSettingsProvider, _calendarProvider);
            mcModel.Engine.RunProcess();

            var dix = fp.DateIndices[vd];
            for (var p = 0; p < mcSettings.NumberOfPaths; p++)
            {
                var pModel = _model.Rebuild(_model.VanillaModel, _portfolio);
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

            _varEngine = new VaREngine(_logger, _model, _portfolio, _bumpedModels.Select((x, ix) => (ix, x)).ToDictionary(kv => kv.ix.ToString(), kv => kv.x));
        }

        private static bool IsFx(string assetId) => assetId.Length == 7 && assetId[3] == '/';

        public (double VaR, string ScenarioId, double cVaR) CalculateVaR(double ci, Currency ccy, string[] excludeTradeIds)
            => _varEngine.CalculateVaR(ci, ccy, excludeTradeIds);

        public (double VaR, string ScenarioId, double cVaR) CalculateVaRInc(double ci, Currency ccy, string[] includeTradeIds)
            => _varEngine.CalculateVaRInc(ci, ccy, includeTradeIds);

        public double[] CalculateVaRRange(double[] cis) => _varEngine.CalculateVaRRange(cis);

        public Dictionary<string, double> GetBaseValuations() => _varEngine.GetBaseValuations();

        public Dictionary<string, double> GetContributions(int ix) => _varEngine.GetContributions(ix.ToString());

        public decimal ComputeStress(string insId, decimal shockSize, int? nNearestSamples = null)
            => _varEngine.ComputeStress(insId, shockSize, nNearestSamples);

        public StressTestResult ComputeStressObject(string insId, decimal shockSize, int? nNearestSamples = null)
            => _varEngine.ComputeStressObject(insId, shockSize, nNearestSamples);

        public (double VaR, string ScenarioId, double cVaR) CalculateVaR(double ci, Currency ccy) => CalculateVaR(ci, ccy, _portfolio);

        public (double VaR, string ScenarioId, double cVaR) CalculateVaR(double ci, Currency ccy, Portfolio pf, bool parallelize = true)
            => _varEngine.CalculateVaR(ci, ccy, pf, parallelize);
    }
}
