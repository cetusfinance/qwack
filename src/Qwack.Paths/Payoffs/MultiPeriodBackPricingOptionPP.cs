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

namespace Qwack.Paths.Payoffs
{
    public class MultiPeriodBackPricingOptionPP : IPathProcess, IRequiresFinish, IAssetPathPayoff, IRequiresPriceEstimators
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
        private readonly DateShifter _dateShifter;

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
        private int _liveDateIx;
        private Vector<double>[] _results;
        private Vector<double> _notional;
        private bool _isComplete;
        private bool _isOption;
        private int? _declaredPeriod;

        private double? _scaleStrike;
        private double? _scaleProportion;
        private double? _scaleStrike2;
        private double? _scaleProportion2;
        private double? _scaleStrike3;
        private double? _scaleProportion3;

        private double[] _contangoScaleFactors;
        private double[] _periodPremia;

        private ITimeStepsFeature _dates;
        private IFeatureCollection _featureCollection;

        private Vector<double>[][] _exercisedPeriod;

        private readonly Vector<double> _one = new(1.0);

        public string RegressionKey => _assetName + (_fxName != null ? $"*{_fxName}" : "");

        public IForwardPriceEstimate SettlementRegressor { get; set; }
        public IForwardPriceEstimate[] AverageRegressors { get; set; }

        public string FixingId { get; set; }
        public string FixingIdDateShifted { get; set; }

        public IFixingDictionary Fixings { get; set; }
        public IFixingDictionary FixingsDateShifted { get; set; }

        public IAssetFxModel VanillaModel { get; set; }

        public double OptionPremiumTotal { get; set; }
        public DateTime? OptionPremiumSettleDate { get; set; }

        public MultiPeriodBackPricingOptionPP(string assetName,
                                              List<DateTime[]> avgDates,
                                              DateTime decisionDate,
                                              DateTime[] settlementFixingDates,
                                              DateTime payDate,
                                              OptionType callPut,
                                              string discountCurve,
                                              Currency ccy,
                                              double notional,
                                              bool isOption = false,
                                              int? declaredPeriod = null,
                                              DateShifter dateShifter = null,
                                              double? scaleStrike = null,
                                              double? scaleProportion = null,
                                              double[] periodPremia = null,
                                              double? scaleStrike2 = null,
                                              double? scaleProportion2 = null,
                                              double? scaleStrike3 = null,
                                              double? scaleProportion3 = null)
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
            _declaredPeriod = declaredPeriod;
            _dateShifter = dateShifter;
            _scaleStrike = scaleStrike;
            _scaleProportion = scaleProportion;
            _scaleStrike2 = scaleStrike2;
            _scaleProportion2 = scaleProportion2;
            _scaleStrike3 = scaleStrike3;
            _scaleProportion3 = scaleProportion3;
            _periodPremia = periodPremia ?? avgDates.Select(x => 0.0).ToArray(); //default to zero spreads

