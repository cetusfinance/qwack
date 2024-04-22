using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Paths.Features;
using Qwack.Paths.Regressors;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;

namespace Qwack.Paths.Payoffs
{
    public class MultiPeriodBackPricingOptionPP : IPathProcess, IRequiresFinish, IAssetPathPayoff
    {
        private readonly object _locker = new();

        private readonly List<DateTime[]> _avgDates;
        private readonly DateTime _decisionDate;
        private readonly OptionType _callPut;
        private readonly string _discountCurve;
        private readonly Currency _ccy;
        private readonly DateTime[] _settleFixingDates;
        private readonly DateTime _payDate;
        private readonly string _assetName;
        private int _assetIndex;
        private readonly string _fxName;
        private int _fxIndex;
        private List<int[]> _dateIndexes;
        private List<int[]> _dateIndexesPast;
        private List<int[]> _dateIndexesFuture;
        private int[] _nPast;
        private int[] _nFuture;
        private int[] _nTotal;
        private int _decisionDateIx;
        private Vector<double>[] _results;
        private Vector<double> _notional;
        private bool _isComplete;
        private double _expiryToSettleCarry;
        private double[] _expiryToAvgCarrys;
        private bool _isOption;

        private readonly Vector<double> _one = new(1.0);

        public string RegressionKey => _assetName + (_fxName != null ? $"*{_fxName}" : "");

        public LinearAveragePriceRegressor SettlementRegressor { get; set; }
        public LinearAveragePriceRegressor[] AverageRegressors { get; set; }

        public IAssetFxModel VanillaModel { get; set; }

        public MultiPeriodBackPricingOptionPP(string assetName, List<DateTime[]> avgDates, DateTime decisionDate, DateTime[] settlementFixingDates, DateTime payDate, OptionType callPut, string discountCurve, Currency ccy, double notional, bool isOption = false)
        {
            _avgDates = avgDates;
            _decisionDate = decisionDate;
            _callPut = callPut;
            _discountCurve = discountCurve;
            _ccy = ccy;
            _settleFixingDates = settlementFixingDates;
            _payDate = payDate;
            _assetName = assetName;
            _notional = new Vector<double>(notional);
            _isOption = isOption;

            if (_ccy.Ccy != "USD")
                _fxName = $"USD/{_ccy.Ccy}";

            AverageRegressors = avgDates.Select(avg => avg.Last() > decisionDate ? new LinearAveragePriceRegressor(decisionDate, avg.Where(x => x > decisionDate).ToArray(), RegressionKey) : null).ToArray();
            SettlementRegressor = _settleFixingDates.Length==1 && settlementFixingDates[0] ==_decisionDate ? 
                null : //settlement is spot price on decision date
                new LinearAveragePriceRegressor(decisionDate, _settleFixingDates, RegressionKey);
        }

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; private set; }

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            if (!string.IsNullOrEmpty(_fxName))
                _fxIndex = dims.GetDimension(_fxName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIndexes = _avgDates
                .Select(x => x.Select(y => dates.GetDateIndex(y)).ToArray())
                .ToList();
            _dateIndexesPast = _avgDates
                .Select(y => y
                    .Where(x => x <= _decisionDate)
                    .Select(x => dates.GetDateIndex(x))
                    .ToArray())
                .ToList();
            _dateIndexesFuture = _avgDates
                .Select(y => y
                    .Where(x => x > _decisionDate)
                    .Select(x => dates.GetDateIndex(x))
                    .ToArray())
                .ToList();
            _decisionDateIx = dates.GetDateIndex(_decisionDate);

            _nPast = _dateIndexesPast.Select(x => x.Length).ToArray();
            _nFuture = _dateIndexesFuture.Select(x => x.Length).ToArray();
            _nTotal = _nPast.Select((x, ix) => x + _nFuture[ix]).ToArray();

            var engine = collection.GetFeature<IEngineFeature>();
            _results = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count];

