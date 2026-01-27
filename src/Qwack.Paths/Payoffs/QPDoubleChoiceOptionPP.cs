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
    public class QPDoubleChoiceOptionPP : IPathProcess, IRequiresFinish, IAssetPathPayoff, IRequiresPriceEstimators
    {
        private readonly List<DateTime[]> _avgDates;
        private readonly DateTime _decisionDate1;
        private readonly DateTime _decisionDate2;
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
        private List<int[]> _dateIndexesFuture;
        private int[] _nPast;
        private int[] _nFuture;
        private int[] _nTotal;
        private int _decisionDate1Ix;
        private int _decisionDate2Ix;
        private Vector<double>[] _results;
        private Vector<double> _notional;
        private bool _isComplete;
        private bool _isOption;

        private double[] _contangoScaleFactors;
        private double[] _periodPremia;

        private ITimeStepsFeature _dates;
        private IFeatureCollection _featureCollection;

        private Vector<double>[][] _exercisedPeriod;

        private readonly Vector<double> _one = new(1.0);

        public string RegressionKey => _assetName + (_fxName != null ? $"*{_fxName}" : "");

        public IForwardPriceEstimate SettlementRegressor1 { get; set; }
        public IForwardPriceEstimate SettlementRegressor2 { get; set; }
        public IForwardPriceEstimate[] AverageRegressors1 { get; set; }
        public IForwardPriceEstimate[] AverageRegressors2 { get; set; }

        public string FixingId { get; set; }
        public string FixingIdDateShifted { get; set; }

        public IFixingDictionary Fixings { get; set; }
        public IFixingDictionary FixingsDateShifted { get; set; }

        public IAssetFxModel VanillaModel { get; set; }

        public double OptionPremiumTotal { get; set; }
        public DateTime? OptionPremiumSettleDate { get; set; }

        public QPDoubleChoiceOptionPP(string assetName,
                                              List<DateTime[]> avgDates,
                                              DateTime decisionDate1,
                                              DateTime decisionDate2,
                                              DateTime[] settlementFixingDates,
                                              DateTime payDate,
                                              OptionType callPut,
                                              string discountCurve,
                                              Currency ccy,
                                              double notional,
                                              bool isOption = false,
                                              int? declaredPeriod = null,
                                              DateShifter dateShifter = null,
                                              double[] periodPremia = null)
        {
            _avgDates = avgDates;
            _decisionDate1 = decisionDate1.Date.AddDays(1).AddTicks(-1);
            _decisionDate2 = decisionDate2.Date.AddDays(1).AddTicks(-1);
            _callPut = callPut;
            _discountCurve = discountCurve;
            _ccy = ccy;
            _settleFixingDates = settlementFixingDates;
            _payDate = payDate;
            _assetName = assetName;
            _notional = new Vector<double>(notional);
            _isOption = isOption;
            _dateShifter = dateShifter;
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
            _dateIndexesFuture = _avgDates
                .Select(y => y                
                    .Select(x => dates.GetDateIndex(x))
                    .ToArray())
                .ToList();
            _decisionDate1Ix = dates.GetDateIndex(_decisionDate1);
            _decisionDate2Ix = dates.GetDateIndex(_decisionDate2);

            _nPast = [.. _dateIndexesFuture.Select(x => 0)];
            _nFuture = [.. _dateIndexesFuture.Select(x => x.Length)];
            _nTotal = [.. _nPast.Select((x, ix) => x + _nFuture[ix])];

            var engine = collection.GetFeature<IEngineFeature>();
            _results = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count];

            _exercisedPeriod = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count][];
            
            var curve = VanillaModel?.GetPriceCurve(_assetName);
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
            if (_settleSpec1 != null && SettlementRegressor1 == null)
            {
                SettlementRegressor1 = _featureCollection.GetPriceEstimator(_settleSpec1);
            }
            if (_settleSpec2 != null && SettlementRegressor2 == null)
            {
                SettlementRegressor2 = _featureCollection.GetPriceEstimator(_settleSpec2);
            }

            foreach (var spec in _avgSpecs1)
            {
                AverageRegressors1 = [.. _avgSpecs1.Select(x => x == null ? null : _featureCollection.GetPriceEstimator(x))];
            }
            foreach (var spec in _avgSpecs1)
            {
                AverageRegressors2 = [.. _avgSpecs2.Select(x => x == null ? null : _featureCollection.GetPriceEstimator(x))];
            }

            var blockBaseIx = block.GlobalPathIndex;
            var nTotalVec = new Vector<double>[_nTotal.Length];
            for (var i = 0; i < nTotalVec.Length; i++)
                nTotalVec[i] = new Vector<double>(_nTotal[i]);

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var globalPathIndex = blockBaseIx + path;
                var resultIx = (blockBaseIx + path) / Vector<double>.Count;

                var steps = block.GetStepsForFactor(path, _assetIndex);
                Span<Vector<double>> stepsFx = null;
                if (_fxName != null)
                    stepsFx = block.GetStepsForFactor(path, _fxIndex);
                var avgs1 = new Vector<double>[_dateIndexes.Count];
                var avgs2 = new Vector<double>[_dateIndexes.Count];
                var avg1Vec = new Vector<double>((_callPut == OptionType.C) ? double.MaxValue : double.MinValue);
                var avg2Vec = new Vector<double>((_callPut == OptionType.C) ? double.MaxValue : double.MinValue);

                var spotAtExpiry1 = steps[_decisionDate1Ix] * (_fxName != null ? stepsFx[_decisionDate1Ix] : _one);
                var spotAtExpiry2 = steps[_decisionDate2Ix] * (_fxName != null ? stepsFx[_decisionDate2Ix] : _one);

                var setReg1 = new double[Vector<double>.Count];
                var setReg2 = new double[Vector<double>.Count];
                for (var i = 0; i < Vector<double>.Count; i++)
                {
                    setReg1[i] = SettlementRegressor1?.GetEstimate(spotAtExpiry1[i], globalPathIndex + i) ?? spotAtExpiry1[i];
                    setReg2[i] = SettlementRegressor2?.GetEstimate(spotAtExpiry2[i], globalPathIndex + i) ?? spotAtExpiry2[i];
                }
                var setVec1 = new Vector<double>(setReg1);
                var setVec2 = new Vector<double>(setReg2);

                //evaluate averages at first expiry
                for (var a = 0; a < _dateIndexes.Count; a++)
                {
                    var futSum = new double[Vector<double>.Count];
                    for (var i = 0; i < Vector<double>.Count; i++)
                    {
                        futSum[i] = AverageRegressors1[a] == null ? 0.0 : AverageRegressors1[a].GetEstimate(spotAtExpiry1[i], globalPathIndex+i) * _nFuture[a];
                        
                    }
                    var futVec = new Vector<double>(futSum);
                    avgs1[a] = futVec / nTotalVec[a] + new Vector<double>(_periodPremia[a]);

                    avg1Vec = (_callPut == OptionType.C) ?
                          Vector.Min(avgs1[a], avg1Vec) :
                          Vector.Max(avgs1[a], avg1Vec);

                    if (a > 0)
                    {
                        futSum = new double[Vector<double>.Count];
                        for (var i = 0; i < Vector<double>.Count; i++)
                        {
                            futSum[i] = AverageRegressors2[a] == null ? 0.0 : AverageRegressors2[a].GetEstimate(spotAtExpiry2[i], globalPathIndex + i) * _nFuture[a];

                        }
                        futVec = new Vector<double>(futSum);
                        avgs2[a] = futVec / nTotalVec[a] + new Vector<double>(_periodPremia[a]);


                        avg2Vec = (_callPut == OptionType.C) ?
                            Vector.Min(avgs2[a], avg2Vec) :
                            Vector.Max(avgs2[a], avg2Vec);
                    }
                }


                var payoffArray = new double[Vector<double>.Count];
                var exBlock = new double[Vector<double>.Count];
                //loop vector element by element to see which was chosen at first exercise
                for (var i = 0; i < Vector<double>.Count; i++)
                {
                    if (avg1Vec[i] == avgs1[0][i]) //assume first exercise, do not continue along path
                    {
                        payoffArray[i] = (_callPut == OptionType.C ? setVec1[i] - avg1Vec[i] : avg1Vec[i] - setVec1[i]);
                    }
                    else
                    {
                        payoffArray[i] = (_callPut == OptionType.C ? setVec2[i] - avg2Vec[i] : avg2Vec[i] - setVec2[i]);
                    }
                }
               
                var payoff = new Vector<double>(payoffArray);
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
            dates.AddDate(_decisionDate1);
            dates.AddDate(_decisionDate2);
        }

        private ForwardPriceEstimatorSpec _settleSpec1;
        private ForwardPriceEstimatorSpec _settleSpec2;
        private List<ForwardPriceEstimatorSpec> _avgSpecs1 = [];
        private List<ForwardPriceEstimatorSpec> _avgSpecs2 = [];

        public List<ForwardPriceEstimatorSpec> GetRequiredEstimators(IAssetFxModel vanillaModel)
        {
            var o = new List<ForwardPriceEstimatorSpec>();
            var curve = vanillaModel.GetPriceCurve(_assetName);

            if (!string.IsNullOrEmpty(FixingId))
                Fixings = vanillaModel.GetFixingDictionary(FixingId);
            if (!string.IsNullOrEmpty(FixingIdDateShifted))
                FixingsDateShifted = vanillaModel.GetFixingDictionary(FixingIdDateShifted);

            var spec1 = new ForwardPriceEstimatorSpec
            {
                AssetId = _assetName,
                AverageDates = _settleFixingDates,
                ValDate = _decisionDate1,
                //DateShifter = _dateShifter
            };
            o.Add(spec1);
            _settleSpec1 = spec1;

            var spec2 = new ForwardPriceEstimatorSpec
            {
                AssetId = _assetName,
                AverageDates = _settleFixingDates,
                ValDate = _decisionDate2,
                //DateShifter = _dateShifter
            };
            o.Add(spec2);
            _settleSpec2 = spec2;

            foreach (var ad in _avgDates)
            {
                if (ad.Last() > _decisionDate1)
                {
                    var pointsPastDecisionDate = ad.Where(d => d > _decisionDate1).ToArray();
                    var spec = new ForwardPriceEstimatorSpec
                    {
                        AssetId = _assetName,
                        AverageDates = pointsPastDecisionDate,
                        ValDate = _decisionDate1,
                        DateShifter = _dateShifter
                    };
                    o.Add(spec);
                    _avgSpecs1.Add(spec);
                }
                else
                    _avgSpecs1.Add(null);

                if (ad.Last() > _decisionDate2)
                {
                    var pointsPastDecisionDate = ad.Where(d => d > _decisionDate2).ToArray();
                    var spec = new ForwardPriceEstimatorSpec
                    {
                        AssetId = _assetName,
                        AverageDates = pointsPastDecisionDate,
                        ValDate = _decisionDate2,
                        DateShifter = _dateShifter
                    };
                    o.Add(spec);
                    _avgSpecs2.Add(spec);
                }
                else
                    _avgSpecs2.Add(null);
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