            if (_ccy.Ccy != "USD")
                _fxName = $"USD/{_ccy.Ccy}";
        }

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; private set; }

        public void Finish(IFeatureCollection collection)
        {
            _featureCollection = collection;

            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            if (!string.IsNullOrEmpty(_fxName))
                _fxIndex = dims.GetDimension(_fxName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dates = dates;
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

            _exercisedPeriod = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count][];
            
            var curve = VanillaModel.GetPriceCurve(_assetName);
            if (Fixings != null && FixingsDateShifted != null && _dateShifter != null)
            {
                var liveSpotDate = VanillaModel.BuildDate.AddPeriod(_dateShifter.RollType, _dateShifter.Calendar, _dateShifter.Period);
                _contangoScaleFactors = new double[_dates.TimeStepCount];
                for (var i = 0; i < _contangoScaleFactors.Length; i++)
                {
                    var date = _dates.Dates[i];
                    if (Fixings.TryGetValue(date, out var fix) && FixingsDateShifted.TryGetValue(date, out var fixShifted))
                    {
                        _contangoScaleFactors[i] = fixShifted / fix;
                    }
                    else
                    {
                        var spotDate = _dates.Dates[i].AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag);
                        var shiftedDate = _dates.Dates[i].AddPeriod(_dateShifter.RollType, _dateShifter.Calendar, _dateShifter.Period);
                        _contangoScaleFactors[i] = curve.GetPriceForDate(shiftedDate) / curve.GetPriceForDate(spotDate);
                    }
                }
            }



            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            if (_settleSpec != null && SettlementRegressor==null)
            {
                SettlementRegressor = _featureCollection.GetPriceEstimator(_settleSpec);
            }
            foreach (var spec in _avgSpecs)
            {
                AverageRegressors = [.. _avgSpecs.Select(x => x == null ? null : _featureCollection.GetPriceEstimator(x))];
            }

            var blockBaseIx = block.GlobalPathIndex;
            var nTotalVec = new Vector<double>[_nTotal.Length];
            for (var i = 0; i < nTotalVec.Length; i++)
                nTotalVec[i] = new Vector<double>(_nTotal[i]);

            var spotIx = _declaredPeriod.HasValue ? _liveDateIx : _decisionDateIx;

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
                        var thisStep = steps[_dateIndexesPast[a][p]] * (_fxName != null ? stepsFx[_dateIndexesPast[a][p]] : _one);
                        if (_contangoScaleFactors != null)
                        {
                            thisStep *= _contangoScaleFactors[_dateIndexesPast[a][p]];
                        }
                        pastSum += thisStep;
                    }

                    var spotAtExpiry = steps[spotIx] * (_fxName != null ? stepsFx[spotIx] : _one);
                    //if (_contangoScaleFactors != null)
                    //{
                    //    spotAtExpiry *= _contangoScaleFactors[spotIx];
                    //}


                    var futSum = new double[Vector<double>.Count];
                    for (var i = 0; i < Vector<double>.Count; i++)
                    {
                        futSum[i] = AverageRegressors[a] == null ? 0.0 : AverageRegressors[a].GetEstimate(spotAtExpiry[i]) * _nFuture[a];
                        setReg[i] = SettlementRegressor?.GetEstimate(spotAtExpiry[i]) ?? spotAtExpiry[i];
                    }
                    var futVec = new Vector<double>(futSum);
                    avgs[a] = (futVec + pastSum) / nTotalVec[a] + new Vector<double>(_periodPremia[a]);


                    avgVec = (_callPut == OptionType.C) ?
                        Vector.Min(avgs[a], avgVec) :
                        Vector.Max(avgs[a], avgVec);
                }

                var setVec = new Vector<double>(setReg);

                if (_declaredPeriod.HasValue)
                {
                    if (_declaredPeriod.Value < 0) //its abandoned
                        avgVec = setVec;
                    else
                        avgVec = avgs[_declaredPeriod.Value];
                }


                var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                _exercisedPeriod[resultIx] = new Vector<double>[_dateIndexes.Count];

                for (var a = 0; a < _dateIndexes.Count; a++)
                {
                    var exBlock = new double[Vector<double>.Count];

                    for (var v = 0; v < Vector<double>.Count; v++)
                    {
                        if (_declaredPeriod.HasValue)
                        {
                            exBlock[v] = _declaredPeriod.Value == a ? 1.0 : 0.0;
                        }
                        else
                            exBlock[v] = avgVec == avgs[a] ? 1.0 : 0.0;
                    }
                    _exercisedPeriod[resultIx][a] = new Vector<double>(exBlock);
                }

                var payoff = _callPut == OptionType.C ? setVec - avgVec : avgVec - setVec;

                if (_scaleProportion.HasValue && _scaleStrike.HasValue)
                {
                    var scalePayoff = _callPut == OptionType.C ?
                        Vector.Max(new Vector<double>(0), avgVec - new Vector<double>(_scaleStrike.Value)) * _scaleProportion.Value :
                        Vector.Max(new Vector<double>(0), new Vector<double>(_scaleStrike.Value) - avgVec) * _scaleProportion.Value;
                    payoff -= scalePayoff;
                }

                if (_scaleProportion2.HasValue && _scaleStrike2.HasValue)
                {
                    var scalePayoff = _callPut == OptionType.C ?
                         Vector.Max(new Vector<double>(0), avgVec - new Vector<double>(_scaleStrike2.Value)) * _scaleProportion2.Value :
                         Vector.Max(new Vector<double>(0), new Vector<double>(_scaleStrike2.Value) - avgVec) * _scaleProportion2.Value;
                    payoff -= scalePayoff;
                }

                if (_scaleProportion3.HasValue && _scaleStrike3.HasValue)
                {
                    var scalePayoff = _callPut == OptionType.C ?
                        Vector.Max(new Vector<double>(0), avgVec - new Vector<double>(_scaleStrike3.Value)) * _scaleProportion3.Value :
                        Vector.Max(new Vector<double>(0), new Vector<double>(_scaleStrike3.Value) - avgVec) * _scaleProportion3.Value;
                    payoff -= scalePayoff;
                }

                if (_isOption)
                {
                    payoff = Vector.Max(new Vector<double>(0), payoff);

                    if (payoff == Vector<double>.Zero) //abandon option
                    {
                        for (var a = 0; a < _dateIndexes.Count; a++)
                        {
                            _exercisedPeriod[resultIx][a] = Vector<double>.Zero;
                        }
                    }
                }

                _results[resultIx] = payoff * _notional - new Vector<double>(OptionPremiumTotal);
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

        private ForwardPriceEstimatorSpec _settleSpec;
        private List<ForwardPriceEstimatorSpec> _avgSpecs = [];

        public List<ForwardPriceEstimatorSpec> GetRequiredEstimators(IAssetFxModel vanillaModel)
        {
            var o = new List<ForwardPriceEstimatorSpec>();
            var curve = vanillaModel.GetPriceCurve(_assetName);

            if (!string.IsNullOrEmpty(FixingId))
                Fixings = vanillaModel.GetFixingDictionary(FixingId);
            if (!string.IsNullOrEmpty(FixingIdDateShifted))
                FixingsDateShifted = vanillaModel.GetFixingDictionary(FixingIdDateShifted);

            if (!(_settleFixingDates.Length == 1 && _settleFixingDates[0] == _decisionDate)) // || _dateShifter != null )
            {
                var spec = new ForwardPriceEstimatorSpec
                {
                    AssetId = _assetName,
                    AverageDates = _settleFixingDates,
                    ValDate = _decisionDate,
                    //DateShifter = _dateShifter
                };
                o.Add(spec);
                _settleSpec = spec;
            }

            foreach (var ad in _avgDates)
            {
                if (ad.Last() > _decisionDate)
                {
                    var pointsPastDecisionDate = ad.Where(d => d > _decisionDate).ToArray();
                    var spec = new ForwardPriceEstimatorSpec
                    {
                        AssetId = _assetName,
                        AverageDates = pointsPastDecisionDate,
                        ValDate = _decisionDate,
                        DateShifter = _dateShifter
                    };
                    o.Add(spec);
                    _avgSpecs.Add(spec);
                }
                else
                    _avgSpecs.Add(null);
            }

            return o;
        }

        public double AverageResult => _results.Select(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec.Average();
        }).Average();

        public double[] ExerciseProbabilities
        {
            get
            {
                var vecLen = Vector<double>.Count;
                var results = new double[_dateIndexes.Count][];
                //[];
                for (var a = 0; a < _dateIndexes.Count; a++)
                {
                    results[a] = new double[_results.Length * vecLen];
                    for (var i = 0; i < _results.Length; i++)
                    {
                        for (var j = 0; j < vecLen; j++)
                        {

                            results[a][i * vecLen + j] = _exercisedPeriod[i][a][j];
                        }
                    }
                }
                return results.Select(x=>x.Average()).ToArray();
            }
        }

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
                Flows =
                [
                    new CashFlow
                    {
                        Fv = ar,
                        Pv = ar * model.FundingModel.Curves[_discountCurve].GetDf(model.BuildDate,_payDate),
                        Currency = _ccy,
                        FlowType =  FlowType.FixedAmount,
                        SettleDate = _payDate,
                        YearFraction = 1.0
                    },
                ]
            };
        }

        public CashFlowSchedule[] ExpectedFlowsByPath(IAssetFxModel model)
        {
            var df = model.FundingModel.Curves[_discountCurve].GetDf(model.BuildDate, _payDate);
            return ResultsByPath.Select(x => new CashFlowSchedule
            {
                Flows =
                [
                    new CashFlow
                    {
                        Fv = x,
                        Pv = x * df,
                        Currency = _ccy,
                        FlowType =  FlowType.FixedAmount,
                        SettleDate = _payDate,
                        YearFraction = 1.0
                    }
                ]
            }).ToArray();

        }
    }
}
