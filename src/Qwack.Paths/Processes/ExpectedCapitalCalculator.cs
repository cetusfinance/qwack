using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Numerics;
using Qwack.Math.Extensions;
using System.Linq;
using Qwack.Core.Models;
using Qwack.Serialization;
using static System.Math;
using Qwack.Core.Instruments;
using Qwack.Core.Curves;
using Qwack.Core.Basic;

namespace Qwack.Paths.Processes
{
    public class ExpectedCapitalCalculator : IPathProcess, IRequiresFinish
    {
        private int _factorIndex;
        private int _nPaths;
        private bool _isComplete;
    
        public double[] PathSum { get; private set; }
        public double[] PathAvg => PathSum.Select(x => x / _nPaths).ToArray();

        public Dictionary<int,double[]> PathSumsByBlock { get; private set; }
        public Dictionary<int, int> PathCountsByBlock { get; private set; }

        public ExpectedCapitalCalculator(Portfolio portfolio, double counterpartyRiskWeight, Dictionary<string,string> assetIdToGroupMap, Currency reportingCurrency, IAssetFxModel assetFxModel, DateTime[] calculationDates)
        {
            _portfolio = portfolio;
            _counterpartyRiskWeight = counterpartyRiskWeight;
            _assetIdToGroupMap = assetIdToGroupMap;
            _reportingCurrency = reportingCurrency;
            _assetFxModel = assetFxModel;
            _calculationDates = calculationDates;
            _assetIds = _portfolio.AssetIds();
        }

        public Dictionary<DateTime, double> ExpectedCapital => _expectedCapital;

        public bool IsComplete => _isComplete;

        private object _threadLock = new object();
        private readonly Portfolio _portfolio;
        private readonly double _counterpartyRiskWeight;
        private readonly Dictionary<string, string> _assetIdToGroupMap;
        private readonly Currency _reportingCurrency;
        private readonly IAssetFxModel _assetFxModel;
        private readonly DateTime[] _calculationDates;
        private readonly string[] _assetIds;

        private int[] _calculationDateIndices;
        private int[] _assetIndices;

        private readonly Dictionary<DateTime, double> _expectedCapital = new Dictionary<DateTime, double>();

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var stepsByFactor = new Dictionary<int, Vector<double>[]>();
                foreach(var ix in _assetIndices)
                    stepsByFactor.Add(ix, block.GetStepsForFactor(path, ix).ToArray());

                var indexCounter = 0;
                var nextIndex = _calculationDateIndices[indexCounter];

                var steps = stepsByFactor.First().Value;

                for (var i = 0; i < steps.Length; i++)
                {
                    if (i == nextIndex)
                    {
                        var currentDate = _calculationDates[indexCounter];
                        var newModel = _assetFxModel.Clone();
                        newModel.OverrideBuildDate(currentDate);
                        var spotsByAsset = stepsByFactor.ToDictionary(x=>x.Key, x => x.Value[i]);
                        for (var j = 0; j < Vector<double>.Count; j++)
                        {
                            //build on-path price curves in model
                            foreach(var spot in spotsByAsset)
                            {
                                var assetId = _assetIds[spot.Key];
                                var baseCurve = _assetFxModel.GetPriceCurve(assetId);
                                var spotOnCurve = baseCurve.GetPriceForFixingDate(currentDate);
                                var ratio = spot.Value[j] / spotOnCurve;
                                var newCurve = new FactorPriceCurve(baseCurve, ratio);
                                newModel.AddPriceCurve(assetId, newCurve);
                            }

                            var ead = _portfolio.SaCcrEAD(newModel, _reportingCurrency, _assetIdToGroupMap);
                            var capital = _counterpartyRiskWeight * ead;
                            if (!_expectedCapital.ContainsKey(currentDate))
                                lock (_threadLock)
                                {
                                    if (!_expectedCapital.ContainsKey(currentDate))
                                        _expectedCapital.Add(currentDate, 0.0);
                                }
                            _expectedCapital[currentDate] += capital;
                        }

                        indexCounter++;
                        nextIndex = indexCounter < _calculationDateIndices.Length 
                            ? _calculationDateIndices[indexCounter] : int.MaxValue;
                    }
                }

                foreach (var key in _expectedCapital.Keys)
                {
                    _expectedCapital[key] /= _nPaths;
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
            var dates = collection.GetFeature<ITimeStepsFeature>();
            _calculationDateIndices = _calculationDates.Select(d => dates.GetDateIndex(d)).ToArray();
            _isComplete = true;
        }
    }
}

