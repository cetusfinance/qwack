using System;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Paths.Features;

namespace Qwack.Paths.Payoffs
{
    public class DoubleNoTouch : PathProductBase
    {
        private readonly DateTime _obsStart;
        private readonly DateTime _obsEnd;
        private readonly BarrierType _barrierType;
        private Vector<double> _barrierUpVec;
        private Vector<double> _barrierDownVec;

        public DoubleNoTouch(string assetName, DateTime obsStart, DateTime obsEnd, double barrierDown, double barrierUp, string discountCurve, Currency ccy, DateTime payDate, double notional, BarrierType barrierType, Currency simulationCcy)
         : base(assetName, discountCurve, ccy, payDate, notional, simulationCcy)
        {
            _obsStart = obsStart;
            _obsEnd = obsEnd;
            _barrierType = barrierType;

            _notional = new Vector<double>(notional);
            _barrierUpVec = new Vector<double>(barrierUp);
            _barrierDownVec = new Vector<double>(barrierDown);
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
            SetupForCcyConversion(collection);
            _isComplete = true;
        }

        public override void Process(IPathBlock block)
        {
            var blockBaseIx = block.GlobalPathIndex;

            var barrierDownHit = new Vector<double>(0);
            var barrierUpHit = new Vector<double>(0);
            var expiryValue = new Vector<double>(0);

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIndex);
                var minValue = new Vector<double>(double.MaxValue);
                var maxValue = new Vector<double>(double.MinValue);
                for (var i = 0; i < _dateIndexes.Length; i++)
                {
                    minValue = Vector.Min(steps[_dateIndexes[i]], minValue);
                    maxValue = Vector.Max(steps[_dateIndexes[i]], maxValue);
                }
                barrierDownHit = Vector.Abs(Vector.ConvertToDouble(Vector.LessThan(minValue, _barrierDownVec)));
                barrierUpHit = Vector.ConvertToDouble(Vector.GreaterThan(maxValue, _barrierUpVec));


                if (_barrierType == BarrierType.KI)
                {
                    var barrierHit = Vector.BitwiseAnd(barrierDownHit, barrierUpHit);
                    var payoff = barrierHit * _notional;
                    ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());
                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    _results[resultIx] = payoff;
                }
                else //KO
                {
                    var barrierHit = Vector.BitwiseAnd(Vector<double>.One - barrierDownHit, Vector<double>.One - barrierUpHit);
                    var payoff = barrierHit * _notional;
                    ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());
                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    _results[resultIx] = payoff;
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
