using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Math.Extensions;
using Qwack.Options;
using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using Qwack.Paths.Payoffs;
using Qwack.Utils.Parallel;

namespace Qwack.Paths.Processes
{
    public class LMEFuturesCurve : IPathProcess, IRequiresFinish
    {
        private readonly Vector<double> _two = new(2.0);
        private readonly IATMVolSurface _surface;
        private readonly IFutureSettingsProvider _futureSettingsProvider;
        private readonly DateTime _expiryDate;
        private readonly DateTime _startDate;
        private readonly DateTime _startSpotDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly ICalendarProvider _calendarProvider;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private readonly List<string> _codes = [];
        private List<int> _frontMonthFactors;
        private readonly List<DateTime> _futuresExpiries = [];
        private readonly List<DateTime> _optionExpiries = [];
        private int[] _factorIndices;
        private int _mainFactorIndex;
        private ITimeStepsFeature _timesteps;
        private readonly Func<DateTime, double> _forwardCurve;
        private bool _isComplete;

        private Vector<double>[] _fixings;

        //drifts are zero for individual futures but we have drifts for spot
        private double[][] _vols;
        private double[] _spotVols;
        private double[] _spotDrifts;
        private Vector<double>[] _tToNextFuture;
        private Vector<double>[] _totalDriftToNextFuture;
        private int[] _futureExpiryPoints;
        private int[] _futuresIndices;


