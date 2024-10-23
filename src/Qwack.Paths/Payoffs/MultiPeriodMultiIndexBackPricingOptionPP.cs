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
using Qwack.Transport.BasicTypes;

namespace Qwack.Paths.Payoffs
{
    public class MultiPeriodMultiIndexBackPricingOptionPP : IPathProcess, IRequiresFinish, IAssetPathPayoff
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
        private double _notionalDbl;
        private bool _isComplete;
        
        private double _expiryToSettleCarry;
        private double[] _expiryToAvgCarries3mAsk;
        private double[] _expiryToAvgCarries3mBid;
        private double[] _expiryToAvgCarriesCashAsk;
        private double[] _expiryToAvgCarriesCashBid;

        private bool _isOption;
        private int? _declaredPeriod;
        private double? _scaleStrike;
        private double? _scaleProportion;

        private double[] _contangoScaleFactors3mBid;
        private double[] _contangoScaleFactors3mAsk;
        private double[] _contangoScaleFactorsCashBid;
        private double[] _periodPremia;


        private Vector<double>[][] _exercisedPeriod;

        private readonly Vector<double> _one = new(1.0);

        public string RegressionKey => _assetName + (_fxName != null ? $"*{_fxName}" : "");

        public string FixingIdBid { get; set; }
        public string FixingIdAsk { get; set; }
        public string FixingId3mBid { get; set; }
        public string FixingId3mAsk { get; set; }

        public IFixingDictionary FixingsCashBid { get; set; }
        public IFixingDictionary Fixings3mBid { get; set; }
        public IFixingDictionary FixingsCashAsk { get; set; }
        public IFixingDictionary Fixings3mAsk { get; set; }
        public double BidAskSpread { get; set; }

        public double OptionPremiumTotal { get; set; }
        public DateTime? OptionPremiumSettleDate { get; set; }

        public IAssetFxModel VanillaModel { get; set; }

        public MultiPeriodMultiIndexBackPricingOptionPP(string assetName,
                                              List<DateTime[]> avgDates,
                                              DateTime decisionDate,
                                              DateTime[] settlementFixingDates,
                                              DateTime payDate,
                                              OptionType callPut,
                                              string discountCurve,
                                              Currency ccy,
                                              double notional,
                                              DateShifter dateShifter,
                                              bool isOption = false,
                                              int? declaredPeriod = null,
                                              double? scaleStrike = null,
                                              double? scaleProportion = null,
                                              double[] periodPremia = null,
                                              double optionPremiumTotal = 0,
                                              DateTime? optionPremiumSettleDate = null)
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
            _notionalDbl = notional;
            _isOption = isOption;
            _declaredPeriod = declaredPeriod;
            _dateShifter = dateShifter;
            _scaleStrike =  scaleStrike;
            _scaleProportion = scaleProportion;
            _periodPremia = periodPremia ?? avgDates.Select(x => 0.0).ToArray(); //default to zero spreads