            if (VanillaModel != null)
            {
                var curve = VanillaModel.GetPriceCurve(_assetName);
                var decisionSpotDate = _decisionDate.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag);
                var settlePromptDates = _settleFixingDates.Select(x => x.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag)).ToArray();

                _expiryToSettleCarry = curve.GetAveragePriceForDates(settlePromptDates) / curve.GetPriceForDate(decisionSpotDate);

                var expToAvg = new List<double>();
                foreach(var ad in _avgDates)
                {
                    double carry = 0;
                    if(ad.First()>_decisionDate)
                    {
                        var pointsPastDecisionDate = ad.Where(d => d > _decisionDate).Select(x=>x.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag)).ToArray();
                        carry = curve.GetAveragePriceForDates(pointsPastDecisionDate) / curve.GetPriceForDate(decisionSpotDate);
                    }
                    expToAvg.Add(carry);
                }
                _expiryToAvgCarrys = expToAvg.ToArray();
            }

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            var blockBaseIx = block.GlobalPathIndex;
            var nTotalVec = new Vector<double>[_nTotal.Length];
            for (var i = 0; i < nTotalVec.Length; i++)
                nTotalVec[i] = new Vector<double>(_nTotal[i]);

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIndex);
                Span<Vector<double>> stepsFx = null;
                if (_fxName != null)
                    stepsFx = block.GetStepsForFactor(path, _fxIndex);
                var avgs = new Vector<double>[_dateIndexes.Count];
                var avgVec = new Vector<double>((_callPut == OptionType.C) ? double.MaxValue : double.MinValue);
                var setReg = new double[Vector<double>.Count];

                for (var a = 0; a < _dateIndexes.Count; a++)
                {
                    var pastSum = new Vector<double>(0.0);
                    for (var p = 0; p < _dateIndexesPast[a].Length; p++)
                    {
                        pastSum += steps[_dateIndexesPast[a][p]] * (_fxName != null ? stepsFx[_dateIndexesPast[a][p]] : _one);
                    }

                    var spotAtExpiry = steps[_decisionDateIx] * (_fxName != null ? stepsFx[_decisionDateIx] : _one);

                    if (VanillaModel != null)
                    {
                        var futSum = new double[Vector<double>.Count];

                        for (var i = 0; i < Vector<double>.Count; i++)
                        {
                            futSum[i] = spotAtExpiry[i] * _expiryToAvgCarrys[a] * _nFuture[a];
                            setReg[i] = spotAtExpiry[i] * _expiryToSettleCarry;
                        }
                        var futVec = new Vector<double>(futSum);
                        avgs[a] = (futVec + pastSum) / nTotalVec[a];
                    }
                    else
                    {
                        var futSum = new double[Vector<double>.Count];
                        for (var i = 0; i < Vector<double>.Count; i++)
                        {
                            futSum[i] = AverageRegressors[a] == null ? 0.0 : AverageRegressors[a].Predict(spotAtExpiry[i]) * _nFuture[a];
                            setReg[i] = SettlementRegressor?.Predict(spotAtExpiry[i]) ?? spotAtExpiry[i];
                        }
                        var futVec = new Vector<double>(futSum);
                        avgs[a] = (futVec + pastSum) / nTotalVec[a];
                    }

                    avgVec = (_callPut == OptionType.C) ?
                        Vector.Min(avgs[a], avgVec) :
                        Vector.Max(avgs[a], avgVec);
                }

                var setVec = new Vector<double>(setReg);

                var payoff = _callPut == OptionType.C ? setVec - avgVec : avgVec - setVec;
                if (_isOption)
                {
                    payoff = Vector.Max(new Vector<double>(0), payoff);
                }

                var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                _results[resultIx] = payoff * _notional;
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_avgDates.SelectMany(x => x));
            dates.AddDates(_settleFixingDates);
            dates.AddDate(_payDate);
            dates.AddDate(_decisionDate);
        }

        public double AverageResult => _results.Select(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec.Average();
        }).Average();

        public double[] ResultsByPath
        {
            get
            {
                var vecLen = Vector<double>.Count;
                var results = new double[_results.Length * vecLen];
                for (var i = 0; i < _results.Length; i++)
                {
                    for (var j = 0; j < vecLen; j++)
                    {
                        results[i * vecLen + j] = _results[i][j];
                    }
                }
                return results;
            }
        }

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
                        YearFraction = 1.0
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
                        YearFraction = 1.0
                    }
                }
            }).ToArray();

        }
    }
}