        public LMEFuturesCurve(IATMVolSurface volSurface, DateTime startDate, DateTime expiryDate, int nTimeSteps, Func<DateTime, double> forwardCurve, string name, ICalendarProvider calendarProvider, TimeDependentLmeCorrelations correlations, Dictionary<DateTime, double> pastFixings = null)        {
            _surface = volSurface;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps;
            _name = name;
            _calendarProvider = calendarProvider;
            _forwardCurve = forwardCurve;
            _pastFixings = pastFixings ?? ([]);

            if (startDate > expiryDate)
                throw new Exception("Start date must be before expiry date");

            var promptDict = new Dictionary<string, DateTime?>
            {
                { name, startDate }
            };

            var spotDate = startDate.Date.SpotDate(2.Bd(), calendarProvider.GetCalendar("GBP"), calendarProvider.GetCalendar("USD"));
            var endSpotDate = expiryDate.Date.SpotDate(2.Bd(), calendarProvider.GetCalendar("GBP"), calendarProvider.GetCalendar("USD"));

            var fmExpiry = spotDate.NextThirdWednesday();
            
            _futuresExpiries.Add(fmExpiry);
            _optionExpiries.Add(fmExpiry.SubtractWeekDays(10));
            _codes.Add($"{name}~{fmExpiry:yyyyMMdd}");
            promptDict[$"{name}~{fmExpiry:yyyyMMdd}"] = fmExpiry;
            while (fmExpiry < endSpotDate)
            {
                fmExpiry = fmExpiry.AddMonths(1).ThirdWednesday();
                _futuresExpiries.Add(fmExpiry);
                _optionExpiries.Add(fmExpiry.SubtractWeekDays(10));
                _codes.Add($"{name}~{fmExpiry:yyyyMMdd}");
                promptDict[$"{name}~{fmExpiry:yyyyMMdd}"] = fmExpiry;
            }

            //fmExpiry = fmExpiry.AddMonths(1).ThirdWednesday();
            //_futuresExpiries.Add(fmExpiry);
            //_optionExpiries.Add(fmExpiry.SubtractWeekDays(10));
            //_codes.Add($"{name}~{fmExpiry:yyyyMMdd}");
            //promptDict[$"{name}~{fmExpiry:yyyyMMdd}"] = fmExpiry;

            _startSpotDate = spotDate;

            correlations.Initialize(promptDict);
        }
        public bool IsComplete => _isComplete;
        public void Finish(IFeatureCollection collection)
        {
            if (!_timesteps.IsComplete)
            {
                return;
            }
            //vols...
            _vols = new double[_timesteps.TimeStepCount][];
            for (var t = 0; t < _vols.Length; t++)
            {
                _vols[t] = new double[_optionExpiries.Count];
                for (var c = 0; c < _vols[t].Length; c++)
                {
                    _vols[t][c] = _surface.GetVolForDeltaStrike(0.5, _optionExpiries[c], 1.0);
                }
            }

            //initial state spot drifts
            _spotDrifts = new double[_timesteps.TimeStepCount];
            _spotVols = new double[_timesteps.TimeStepCount];
            _tToNextFuture = new Vector<double>[_timesteps.TimeStepCount];
            _totalDriftToNextFuture = new Vector<double>[_timesteps.TimeStepCount];


            var dates = collection.GetFeature<ITimeStepsFeature>();
            var fixings = new List<Vector<double>>();
            for (var d = 0; d < dates.Dates.Length; d++)
            {
                var date = dates.Dates[d];
                if (date >= _startDate) break;
                try
                {
                    var vect = new Vector<double>(_pastFixings[date]);
                    fixings.Add(vect);
                }
                catch (Exception e)
                {
                }

            }
            _fixings = [.. fixings];

            var prevSpotDate = _startSpotDate;
            var prevSpot = _forwardCurve(prevSpotDate);
            var firstTime = _timesteps.Times[_fixings.Length];
            for (var t = _fixings.Length + 1; t < _spotDrifts.Length; t++)
            {
                var d = _timesteps.Dates[t];
                var dPrev = _timesteps.Dates[t - 1];
                var spotDate = d.Date.SpotDate(2.Bd(), _calendarProvider.GetCalendar("GBP"), _calendarProvider.GetCalendar("USD"));
                var d3w = prevSpotDate.NextThirdWednesday();
                var d3m = d3w.SubtractPeriod(Transport.BasicTypes.RollType.P, _calendarProvider.GetCalendar("GBP+USD"), 2.Bd());
                var td3w = (d3m - dPrev).TotalDays;
                _tToNextFuture[t] = new Vector<double>(td3w / 365.0);
                var time = _timesteps.Times[t] - firstTime;
                var prevTime = _timesteps.Times[t - 1] - firstTime;
                var atmVol = _surface.GetForwardATMVol(0, time);
                var prevVol =  _surface.GetForwardATMVol(0, prevTime);
                var spotPrice = _forwardCurve(spotDate);
                var nextFuturePrice = _forwardCurve(d3w);
                _totalDriftToNextFuture[t] = new Vector<double>(nextFuturePrice / prevSpot);
                var varStart = System.Math.Pow(prevVol, 2) * prevTime;
                var varEnd = System.Math.Pow(atmVol, 2) * time;
                var x0 = varEnd - varStart;
                if (x0 < 0)
                {
                    Debug.Assert(true);
                }
                var fwdVariance = System.Math.Max(0, varEnd - varStart);
                _spotVols[t] = System.Math.Sqrt(fwdVariance / _timesteps.TimeSteps[t]);
                _spotDrifts[t] = System.Math.Log(spotPrice / prevSpot) / _timesteps.TimeSteps[t];

                prevSpot = spotPrice;
                prevSpotDate = spotDate;
            }

            var mappingFeature = collection.GetFeature<IPathMappingFeature>();
            var codesForDate = _timesteps.Dates.Select(d => d.SpotDate(2.Bd(), _calendarProvider.GetCalendar("GBP"), _calendarProvider.GetCalendar("USD")).NextThirdWednesday());
            _frontMonthFactors = [.. codesForDate.Select(c => mappingFeature.GetDimension($"{_name}~{c:yyyyMMdd}"))];
            _futureExpiryPoints = [.. _futuresExpiries.Select(e=>dates.GetDateIndex(e.SubtractPeriod(Transport.BasicTypes.RollType.P, _calendarProvider.GetCalendar("GBP+USD"),2.Bd())))];
            _futuresIndices = [.. _codes.Select(mappingFeature.GetDimension)];
            _isComplete = true;
        }
        public void Process(IPathBlock block) //=> ParallelUtils.Instance.For(0, block.NumberOfPaths, Vector<double>.Count, path =>
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var c = _fixings.Length;
                Vector<double> previousStep;
                Span<Vector<double>> steps;
                for (var f = 0; f < _factorIndices.Length; f++)
                //ParallelUtils.Instance.For(0,_factorIndices.Length,1,f=>
                {
                    previousStep = new Vector<double>(_forwardCurve(_futuresExpiries[f]));
                    steps = block.GetStepsForFactor(path, _factorIndices[f]);
                    foreach (var kv in _pastFixings.Where(x => x.Key < _startDate))
                    {
                        steps[c] = new Vector<double>(kv.Value);
                        c++;
                    }
                    steps[c] = previousStep;
                    for (var step = c + 1; step < block.NumberOfSteps; step++)
                    {
                        var W = steps[step];
                        var dt = new Vector<double>(_timesteps.TimeSteps[step]);
                        var bm = (-_vols[step][f] * _vols[step][f] / 2.0) * dt + (_vols[step][f] * _timesteps.TimeStepsSqrt[step] * W);
                        previousStep *= bm.Exp();
                        steps[step] = previousStep;
                    }
                }//).Wait();


                //fill in spot using a series of brownian bridges
                previousStep = new Vector<double>(_forwardCurve(_startSpotDate));
                steps = block.GetStepsForFactor(path, _mainFactorIndex);
                _fixings.AsSpan().CopyTo(steps);
                steps[c] = previousStep;

                var bb = new TermStructureBridge(_timesteps.Times, _spotDrifts, _spotVols);

