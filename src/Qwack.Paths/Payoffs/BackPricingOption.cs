using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Paths.Features;
using Qwack.Paths.Regressors;

namespace Qwack.Paths.Payoffs
{
    public class BackPricingOption : PathProductBase
    {
        private List<DateTime> _avgDates;
        private readonly DateTime _decisionDate;
        private readonly OptionType _callPut;
        private readonly DateTime _settleFixingDate;
        private string _fxName;
        private int _fxIndex;
        private int[] _dateIndexesPast;
        private int[] _dateIndexesFuture;
        private int _nPast;
        private int _nFuture;
        private int _nTotal;
        private int _decisionDateIx;
        private double _expiryToSettleCarry;

        private readonly Vector<double> _one = new Vector<double>(1.0);

        public override string RegressionKey => _assetName + (_fxName != null ? $"*{_fxName}" : "");

        public LinearAveragePriceRegressor SettlementRegressor { get; set; }
        public LinearAveragePriceRegressor AverageRegressor { get; set; }

        public IAssetFxModel VanillaModel { get; set; }

        public BackPricingOption(string assetName, List<DateTime> avgDates, DateTime decisionDate, DateTime settlementFixingDate, DateTime payDate, OptionType callPut, string discountCurve, Currency ccy, double notional, Currency simulationCcy)
            : base(assetName, discountCurve, ccy, payDate, notional, simulationCcy)
        {
            _avgDates = avgDates;
            _decisionDate = decisionDate;
            _callPut = callPut;
            _settleFixingDate = settlementFixingDate;
            _notional = new Vector<double>(notional);

            if (_ccy.Ccy != "USD")
                _fxName = $"USD/{_ccy.Ccy}";

            AverageRegressor = avgDates.Last() > decisionDate ? new LinearAveragePriceRegressor(decisionDate, avgDates.Where(x => x > decisionDate).ToArray(), RegressionKey) : null;
            SettlementRegressor = new LinearAveragePriceRegressor(decisionDate, new[] { _settleFixingDate }, RegressionKey);
        }

        public override void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            if (!string.IsNullOrEmpty(_fxName))
                _fxIndex = dims.GetDimension(_fxName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIndexes = _avgDates
                .Select(x => dates.GetDateIndex(x))
                .ToArray();
            _dateIndexesPast = _avgDates
                .Where(x => x <= _decisionDate)
                .Select(x => dates.GetDateIndex(x))
                .ToArray();
            _dateIndexesFuture = _avgDates
                .Where(x => x > _decisionDate)
                .Select(x => dates.GetDateIndex(x))
                .ToArray();
            _decisionDateIx = dates.GetDateIndex(_decisionDate);

            _nPast = _dateIndexesPast.Length;
            _nFuture = _dateIndexesFuture.Length;
            _nTotal = _nPast + _nFuture;

            var engine = collection.GetFeature<IEngineFeature>();
            _results = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count];

            if (VanillaModel != null)
            {
                var curve = VanillaModel.GetPriceCurve(_assetName);
                var decisionSpotDate = _decisionDate.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag);
                _expiryToSettleCarry = curve.GetPriceForDate(_payDate) - curve.GetPriceForDate(decisionSpotDate);
            }

            SetupForCcyConversion(collection);
            _isComplete = true;
        }

        public override void Process(IPathBlock block)
        {
            var blockBaseIx = block.GlobalPathIndex;
            var nTotalVec = new Vector<double>(_nTotal);

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIndex);
                Span<Vector<double>> stepsFx = null;
                if (_fxName != null)
                    stepsFx = block.GetStepsForFactor(path, _fxIndex);

                var pastSum = new Vector<double>(0.0);
                for(var p=0;p<_dateIndexesPast.Length;p++)
                {
                    pastSum += steps[_dateIndexesPast[p]] * (_fxName != null ? stepsFx[_dateIndexesPast[p]] : _one);
                }

                var spotAtExpiry = steps[_decisionDateIx] * (_fxName != null ? stepsFx[_decisionDateIx] : _one);

                if (VanillaModel != null && AverageRegressor == null)
                {
                    var setReg = new double[Vector<double>.Count];
                    for (var i = 0; i < Vector<double>.Count; i++)
                          setReg[i] = spotAtExpiry[i] + _expiryToSettleCarry;
                    
                    var avgVec =  pastSum / nTotalVec;
                    var setVec = new Vector<double>(setReg);

                    var payoff = (_callPut == OptionType.C) ?
                            Vector.Max(new Vector<double>(0), setVec - avgVec) :
                            Vector.Max(new Vector<double>(0), avgVec - setVec);
                    ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());
                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    _results[resultIx] = payoff * _notional;
                }
                else
                {
                    var futSum = new double[Vector<double>.Count];
                    var setReg = new double[Vector<double>.Count];
                    for (var i = 0; i < Vector<double>.Count; i++)
                    {
                        futSum[i] = AverageRegressor == null ? 0.0 : AverageRegressor.Predict(spotAtExpiry[i]) * _nFuture;
                        setReg[i] = SettlementRegressor.Predict(spotAtExpiry[i]);
                    }
                    var futVec = new Vector<double>(futSum);
                    var avgVec = (futVec + pastSum) / nTotalVec;
                    var setVec = new Vector<double>(setReg);

                    var payoff = (_callPut == OptionType.C) ?
                            Vector.Max(new Vector<double>(0), setVec - avgVec) :
                            Vector.Max(new Vector<double>(0), avgVec - setVec);
                    ConvertToSimCcyIfNeeded(block, path, payoff, _dateIndexes.Last());
                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    _results[resultIx] = payoff * _notional;
                }
            }
        }

        public override void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_avgDates);
            dates.AddDate(_payDate);
            dates.AddDate(_decisionDate);
        }

    }
}
