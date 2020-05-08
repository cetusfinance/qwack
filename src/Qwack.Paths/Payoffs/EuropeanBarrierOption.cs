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
using Qwack.Transport.BasicTypes;

namespace Qwack.Paths.Payoffs
{
    public class EuropeanBarrierOption : PathProductBase
    {
        private readonly DateTime _obsStart;
        private readonly DateTime _obsEnd;
        private readonly DateTime _expiry;
        private readonly OptionType _callPut;
        private readonly BarrierType _barrierType;
        private readonly BarrierSide _barrierSide;

        private int _expiryIx;

        private Vector<double> _barrierVec;
        private Vector<double> _strikeVec;

        public EuropeanBarrierOption(string assetName, DateTime obsStart, DateTime obsEnd, DateTime expiry, OptionType callPut, double strike, double barrier, string discountCurve, Currency ccy, DateTime payDate, double notional, BarrierSide barrierSide, BarrierType barrierType, Currency simulationCcy)
             : base(assetName, discountCurve, ccy, payDate, notional, simulationCcy)
        {
            _obsStart = obsStart;
            _obsEnd = obsEnd;
            _expiry = expiry;
            _callPut = callPut;

            _barrierType = barrierType;
            _barrierSide = barrierSide;

            _notional = new Vector<double>(notional);
            _barrierVec = new Vector<double>(barrier);
            _strikeVec = new Vector<double>(strike);
        }

        public override void Finish(IFeatureCollection collection)
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
            SetupForCcyConversion(collection);
            _isComplete = true;
        }

        public override void Process(IPathBlock block)
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
                    barrierHit = Vector.ConvertToDouble(Vector.LessThan(minValue, _barrierVec));
                    expiryValue = steps[_expiryIx];

                    var vanillaValue = _callPut == OptionType.C
                        ? Vector.Max(expiryValue - _strikeVec, _zero)
                        : Vector.Max(_strikeVec - expiryValue, _zero);

                    if (_barrierType == BarrierType.KI)
                    {
                        var payoff = vanillaValue * barrierHit * _notional;
                        ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());
                        var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                        _results[resultIx] = payoff;
                    }
                    else //KO
                    {
                        var didntHit = (new Vector<double>(1.0)) - barrierHit;
                        var payoff = vanillaValue * didntHit * _notional;
                        ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());
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
                    barrierHit = Vector.ConvertToDouble(Vector.GreaterThan(maxValue, _barrierVec));
                    expiryValue = steps[_expiryIx];

                    var vanillaValue = _callPut == OptionType.C
                        ? Vector.Max(expiryValue - _strikeVec, _zero)
                        : Vector.Max(_strikeVec - expiryValue, _zero);

                    if (_barrierType == BarrierType.KI)
                    {
                        var payoff = vanillaValue * barrierHit * _notional;
                        ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());
                        var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                        _results[resultIx] = payoff;
                    }
                    else //KO
                    {
                        var didntHit = (new Vector<double>(1.0)) + barrierHit;
                        var payoff = vanillaValue * didntHit * _notional;
                        ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());
                        var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                        _results[resultIx] = payoff;
                    }
                }
            }
        }

        public override void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDate(_obsStart);
            dates.AddDate(_obsEnd);
            dates.AddDate(_expiry);
        }
    }
}