            if (_ccy.Ccy != "USD")
                _fxName = $"USD/{_ccy.Ccy}";

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
                _liveDateIx = dates.GetDateIndex(VanillaModel.BuildDate);
                var liveSpotDate = VanillaModel.BuildDate.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag);

                if (!string.IsNullOrEmpty(FixingIdBid))
                    FixingsCashBid = VanillaModel.GetFixingDictionary(FixingIdBid);
                if (!string.IsNullOrEmpty(FixingIdAsk))
                    FixingsCashAsk = VanillaModel.GetFixingDictionary(FixingIdAsk);
                if (!string.IsNullOrEmpty(FixingId3mBid))
                    Fixings3mBid = VanillaModel.GetFixingDictionary(FixingId3mBid);
                if (!string.IsNullOrEmpty(FixingId3mAsk))
                    Fixings3mAsk = VanillaModel.GetFixingDictionary(FixingId3mAsk);


                liveSpotDate = VanillaModel.BuildDate.AddPeriod(_dateShifter.RollType, _dateShifter.Calendar, _dateShifter.Period);

                _contangoScaleFactors3mBid = new double[dates.TimeStepCount];
                _contangoScaleFactors3mAsk = new double[dates.TimeStepCount];
                _contangoScaleFactorsCashBid = new double[dates.TimeStepCount];
                for (var i = 0; i < dates.TimeStepCount; i++)
                {
                    var date = dates.Dates[i];
                    if (FixingsCashAsk.TryGetValue(date, out var fixCashAsk))
                    {
                        if(Fixings3mAsk.TryGetValue(date, out var fix3mAsk))
                            _contangoScaleFactors3mAsk[i] = fix3mAsk / fixCashAsk;
                        if (Fixings3mBid.TryGetValue(date, out var fix3mBid))
                            _contangoScaleFactors3mBid[i] = fix3mBid / fixCashAsk;
                        if (FixingsCashBid.TryGetValue(date, out var fixCashBid))
                            _contangoScaleFactorsCashBid[i] = fixCashBid / fixCashAsk;
                    }
                    else
                    {
                        var spotDate = dates.Dates[i].AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag);
                        var shiftedDate = dates.Dates[i].AddPeriod(_dateShifter.RollType, _dateShifter.Calendar, _dateShifter.Period);
                        _contangoScaleFactors3mAsk[i] = curve.GetPriceForDate(shiftedDate) / curve.GetPriceForDate(spotDate);
                        _contangoScaleFactors3mBid[i] = (curve.GetPriceForDate(shiftedDate) - BidAskSpread) / curve.GetPriceForDate(spotDate);
                        _contangoScaleFactorsCashBid[i] = (curve.GetPriceForDate(spotDate) - BidAskSpread) / curve.GetPriceForDate(spotDate);
                    }
                }

                var decisionSpotDate = _decisionDate.AddPeriod(RollType.F, _dateShifter.Calendar, 2.Bd());
                var settlePromptDates = _settleFixingDates.Select(x => x.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag)).ToArray();

                _expiryToSettleCarry = curve.GetAveragePriceForDates(settlePromptDates) / curve.GetPriceForDate(_declaredPeriod.HasValue ? liveSpotDate : decisionSpotDate);

                var expToAvg3mAsk = new List<double>();
                var expToAvgCashAsk = new List<double>();
                var expToAvg3mBid = new List<double>();
                var expToAvgCashBid = new List<double>();
                var spotOnDecDate = curve.GetPriceForDate(_declaredPeriod.HasValue ? liveSpotDate : decisionSpotDate);

                foreach (var ad in _avgDates)
                {
                    double carryTo3mAsk = 0;
                    double carryToCashAsk = 0;
                    double carryTo3mBid = 0;
                    double carryToCashBid = 0;
                    if (ad.Last() > _decisionDate)
                    {
                        var pointsPastDecisionDateFixing = ad.Where(d => d > _decisionDate).ToArray();
                        var pointsPastDecisionDate3m = pointsPastDecisionDateFixing.Select(x => x.AddPeriod(_dateShifter.RollType, _dateShifter.Calendar, _dateShifter.Period)).ToArray();
                        var pointsPastDecisionDateCash = pointsPastDecisionDateFixing.Select(x => x.AddPeriod(RollType.F, _dateShifter.Calendar, 2.Bd())).ToArray();

                        var avgCash = curve.GetAveragePriceForDates(pointsPastDecisionDateCash);
                        var avg3m = curve.GetAveragePriceForDates(pointsPastDecisionDate3m);

                        carryTo3mAsk = avg3m / spotOnDecDate;
                        carryToCashAsk = avgCash / spotOnDecDate;
                        carryTo3mBid = (avg3m - BidAskSpread) / spotOnDecDate;
                        carryToCashBid = (avgCash - BidAskSpread) / spotOnDecDate;
                    }
                    expToAvg3mAsk.Add(carryTo3mAsk);
                    expToAvg3mBid.Add(carryTo3mBid);
                    expToAvgCashAsk.Add(carryToCashAsk);
                    expToAvgCashBid.Add(carryToCashBid);
                }
                _expiryToAvgCarries3mAsk = expToAvg3mAsk.ToArray();
                _expiryToAvgCarries3mBid = expToAvg3mBid.ToArray();
                _expiryToAvgCarriesCashAsk = expToAvgCashAsk.ToArray();
                _expiryToAvgCarriesCashBid = expToAvgCashBid.ToArray();

            }

            _exercisedPeriod = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count][];
            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            var blockBaseIx = block.GlobalPathIndex;
            var nTotalVec = new Vector<double>[_nTotal.Length];
            for (var i = 0; i < nTotalVec.Length; i++)
                nTotalVec[i] = new Vector<double>(_nTotal[i]);
            var isCall = _callPut == OptionType.C;
            var spotIx = _declaredPeriod.HasValue ? _liveDateIx : _decisionDateIx;

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIndex);
                Span<Vector<double>> stepsFx = null;
                if (_fxName != null)
                    stepsFx = block.GetStepsForFactor(path, _fxIndex);

                var avgsCash = new Vector<double>[_dateIndexes.Count];
                var avgs3m = new Vector<double>[_dateIndexes.Count];
                
                var avgVec = new Vector<double>(isCall ? double.MaxValue : double.MinValue);
                var setReg = new double[Vector<double>.Count];

                //each "a" represents an averaging period
                for (var a = 0; a < _dateIndexes.Count; a++)
                {
                    var expiryToAvgCarrysCash = isCall ? _expiryToAvgCarriesCashBid : _expiryToAvgCarriesCashAsk;
                    var expiryToAvgCarrys3m = isCall ? _expiryToAvgCarries3mBid : _expiryToAvgCarries3mAsk;

                    var pastSumCash = new Vector<double>(0.0);
                    var pastSum3m = new Vector<double>(0.0);

                    for (var p = 0; p < _dateIndexesPast[a].Length; p++)
                    {
                        var step = _dateIndexesPast[a][p];
                        var thisStepCash = steps[step] * (_fxName != null ? stepsFx[step] : _one);

                        var thisStep3m = thisStepCash * (isCall ? _contangoScaleFactors3mBid[step] : _contangoScaleFactors3mAsk[step]);
                        if (isCall)
                            thisStepCash *= _contangoScaleFactorsCashBid[step];

                        pastSumCash += thisStepCash;
                        pastSum3m += thisStep3m;
                    }

                    var spotAtExpiry = steps[spotIx] * (_fxName != null ? stepsFx[spotIx] : _one);


                    var futSumCash = new double[Vector<double>.Count];
                    var futSum3m = new double[Vector<double>.Count];

                    for (var i = 0; i < Vector<double>.Count; i++)
                    {
                        futSumCash[i] = spotAtExpiry[i] * expiryToAvgCarrysCash[a] * _nFuture[a];
                        futSum3m[i] = spotAtExpiry[i] * expiryToAvgCarrys3m[a] * _nFuture[a];
                        setReg[i] = spotAtExpiry[i] * _expiryToSettleCarry;
                    }
                    
                    var futVecCash = new Vector<double>(futSumCash);
                    var futVec3m = new Vector<double>(futSum3m);

                    avgsCash[a] = (futVecCash + pastSumCash) / nTotalVec[a] + new Vector<double>(_periodPremia[a]);
                    avgs3m[a] = (futVec3m + pastSum3m) / nTotalVec[a] + new Vector<double>(_periodPremia[a]);

                    avgVec = (_callPut == OptionType.C) ?
                        Vector.Min(Vector.Min(avgsCash[a], avgs3m[a]), avgVec) :
                        Vector.Max(Vector.Max(avgsCash[a], avgs3m[a]), avgVec);
                }

                var setVec = new Vector<double>(setReg);

                if (_declaredPeriod.HasValue)
                {
                    if (_declaredPeriod.Value < 0) //its abandoned
                        avgVec = setVec;
                    else
                        avgVec = (_callPut == OptionType.C) ?
                            Vector.Min(avgsCash[_declaredPeriod.Value], avgs3m[_declaredPeriod.Value]) :
                            Vector.Max(avgsCash[_declaredPeriod.Value], avgs3m[_declaredPeriod.Value]);
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
                            exBlock[v] = (avgVec == avgsCash[a] || avgVec == avgs3m[a]) ? 1.0 : 0.0;
                    }
                    _exercisedPeriod[resultIx][a] = new Vector<double>(exBlock);
                }

                var payoff = _callPut == OptionType.C ? setVec - avgVec : avgVec - setVec;
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

                if(_scaleProportion.HasValue && _scaleStrike.HasValue)
                {
                    var scalePayoff = Vector.Max(new Vector<double>(0), avgVec - new Vector<double>(_scaleStrike.Value)) * _scaleProportion.Value;
                    payoff -= scalePayoff;
                }

                _results[resultIx] = payoff * _notional - new Vector<double>(OptionPremiumTotal / _notionalDbl);
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
