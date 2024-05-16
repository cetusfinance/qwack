using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Paths.Features;
using Qwack.Transport.BasicTypes;

namespace Qwack.Paths.Payoffs
{
    public class LookBackOption : PathProductBase
    {
        private readonly List<DateTime> _sampleDates;
        private readonly OptionType _callPut;
        private string _fxName;
        private int _fxIndex;
        private int _windowSize;

        private readonly Vector<double> _one = new(1.0);

        private bool IsFx => _assetName.Length == 7 && _assetName[3] == '/';

        public override string RegressionKey => _assetName + (_fxName != null ? $"*{_fxName}" : "");

        public LookBackOption(string assetName, List<DateTime> sampleDates, OptionType callPut, string discountCurve, Currency ccy, DateTime payDate, double notional, Currency simulationCcy, int windowSize = 1)
            : base(assetName, discountCurve, ccy, payDate, notional, simulationCcy)
        {
            _sampleDates = sampleDates;
            _callPut = callPut;
            _notional = new Vector<double>(notional);
            _windowSize = windowSize;
        }

        public override void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            if (!IsFx && _ccy.Ccy != "USD")
            {
                _fxName = $"USD/{_ccy.Ccy}";
                _fxIndex = dims.GetDimension(_fxName);
            }

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIndexes = new int[_sampleDates.Count];
            for (var i = 0; i < _sampleDates.Count; i++)
            {
                _dateIndexes[i] = dates.GetDateIndex(_sampleDates[i]);
            }

            var engine = collection.GetFeature<IEngineFeature>();

            _results = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count];

            SetupForCcyConversion(collection);
            _isComplete = true;
        }

        public override void Process(IPathBlock block)
        {
            var blockBaseIx = block.GlobalPathIndex;

            if (_callPut == OptionType.C)
            {
                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var steps = block.GetStepsForFactor(path, _assetIndex);
                    Span<Vector<double>> stepsFx = null;
                    if (_fxName != null)
                        stepsFx = block.GetStepsForFactor(path, _fxIndex);
                    
                    var winVec = new Vector<double>(_windowSize);
                    var runningTotal = new Vector<double>(0);
                    var minValue = new Vector<double>(double.MaxValue);
                    for (var i = 0; i < _dateIndexes.Length; i++)
                    {
                        var point = steps[_dateIndexes[i]] * (_fxName != null ? stepsFx[_dateIndexes[i]] : _one);
                        if (i < _windowSize)
                        {
                            runningTotal += point;
                        }
                        else
                        {
                            var pointToRemove = steps[_dateIndexes[i-_windowSize]] * (_fxName != null ? stepsFx[_dateIndexes[i - _windowSize]] : _one);
                            runningTotal += point - pointToRemove;
                        }

                        if(i>=(_windowSize-1))
                            minValue = Vector.Min(runningTotal/ winVec, minValue);
                    }
                    var lastValue = steps[_dateIndexes.Last()] * (_fxName != null ? stepsFx[_dateIndexes.Last()] : _one);
                    var payoff = Vector.Max(_zero,(lastValue - minValue) * _notional);

                    ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());

                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    _results[resultIx] = payoff;
                }
            }
            else
            {
                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var steps = block.GetStepsForFactor(path, _assetIndex);
                    Span<Vector<double>> stepsFx = null;
                    if (_fxName != null)
                        stepsFx = block.GetStepsForFactor(path, _fxIndex);

                    var runningTotal = new Vector<double>(0);
                    var maxValue = new Vector<double>(double.MinValue);
                    for (var i = 0; i < _dateIndexes.Length; i++)
                    {
                        var point = steps[_dateIndexes[i]] * (_fxName != null ? stepsFx[_dateIndexes[i]] : _one);
                        if (i < _windowSize)
                        {
                            runningTotal += point;
                        }
                        else
                        {
                            var pointToRemove = steps[_dateIndexes[i - _windowSize]] * (_fxName != null ? stepsFx[_dateIndexes[i - _windowSize]] : _one);
                            runningTotal += point - pointToRemove;
                        }

                        if (i >= (_windowSize - 1))
                            maxValue = Vector.Max(runningTotal, maxValue);
                    }
                    var lastValue = steps[_dateIndexes.Last()] * (_fxName != null ? stepsFx[_dateIndexes.Last()] : _one);
                    var payoff = Vector.Max(_zero, (maxValue - lastValue) * _notional);

                    ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());

                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    _results[resultIx] = payoff;
                }
            }
        }

        public override void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_sampleDates);
        }
    }
}
