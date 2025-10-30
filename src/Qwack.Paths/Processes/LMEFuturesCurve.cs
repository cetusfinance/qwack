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
        private int[] _futureExpiryPoints;
        private int[] _futuresIndices;

        private double[] _totalDrift;
        private double[] _totalVar;
        private DateTime[] _futExpiryDates;

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

            _totalDrift = new double[_optionExpiries.Count];
            _totalVar = new double[_optionExpiries.Count];
            _futExpiryDates = new DateTime[_optionExpiries.Count];

            var initialSpot = _forwardCurve(_startSpotDate);
            for (var c = 0; c < _totalDrift.Length; c++)
            {
                var lastDay = _futuresExpiries[c].SubtractPeriod(Transport.BasicTypes.RollType.P, _calendarProvider.GetCalendar("GBP+USD"), 2.Bd());
                _futExpiryDates[c] = lastDay;

                var vol = _surface.GetVolForDeltaStrike(0.5, _futExpiryDates[c], 1.0);
                var fwd = _forwardCurve(_futuresExpiries[c]);

                var time = (lastDay - _startDate).TotalDays / 365.0;
                _totalVar[c] = System.Math.Pow(vol, 2) * time;
                _totalDrift[c] = System.Math.Log(fwd / initialSpot);
            }

            //vols...
            _vols = new double[_timesteps.TimeStepCount][];
            for (var t = 0; t < _vols.Length; t++)
            {
                _vols[t] = new double[_optionExpiries.Count];
                for (var c = 0; c < _vols[t].Length; c++)
                {
                    _vols[t][c] = _surface.GetVolForDeltaStrike(0.5, _futExpiryDates[c], 1.0);
                }
            }



            //initial state spot drifts
            _spotDrifts = new double[_timesteps.TimeStepCount];
            _spotVols = new double[_timesteps.TimeStepCount];

            var dates = collection.GetFeature<ITimeStepsFeature>();
            var fixings = new List<Vector<double>>();
            for (var d = 0; d < dates.Dates.Length; d++)
            {
                var date = dates.Dates[d];
                if (date > _startDate) break;
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
            var sdIx = _timesteps.GetDateIndex(_startDate);
            var firstTime = _timesteps.Times[sdIx];
            for (var t = _fixings.Length + 1; t < _spotDrifts.Length; t++)
            {
                var d = _timesteps.Dates[t];
                var dPrev = _timesteps.Dates[t - 1];
                var spotDate = d.Date.SpotDate(2.Bd(), _calendarProvider.GetCalendar("GBP"), _calendarProvider.GetCalendar("USD"));
                var d3w = prevSpotDate.NextThirdWednesday();
                var d3m = d3w.SubtractPeriod(Transport.BasicTypes.RollType.P, _calendarProvider.GetCalendar("GBP+USD"), 2.Bd());
                var td3w = (d3m - dPrev).TotalDays;
                var time = _timesteps.Times[t] - firstTime;
                var prevTime = _timesteps.Times[t - 1] - firstTime;
                var atmVol = _surface.GetForwardATMVol(0, time);
                var prevVol =  _surface.GetForwardATMVol(0, prevTime);
                var spotPrice = _forwardCurve(spotDate);
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
                    //foreach (var kv in _pastFixings.Where(x => x.Key < _startDate))
                    //{
                    //    steps[c] = new Vector<double>(kv.Value);
                    //    c++;
                    //}
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


                previousStep = new Vector<double>(_forwardCurve(_startSpotDate));
                steps = block.GetStepsForFactor(path, _mainFactorIndex);
                _fixings.AsSpan().CopyTo(steps);
                steps[c] = previousStep;

                //var bb = new TermStructureBridge(_timesteps.Times, _spotDrifts, _spotVols);

                //fill in the spot path assuming 1:1 correlation to front month future
                var fut = 0;
                for (var step = c + 1; step < block.NumberOfSteps; step++)
                {
                    var nextExp = _futureExpiryPoints[fut];
                    var futIx = _futuresIndices[fut];
                   
                    var ta = _timesteps.Times[step - 1];
                    var tb = _timesteps.Times[nextExp];

                    //var vvb = bb.SigmaSqIntegral(0, tb);

                    for (var ti = step; ti < nextExp; ti++)
                    {
                        var t = _timesteps.Times[ti];
                        var tToNextExp = new Vector<double>(tb - t);
                        var futPrice = block.GetStepsForFactor(path, futIx)[ti];
                        var futReturn = (futPrice / block.GetStepsForFactor(path, futIx)[ti-1]);
                        previousStep *= futReturn;
                        var lastSpotPrice = previousStep;
                        var totalDrift = (futPrice / lastSpotPrice).Log() / tToNextExp;

                        var W = Vector<double>.Zero; // steps[ti];
                        var dt = new Vector<double>(_timesteps.TimeSteps[ti]);
                        //var bm = (totalDrift - new Vector<double>(_spotVols[ti] * _spotVols[ti] / 2.0)) * dt + (_spotVols[ti] * _timesteps.TimeStepsSqrt[ti] * W);
                        var bm = totalDrift * dt;
                        previousStep *= bm.Exp();
                        steps[ti] = previousStep;

                        //ya = y;
                        //va += A;
                        //ta = t;
                    }

                    step = nextExp;
                    var futPriceA = block.GetStepsForFactor(path, futIx)[step];
                    steps[step] = futPriceA;
                    previousStep = futPriceA;
                    fut++;
                }

                //fill in spot using a series of brownian bridges
                //previousStep = new Vector<double>(_forwardCurve(_startSpotDate));
                //steps = block.GetStepsForFactor(path, _mainFactorIndex);
                //_fixings.AsSpan().CopyTo(steps);
                //steps[c] = previousStep;

                //var bb = new TermStructureBridge(_timesteps.Times, _spotDrifts, _spotVols);

                //var fut = 0;
                //for (var step = c + 1; step < block.NumberOfSteps; step++)
                //{
                //    var nextExp = _futureExpiryPoints[fut];
                //    var futIx = _futuresIndices[fut];
                //    var futPrice = block.GetStepsForFactor(path, futIx)[nextExp];
                //    var ta = _timesteps.Times[step - 1];
                //    var tb = _timesteps.Times[nextExp];
                //    var xa = previousStep.Log();
                //    var xb = futPrice.Log();
                //    var Da = fut == 0 ? 0 : (_totalDrift[fut - 1] - 0.5 * _totalVar[fut-1]); //bb.DriftIntegral2(ta);
                //    var Db = _totalDrift[fut] - 0.5 * _totalVar[fut]; //bb.DriftIntegral2(tb);
                //    var ya = xa - new Vector<double>(Da);
                //    var yb = xb - new Vector<double>(Db);

                //    var va = fut==0 ? 0 : _totalVar[fut - 1];
                //    var vb = _totalVar[fut];
                //    var V_total = vb - va;

                //    //var vvb = bb.SigmaSqIntegral(0, tb);

                //    for(var ti=step; ti < nextExp; ti++)
                //    {
                //        var t = _timesteps.Times[ti];
                //        var lambda = (t - ta) / (tb - ta);

                //        var A = (vb - va) * lambda;

                //        var meanY = ya + lambda * (yb - ya);
                //        var varY = A * (1 - lambda);

                //        var W = steps[ti];
                //        var y = meanY + System.Math.Sqrt(varY) * W;

                //        var D = bb.DriftIntegral2(t);
                //        var x = y + new Vector<double>(D);
                //        var S = x.ExpD(1e-14);
                //        steps[ti] = S;

                //        //ya = y;
                //        //va += A;
                //        //ta = t;
                //    }

                //    step = nextExp;
                //    steps[step] = futPrice;
                //    previousStep = futPrice;
                //    fut++;
                //}
            }
        }
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
