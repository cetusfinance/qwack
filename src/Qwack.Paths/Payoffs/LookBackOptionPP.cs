using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Paths.Features;
using Qwack.Paths.Regressors;
using Qwack.Transport.BasicTypes;

namespace Qwack.Paths.Payoffs
{
    public class LookBackOptionPP : PathProductBase
    {
        private readonly List<DateTime> _sampleDates;
        private readonly OptionType _callPut;
        private readonly DateTime _decisionDate;
        private readonly DateTime[] _settlementFixingDates;
        private string _fxName;
        private int _fxIndex;
        private int _windowSize;
        private double _expiryToSettleCarry;
        private int _decisionIx;

        private readonly Vector<double> _one = new(1.0);

        private bool IsFx => _assetName.Length == 7 && _assetName[3] == '/';

        public string RegressionKey => _assetName + (_fxName != null ? $"*{_fxName}" : "");
        public LinearAveragePriceRegressor SettlementRegressor { get; set; }

        public IAssetFxModel VanillaModel { get; set; }

        public LookBackOptionPP(string assetName, List<DateTime> sampleDates, OptionType callPut, string discountCurve, Currency ccy, DateTime payDate, double notional, Currency simulationCcy, DateTime decisionDate, DateTime[] settlementFixingDates, int windowSize = 1)
            : base(assetName, discountCurve, ccy, payDate, notional, simulationCcy)
        {
            _sampleDates = sampleDates;
            _callPut = callPut;
            _decisionDate = decisionDate;
            _settlementFixingDates = settlementFixingDates;
            _notional = new Vector<double>(notional);
            _windowSize = windowSize;

            SettlementRegressor = _settlementFixingDates.Length == 1 && _settlementFixingDates[0] == _decisionDate ?
                null : //settlement is spot price on decision date
                new LinearAveragePriceRegressor(_decisionDate, _settlementFixingDates, RegressionKey);
        }


        private Vector<double>[] _lookbackMinIxs;
        private Vector<double>[][] _averages;

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

            _decisionIx = dates.GetDateIndex(_decisionDate);

            var engine = collection.GetFeature<IEngineFeature>();

            _results = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count];

            SetupForCcyConversion(collection);

            if (VanillaModel != null)
            {
                var curve = VanillaModel.GetPriceCurve(_assetName);

                var decisionSpotDate = _decisionDate.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag);
                var settlePromptDates = _settlementFixingDates.Select(x => x.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag)).ToArray();

                _expiryToSettleCarry = curve.GetAveragePriceForDates(settlePromptDates) / curve.GetPriceForDate(decisionSpotDate);
            }

            _lookbackMinIxs = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count];
            _averages = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count][];

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
                    var minIxs = new double[Vector<double>.Count];

                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;

                    _averages[resultIx] = new Vector<double>[_dateIndexes.Length];

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

                        if (i >= (_windowSize -1) )
                        {
                            var cp = runningTotal / winVec;
                            minValue = Vector.Min(cp, minValue);
                            _averages[resultIx][i] = cp;
                            for (var v = 0; v < Vector<double>.Count; v++)
                            {
                                if (cp[v] == minValue[v])
                                    minIxs[v] = i;
                            }
                           
                        }
                    }
            
                    var spotAtDecision = steps[_decisionIx] * (_fxName != null ? stepsFx[_decisionIx] : _one);
                    var setReg = new double[Vector<double>.Count];

                    if (VanillaModel != null)
                    {
                        for (var i = 0; i < Vector<double>.Count; i++)
                        {
                            setReg[i] = spotAtDecision[i] * _expiryToSettleCarry;
                        }
                    }
                    else
                    {
                        for (var i = 0; i < Vector<double>.Count; i++)
                        {
                            setReg[i] = SettlementRegressor?.Predict(spotAtDecision[i]) ?? spotAtDecision[i];
                        }
                    }
                    var lastValue = new Vector<double>(setReg);

                    var payoff = Vector.Max(_zero, (lastValue - minValue) * _notional);

                    ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());

                    _results[resultIx] = payoff;
                    _lookbackMinIxs[resultIx] = new Vector<double>(minIxs);
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
                    var maxIxs = new double[Vector<double>.Count];
                    var winVec = new Vector<double>(_windowSize);

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

                        if (i >= _windowSize )
                        {
                            var cp = runningTotal / winVec;
                            maxValue = Vector.Max(cp, maxValue);

                            for (var v = 0; v < Vector<double>.Count; v++)
                            {
                                if (cp[v] == maxValue[v])
                                    maxIxs[v] = i;
                            }
                        }
                    }
                    var spotAtDecision = steps[_decisionIx] * (_fxName != null ? stepsFx[_decisionIx] : _one);
                    var setReg = new double[Vector<double>.Count];

                    if (VanillaModel != null)
                    {
                        for (var i = 0; i < Vector<double>.Count; i++)
                        {
                            setReg[i] = spotAtDecision[i] * _expiryToSettleCarry;
                        }
                    }
                    else
                    {
                        for (var i = 0; i < Vector<double>.Count; i++)
                        {
                            setReg[i] = SettlementRegressor?.Predict(spotAtDecision[i]) ?? spotAtDecision[i];
                        }
                    }
                    var lastValue = new Vector<double>(setReg);

                    var payoff = Vector.Max(_zero, (maxValue - lastValue) * _notional);

                    ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());

                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    _results[resultIx] = payoff;
                    _lookbackMinIxs[resultIx] = new Vector<double>(maxIxs);
                }
            }
        }

        public double[] ExerciseProbabilities
        {
            get
            {
                var vecLen = Vector<double>.Count;

                var results = new double[_dateIndexes.Length - _windowSize + 1][];
                //[];
                for (var a = _windowSize - 1; a < _dateIndexes.Length; a++)
                {
                    results[a -_windowSize + 1] = new double[_results.Length * vecLen];
                    for (var i = 0; i < _results.Length; i++)
                    {
                        for (var j = 0; j < vecLen; j++)
                        {
                            if (_lookbackMinIxs[i][j] == a)
                                results[a - _windowSize + 1][i * vecLen + j] = 1.0;
                        }
                    }
                }
                return results.Select(x => x.Average()).ToArray();
            }
        }

        public Tuple<DateTime, DateTime>[] ExercisePeriods
        {
            get
            {
                var results = new Tuple<DateTime, DateTime>[_dateIndexes.Length-_windowSize+1];
                for (var a = _windowSize - 1; a < _dateIndexes.Length; a++)
                {
                    results[a - _windowSize +1] = new Tuple<DateTime, DateTime>(_sampleDates[a - _windowSize + 1], _sampleDates[a]);
                }
                return results;
            }
        }

        public double[] Averages
        {
            get
            {
                var vecLen = Vector<double>.Count;
                var results = new double[_dateIndexes.Length][];
                //[];
                for (var a = 0; a < _dateIndexes.Length; a++)
                {
                    results[a] = new double[_results.Length * vecLen];
                    if (a >= _windowSize - 1)
                    {
                        for (var i = 0; i < _results.Length; i++)
                        {
                            for (var j = 0; j < vecLen; j++)
                            {

                                results[a - _windowSize + 1][i * vecLen + j] = _averages[i][a][j];
                            }
                        }
                    }
                }
                return results.Select(x => x.Average()).ToArray();
            }
        }

        public override void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_sampleDates);
            dates.AddDate(_decisionDate);
        }
    }
}