                var fut = 0;
                for (var step = c + 1; step < block.NumberOfSteps; step++)
                {
                    var nextExp = _futureExpiryPoints[fut];
                    var futIx = _futuresIndices[fut];
                    var futPrice = block.GetStepsForFactor(path, futIx)[nextExp];
                    var ta = _timesteps.Times[step - 1];
                    var tb = _timesteps.Times[nextExp];
                    var xa = previousStep.Log();
                    var xb = futPrice.Log();
                    var Da = bb.DriftIntegral2(ta);
                    var Db = bb.DriftIntegral2(tb);
                    var ya = xa - new Vector<double>(Da);
                    var yb = xb - new Vector<double>(Db);

                    var V_total = bb.VarianceIntegral(ta, tb);

                    for(var ti=step; ti < nextExp; ti++)
                    {
                        var t = _timesteps.Times[ti];
                        //var A = bb.VarianceIntegral(ta, t);
                        //var lambda = V_total==0 ? 0 : A / V_total;
                        var lambda = (t - ta) / (tb - ta);

                        var meanY = ya + lambda * (yb - ya);
                        var varY = V_total * (1 - lambda) * lambda;

                        var W = steps[ti];
                        var y = meanY + System.Math.Sqrt(varY) * W;
                        //var y = (1 - lambda) * ya + lambda * yb + new Vector<double>(System.Math.Sqrt(A*lambda*(1-lambda)*t))*W;

                        var D = bb.DriftIntegral2(t);
                        var x = y + new Vector<double>(D);
                        var S = x.ExpD(1e-14);
                        steps[ti] = S;
                    }

                    step = nextExp;
                    steps[step] = futPrice;
                    previousStep = futPrice;
                    fut++;
                }

                //previousStep = new Vector<double>(_forwardCurve(_startSpotDate));
                //steps = block.GetStepsForFactor(path, _mainFactorIndex);
                //_fixings.AsSpan().CopyTo(steps);
                //steps[c] = previousStep;

                //for (var step = c + 1; step < block.NumberOfSteps; step++)
                //{
                //    var fmPath = block.GetStepsForFactor(path, _frontMonthFactors[step - 1]);
                //    var fmPrice = fmPath[step];
                //    var tToNextFuture = _tToNextFuture[step];

                //    Vector<double> drift;
                //    Vector<double> vol;
                //    var dt = new Vector<double>(_timesteps.TimeSteps[step]);
                //    if (tToNextFuture == Vector<double>.Zero)
                //    {
                //        var currentTotalDrift = fmPrice / previousStep;
                //        var excessDrift = currentTotalDrift.Log() / dt;
                //        drift = excessDrift;
                //        vol = new Vector<double>(0);
                //    }
                //    else
                //    {
                //        vol = new Vector<double>(_spotVols[step]);

                //        var totalDriftToNextFuture = _totalDriftToNextFuture[step];
                //        var currentTotalDrift = fmPrice / previousStep;
                //        //var d2 = currentTotalDrift.Log() / tToNextFuture;
                //        var excessFwd = currentTotalDrift / totalDriftToNextFuture;
                //        var excessDrift = excessFwd.Log() / tToNextFuture;
                //        drift = new Vector<double>(_spotDrifts[step]) + excessDrift;
                //        //drift = d2 - vol * vol / _two;

                //    }

                //    var W = steps[step];
                //    var bm = (drift - vol * vol / _two) * dt + (vol * _timesteps.TimeStepsSqrt[step] * W);
                //    previousStep *= bm.Exp(64);
                //    steps[step] = previousStep;

                //    for (var i = 0; i < Vector<double>.Count; i++)
                //    {
                //        if (System.Math.Abs(previousStep[i]) > 1e6 || double.IsNaN(previousStep[i]) || double.IsInfinity(previousStep[i]))
                //        {
                //            Debug.Assert(true);
                //        }
                //    }

                //}
            }
        }//).Wait();
        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            _factorIndices = new int[_codes.Count];
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _mainFactorIndex = mappingFeature.AddDimension(_name);

            for (var c = 0; c < _factorIndices.Length; c++)
            {
                _factorIndices[c] = mappingFeature.AddDimension(_codes[c]);
            }
     

            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            var simDates = new List<DateTime>(_pastFixings.Keys.Where(x => x < _startDate))
            {
                _startDate,
            };

            //var d = _startDate.NextThirdWednesday(true);
            //while (d < _expiryDate)
            //{
            //    simDates.Add(d.Date.SubtractPeriod(Transport.BasicTypes.RollType.P, _calendarProvider.GetCalendar("GBP+USD"), 2.Bd()));
            //    d = d.NextThirdWednesday(true);
            //}
            //simDates.Add(d.Date.SubtractPeriod(Transport.BasicTypes.RollType.P, _calendarProvider.GetCalendar("GBP+USD"), 2.Bd()));

            simDates.AddRange(_futuresExpiries.Select(e => e.SubtractPeriod(Transport.BasicTypes.RollType.P, _calendarProvider.GetCalendar("GBP+USD"), 2.Bd())));

            var stepSize = (_expiryDate - _startDate).TotalDays / _numberOfSteps;

            for (var i = 0; i < _numberOfSteps - 1; i++)
            {
                simDates.Add(_startDate.AddDays(i * stepSize).Date);
            }
            simDates = simDates.Distinct().ToList();
            _timesteps.AddDates(simDates);
        }
    }
}
