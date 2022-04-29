using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Paths.Features;

namespace Qwack.Paths.Processes
{
    public class ExpectedCapitalCalculator : IPathProcess, IRequiresFinish
    {
        private readonly int _factorIndex;
        private int _nPaths;
        private bool _isComplete;

        public ExpectedCapitalCalculator(Portfolio portfolio, double counterpartyRiskWeight, Dictionary<string, string> assetIdToGroupMap, Currency reportingCurrency, IAssetFxModel assetFxModel, DateTime[] calculationDates)
        {
            _portfolio = portfolio;
            _counterpartyRiskWeight = counterpartyRiskWeight;
            _assetIdToGroupMap = assetIdToGroupMap;
            _reportingCurrency = reportingCurrency;
            _assetFxModel = assetFxModel;
            _calculationDates = calculationDates;
            _assetIds = _portfolio.AssetIds().Concat(_portfolio.FxPairs(assetFxModel)).ToArray();

        }

        public Dictionary<DateTime, double> ExpectedCapital => _expectedCapital.ToDictionary(x => x.Key, x => x.Value / _nPaths);

        public bool IsComplete => _isComplete;

        private readonly object _threadLock = new();
        private readonly Portfolio _portfolio;
        private readonly double _counterpartyRiskWeight;
        private readonly Dictionary<string, string> _assetIdToGroupMap;
        private readonly Currency _reportingCurrency;
        private readonly IAssetFxModel _assetFxModel;
        private readonly DateTime[] _calculationDates;
        private readonly string[] _assetIds;

        private int[] _calculationDateIndices;
        private int[] _assetIndices;

        private ITimeStepsFeature _timeFeature;

        private readonly Dictionary<DateTime, double> _expectedCapital = new();

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var stepsByFactor = new Dictionary<int, Vector<double>[]>();
                foreach (var ix in _assetIndices)
                    stepsByFactor.Add(ix, block.GetStepsForFactor(path, ix).ToArray());

                var indexCounter = 0;
                var nextIndex = _calculationDateIndices[indexCounter];

                var steps = stepsByFactor.First().Value;
                var fixingDictionaries = _assetFxModel.FixingDictionaryNames.ToDictionary(x => x, x => (IFixingDictionary)new FixingDictionary(_assetFxModel.GetFixingDictionary(x)));

                for (var i = 0; i < steps.Length; i++)
                {
                    var d = _timeFeature.Dates[i];
                    for (var j = 0; j < Vector<double>.Count; j++)
                    {
                        for (var a = 0; a < _assetIndices.Length; a++)
                        {
                            if (!fixingDictionaries.TryGetValue(_assetIds[a], out var fd))
                                throw new Exception($"Fixing dictionary not found for asset {_assetIds[a]} ");
                            fd[d] = stepsByFactor[a][i][j];
                        }
                    }

                    if (i == nextIndex)
                    {
                        var currentDate = _calculationDates[indexCounter];
                        var newModel = _assetFxModel.Clone();
                        newModel.OverrideBuildDate(currentDate);
                        newModel.AddFixingDictionaries(fixingDictionaries);

                        var spotsByAsset = stepsByFactor.ToDictionary(x => x.Key, x => x.Value[i]);
                        for (var j = 0; j < Vector<double>.Count; j++)
                        {
                            //build on-path price curves in model
                            foreach (var spot in spotsByAsset)
                            {
                                var assetId = _assetIds[spot.Key];
                                var baseCurve = _assetFxModel.GetPriceCurve(assetId);
                                var spotOnCurve = baseCurve.GetPriceForFixingDate(currentDate);
                                var ratio = spot.Value[j] / spotOnCurve;
                                var newCurve = new FactorPriceCurve(baseCurve, ratio);
                                newModel.AddPriceCurve(assetId, newCurve);
                            }

                            var ead = 0.0;// _portfolio.SaCcrEAD(newModel, _reportingCurrency, _assetIdToGroupMap);
                            var capital = _counterpartyRiskWeight * ead;
                            if (!_expectedCapital.ContainsKey(currentDate))
                                lock (_threadLock)
                                {
                                    if (!_expectedCapital.ContainsKey(currentDate))
                                        _expectedCapital.Add(currentDate, 0.0);
                                }
                            if (double.IsNaN(capital) || double.IsInfinity(capital))
                                throw new Exception("Invalid capital generated");

                            _expectedCapital[currentDate] += capital;
                        }

                        indexCounter++;
                        nextIndex = indexCounter < _calculationDateIndices.Length
                            ? _calculationDateIndices[indexCounter] : int.MaxValue;
                    }
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_calculationDates);

            var dims = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _assetIndices = _assetIds.Select(a => dims.GetDimension(a)).ToArray();

            var engine = pathProcessFeaturesCollection.GetFeature<IEngineFeature>();
            _nPaths = engine.NumberOfPaths;
        }

        public void Finish(IFeatureCollection collection)
        {
            _timeFeature = collection.GetFeature<ITimeStepsFeature>();
            _calculationDateIndices = _calculationDates.Select(d => _timeFeature.GetDateIndex(d)).ToArray();
            _isComplete = true;
        }
    }
}

