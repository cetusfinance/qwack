using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Paths.Features;

namespace Qwack.Paths.Payoffs
{
    public class EuropeanBarrierOption : IPathProcess, IRequiresFinish, IAssetPathPayoff
    {
        private object _locker = new object();

        private readonly DateTime _obsStart;
        private readonly DateTime _obsEnd;
        private readonly DateTime _expiry;
        private readonly OptionType _callPut;
        private readonly double _strike;
        private readonly double _barrier;
        private readonly string _discountCurve;
        private readonly Currency _ccy;
        private readonly DateTime _payDate;
        private readonly BarrierType _barrierType;
        private readonly BarrierSide _barrierSide;
        private readonly string _assetName;
        private int _assetIndex;
        private int[] _dateIndexes;
        private int _expiryIx;
        private Vector<double>[] _results;
        private Vector<double> _notional;
        private Vector<double> _barrierVec;
        private Vector<double> _strikeVec;
        private Vector<double> _zero = new Vector<double>(0.0);
        private bool _isComplete;

        public string RegressionKey => _assetName;


        public EuropeanBarrierOption(string assetName, DateTime obsStart, DateTime obsEnd, DateTime expiry, OptionType callPut, double strike, double barrier, string discountCurve, Currency ccy, DateTime payDate, double notional, BarrierSide barrierSide, BarrierType barrierType)
        {
            _obsStart = obsStart;
            _obsEnd = obsEnd;
            _expiry = expiry;
            _callPut = callPut;
            _strike = strike;
            _barrier = barrier;
            _discountCurve = discountCurve;
            _ccy = ccy;
            _payDate = payDate;

            _barrierType = barrierType;
            _barrierSide = barrierSide;

            _assetName = assetName;
            _notional = new Vector<double>(notional);
            _barrierVec = new Vector<double>(barrier);
            _strikeVec = new Vector<double>(strike);
        }

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; private set; }


        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            
            var obsStart = dates.GetDateIndex(_obsStart);
            var obsEnd = dates.GetDateIndex(_obsEnd);
            _expiryIx = dates.GetDateIndex(_expiry);
            _dateIndexes = Enumerable.Range(obsStart, (obsEnd - obsStart) + 1).ToArray();

            var engine = collection.GetFeature<IEngineFeature>();
            _results = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count];
            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            var blockBaseIx = block.GlobalPathIndex;

            var barrierHit = new Vector<double>(0);
            var expiryValue = new Vector<double>(0);
            if (_barrierSide == BarrierSide.Down)
            {
                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var steps = block.GetStepsForFactor(path, _assetIndex);
                    var minValue = new Vector<double>(double.MaxValue);
                    for (var i = 0; i < _dateIndexes.Length; i++)
                    {
                        minValue = Vector.Min(steps[_dateIndexes[i]], minValue);
                    }
                    barrierHit = Vector.LessThan<double>(minValue, _barrierVec);
                    expiryValue = steps[_expiryIx];

                    var vanillaValue = _callPut == OptionType.C
                        ? Vector.Max(expiryValue - _strikeVec, _zero)
                        : Vector.Max(_strikeVec - expiryValue, _zero);

                    if (_barrierType == BarrierType.KI)
                    {
                        var payoff = vanillaValue * barrierHit * _notional;
                        var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                        _results[resultIx] = payoff;
                    }
                    else //KO
                    {
                        var didntHit = (new Vector<double>(1.0)) - barrierHit;
                        var payoff = vanillaValue * didntHit * _notional;
                        var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                        _results[resultIx] = payoff;
                    }
                }
            }
            else
            {
                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var steps = block.GetStepsForFactor(path, _assetIndex);
                    var maxValue = new Vector<double>(double.MinValue);
                    for (var i = 0; i < _dateIndexes.Length; i++)
                    {
                        maxValue = Vector.Max(steps[_dateIndexes[i]], maxValue);
                    }
                    barrierHit = Vector.GreaterThan<double>(maxValue, _barrierVec);
                    expiryValue = steps[_expiryIx];

                    var vanillaValue = _callPut == OptionType.C
                        ? Vector.Max(expiryValue - _strikeVec, _zero)
                        : Vector.Max(_strikeVec - expiryValue, _zero);

                    if (_barrierType == BarrierType.KI)
                    {
                        var payoff = vanillaValue * barrierHit * _notional;
                        var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                        _results[resultIx] = payoff;
                    }
                    else //KO
                    {
                        var didntHit = (new Vector<double>(1.0)) - barrierHit;
                        var payoff = vanillaValue * didntHit * _notional;
                        var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                        _results[resultIx] = payoff;
                    }
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDate(_obsStart);
            dates.AddDate(_obsEnd);
            dates.AddDate(_expiry);
        }

        public double AverageResult => _results.Select(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec.Average();
        }).Average();

        public double[] ResultsByPath => _results.SelectMany(x => x.Values()).ToArray();

        public double ResultStdError => _results.SelectMany(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec;
        }).StdDev();



        public CashFlowSchedule ExpectedFlows(IAssetFxModel model)
        {
            var ar = AverageResult;
            return new CashFlowSchedule
            {
                Flows = new List<CashFlow>
                {
                    new CashFlow
                    {
                        Fv = ar,
                        Pv = ar * model.FundingModel.Curves[_discountCurve].GetDf(model.BuildDate,_payDate),
                        Currency = _ccy,
                        FlowType =  FlowType.FixedAmount,
                        SettleDate = _payDate,
                        NotionalByYearFraction = 1.0
                    }
                }
            };
        }

        public CashFlowSchedule[] ExpectedFlowsByPath(IAssetFxModel model)
        {
            var df = model.FundingModel.Curves[_discountCurve].GetDf(model.BuildDate, _payDate);
            return ResultsByPath.Select(x => new CashFlowSchedule
            {
                Flows = new List<CashFlow>
                {
                    new CashFlow
                    {
                        Fv = x,
                        Pv = x * df,
                        Currency = _ccy,
                        FlowType =  FlowType.FixedAmount,
                        SettleDate = _payDate,
                        NotionalByYearFraction = 1.0
                    }
                }
            }).ToArray();

        }
    }
}
