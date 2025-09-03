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
    public class StripQPSwaptionPP(string assetName,
                                          DateTime[] qps,
                                          int[] offsets,
                                          DateTime decisionDate,
                                          DateTime payDate,
                                          OptionType callPut,
                                          string discountCurve,
                                          Currency ccy,
                                          double notional,
                                          ICalendarProvider calendarProvider,
                                          DateShifter dateShifter = null) : IPathProcess, IRequiresFinish, IAssetPathPayoff, IRequiresPriceEstimators
    {
        
        private readonly DateTime[][][] _avgDates = new DateTime[qps.Length][][];
        private readonly DateTime[] _settleFixingDates;
        private int _assetIndex;

        private int _decisionDateIx;
        private int _liveDateIx;
        private Vector<double>[] _results;
        private Vector<double> _notional = new Vector<double>(notional);
        private bool _isComplete;
        private bool _isOption = true;
        private int? _declaredPeriod;
        private double[] _contangoScaleFactors;

        private ITimeStepsFeature _dates;
        private IFeatureCollection _featureCollection;

        private Vector<double>[][] _exercisedPeriod;

        private readonly Vector<double> _one = new(1.0);

        public string RegressionKey => assetName;

        public IForwardPriceEstimate SettlementRegressor { get; set; }
        public IForwardPriceEstimate[][] AverageRegressors { get; set; }

        public string FixingId { get; set; }
        public string FixingIdDateShifted { get; set; }

        public IFixingDictionary Fixings { get; set; }
        public IFixingDictionary FixingsDateShifted { get; set; }

        public IAssetFxModel VanillaModel { get; set; }

        public double OptionPremiumTotal { get; set; }
        public DateTime? OptionPremiumSettleDate { get; set; }

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; private set; }

        public void Finish(IFeatureCollection collection)
        {
            _featureCollection = collection;

            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(assetName);


            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dates = dates;
            _decisionDateIx = dates.GetDateIndex(decisionDate);

        

            var engine = collection.GetFeature<IEngineFeature>();
            _results = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count];

            _exercisedPeriod = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count][];
            
            //var curve = VanillaModel.GetPriceCurve(assetName);
            //if (Fixings != null && FixingsDateShifted != null && dateShifter != null)
            //{
            //    var liveSpotDate = VanillaModel.BuildDate.AddPeriod(dateShifter.RollType, dateShifter.Calendar, dateShifter.Period);
            //    _contangoScaleFactors = new double[_dates.TimeStepCount];
            //    for (var i = 0; i < _contangoScaleFactors.Length; i++)
            //    {
            //        var date = _dates.Dates[i];
            //        if (Fixings.TryGetValue(date, out var fix) && FixingsDateShifted.TryGetValue(date, out var fixShifted))
            //        {
            //            _contangoScaleFactors[i] = fixShifted / fix;
            //        }
            //        else
            //        {
            //            var spotDate = _dates.Dates[i].AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag);
            //            var shiftedDate = _dates.Dates[i].AddPeriod(dateShifter.RollType, dateShifter.Calendar, dateShifter.Period);
            //            _contangoScaleFactors[i] = curve.GetPriceForDate(shiftedDate) / curve.GetPriceForDate(spotDate);
            //        }
            //    }
            //}

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            if (_settleSpec != null && SettlementRegressor==null)
            {
                SettlementRegressor = _featureCollection.GetPriceEstimator(_settleSpec);
            }
            for(var i=0;i<_avgSpecs.Count;i++)
            {
                for(var j=0;j<_avgSpecs[i].Count;j++)
                {
                        AverageRegressors[i][j] = _featureCollection.GetPriceEstimator(_avgSpecs[i][j]);
                }
            }


            var blockBaseIx = block.GlobalPathIndex;
            var spotIx = _liveDateIx;

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIndex);
                var swapRates = new Vector<double>[offsets.Length];
                var strikes = new Vector<double>();
                for (var q=0;q<_avgDates.Length;q++)
                {
                    //ignore discounting for now
                    var strikeArr = new double[Vector<double>.Count];
                    for (var i = 0; i < Vector<double>.Count; i++)
                    {
                        strikeArr[i] = AverageRegressors[q][0].GetEstimate(steps[_decisionDateIx][i], blockBaseIx + path + i) / _avgDates.Length;
                    }
                    strikes += new Vector<double>(strikeArr);

                    for (var o = 1; o < offsets.Length; o++)
                    {
                        var swapRatesArr = new double[Vector<double>.Count];
                        for (var i = 0; i < Vector<double>.Count; i++)
                        {
                            swapRatesArr[i] = AverageRegressors[q][o].GetEstimate(steps[_decisionDateIx][i], blockBaseIx + path + i) / _avgDates.Length;
                        }
                        swapRates[o] += new Vector<double>(swapRatesArr);
                    }
                }
    
                var avgVec = new Vector<double>((callPut == OptionType.C) ? double.MaxValue : double.MinValue);

                for(var i=1;i<offsets.Length;i++)
                {
                    avgVec = (callPut == OptionType.C) ?
                        Vector.Min(swapRates[i], avgVec) :
                        Vector.Max(swapRates[i], avgVec);
                }

                var payoff = callPut == OptionType.C ? avgVec - strikes : strikes - avgVec;
                if (_isOption)
                      payoff = Vector.Max(new Vector<double>(0), payoff);

                var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                _results[resultIx] = payoff * _notional - new Vector<double>(OptionPremiumTotal);
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_avgDates.SelectMany(x => x.SelectMany(xx=>xx)));
            dates.AddDate(payDate);
            dates.AddDate(decisionDate);
        }

        private ForwardPriceEstimatorSpec _settleSpec;
        private List<List<ForwardPriceEstimatorSpec>> _avgSpecs = [];

        public List<ForwardPriceEstimatorSpec> GetRequiredEstimators(IAssetFxModel vanillaModel)
        {
            var oo = new List<ForwardPriceEstimatorSpec>();

            if (!string.IsNullOrEmpty(FixingId))
                Fixings = vanillaModel.GetFixingDictionary(FixingId);
            if (!string.IsNullOrEmpty(FixingIdDateShifted))
                FixingsDateShifted = vanillaModel.GetFixingDictionary(FixingIdDateShifted);

            for (var i = 0; i < _avgDates.Length; i++)
            {
                _avgDates[i] = new DateTime[offsets.Length][];
                var s = qps[i].FirstDayOfMonth();
                for (var o = 0; o < offsets.Length; o++)
                {
                    _avgDates[i][o] = [.. s.AddMonths(offsets[o]).BusinessDaysInPeriod(s.AddMonths(offsets[o]).LastDayOfMonth(), calendarProvider.GetCalendarSafe("GBP"))];
                }
            }

            foreach (var qp in _avgDates)
            {
                var sp = new List<ForwardPriceEstimatorSpec>();
                _avgSpecs.Add(sp);
                foreach (var ad in qp)
                {
                    var spec = new ForwardPriceEstimatorSpec
                    {
                        AssetId = assetName,
                        AverageDates = ad,
                        ValDate = decisionDate,
                        DateShifter = dateShifter
                    };
                    oo.Add(spec);
                    sp.Add(spec);
                }
            }

            AverageRegressors = new IForwardPriceEstimate[_avgSpecs.Count][];
            for(var i = 0; i < _avgSpecs.Count; i++)
            {
                AverageRegressors[i] = new IForwardPriceEstimate[offsets.Length];
            }

            return oo;
        }

        public double AverageResult => _results.Select(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec.Average();
        }).Average();

        public double[] ExerciseProbabilities { get; set; }

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
                        Pv = ar * model.FundingModel.Curves[discountCurve].GetDf(model.BuildDate,payDate),
                        Currency = ccy,
                        FlowType =  FlowType.FixedAmount,
                        SettleDate = payDate,
                        YearFraction = 1.0
                    },
                ]
            };
        }

        public CashFlowSchedule[] ExpectedFlowsByPath(IAssetFxModel model)
        {
            var df = model.FundingModel.Curves[discountCurve].GetDf(model.BuildDate, payDate);
            return ResultsByPath.Select(x => new CashFlowSchedule
            {
                Flows =
                [
                    new CashFlow
                    {
                        Fv = x,
                        Pv = x * df,
                        Currency = ccy,
                        FlowType =  FlowType.FixedAmount,
                        SettleDate = payDate,
                        YearFraction = 1.0
                    }
                ]
            }).ToArray();

        }
    }
}
