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
    public class OneTouch : PathProductBase
    {
        private object _locker = new object();

        private readonly DateTime _obsStart;
        private readonly DateTime _obsEnd;
        private readonly double _barrier;
        private readonly BarrierType _barrierType;
        private readonly BarrierSide _barrierSide;

        private Vector<double> _barrierVec;

        public OneTouch(string assetName, DateTime obsStart, DateTime obsEnd, double barrier, string discountCurve, Currency ccy, DateTime payDate, double notional, BarrierSide barrierSide, BarrierType barrierType)
            : base(assetName, discountCurve, ccy, payDate, notional)
        {
            _obsStart = obsStart;
            _obsEnd = obsEnd;
            _barrier = barrier;

            _barrierType = barrierType;
            _barrierSide = barrierSide;

            _notional = new Vector<double>(notional);
            _barrierVec = new Vector<double>(barrier);
        }

        public override void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            
            var obsStart = dates.GetDateIndex(_obsStart);
            var obsEnd = dates.GetDateIndex(_obsEnd);
            _dateIndexes = Enumerable.Range(obsStart, (obsEnd - obsStart) + 1).ToArray();

            var engine = collection.GetFeature<IEngineFeature>();
            _results = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count];
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
           
                    if (_barrierType == BarrierType.KI)
                    {
                        var payoff = -barrierHit * _notional;
                        var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                        _results[resultIx] = payoff;
                    }
                    else //KO
                    {
                        var didntHit = (new Vector<double>(1.0)) + barrierHit;
                        var payoff = didntHit * _notional;
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
                 
                    if (_barrierType == BarrierType.KI)
                    {
                        var payoff = barrierHit * _notional;
                        var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                        _results[resultIx] = payoff;
                    }
                    else //KO
                    {
                        var didntHit = (new Vector<double>(1.0)) + barrierHit;
                        var payoff = didntHit * _notional;
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
        }
    }
}
