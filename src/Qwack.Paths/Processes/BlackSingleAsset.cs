using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Models;
using Qwack.Math.Extensions;
using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using Qwack.Serialization;
using static System.Math;

namespace Qwack.Paths.Processes
{
    public class BlackSingleAsset : IPathProcess, IRequiresFinish
    {
        private readonly IATMVolSurface _surface;

        private readonly IATMVolSurface _adjSurface;
        private readonly double _correlation;

        private readonly DateTime _expiryDate;
        private readonly DateTime _startDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private int _factorIndex;
        private ITimeStepsFeature _timesteps;
        [SkipSerialization]
        private readonly Func<double, double> _forwardCurve;
        [SkipSerialization]
        private readonly Func<DateTime, double> _forwardCurve2;
        private bool _isComplete;
        private double[] _drifts;
        private double[] _vols;
        private Vector<double>[] _fixings;

        private readonly bool _siegelInvert;

        public BlackSingleAsset(IATMVolSurface volSurface, DateTime startDate, DateTime expiryDate, int nTimeSteps, Func<double, double> forwardCurve, string name, Dictionary<DateTime, double> pastFixings = null, IATMVolSurface fxAdjustSurface = null, double fxAssetCorrelation = 0.0)
        {
            _surface = volSurface;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps;
            _name = name;
            _forwardCurve = forwardCurve;
            _pastFixings = pastFixings ?? (new Dictionary<DateTime, double>());

            _adjSurface = fxAdjustSurface;
            _correlation = fxAssetCorrelation;

            if (volSurface is InverseFxSurface || (_adjSurface != null && _adjSurface?.AssetId == volSurface.AssetId))
                _siegelInvert = true;

            if (_adjSurface != null && _adjSurface?.AssetId == volSurface.AssetId)
                _adjSurface = null;
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            if (!_timesteps.IsComplete)
            {
                return;
            }

            //drifts and vols...
            _drifts = new double[_timesteps.TimeStepCount];
            _vols = new double[_timesteps.TimeStepCount];

            

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

            var prevSpot = _forwardCurve(0);
            var firstTime = _timesteps.Times[_fixings.Length];
            for (var t = _fixings.Length  + 1; t < _drifts.Length; t++)
            {
                var time = _timesteps.Times[t] - firstTime;
                var prevTime = _timesteps.Times[t - 1] - firstTime;
                var atmVol = _surface.GetForwardATMVol(0, time);
                var fxAtmVol = _adjSurface == null ? 0.0 : _adjSurface.GetForwardATMVol(0, time);
                var driftAdj = _adjSurface == null ? 1.0 : Exp(atmVol * fxAtmVol * time * _correlation);
                var spot = _forwardCurve(time) * driftAdj;
                var varStart = Pow(_surface.GetForwardATMVol(0, prevTime), 2) * prevTime;
                var varEnd = Pow(atmVol, 2) * time;
                var fwdVariance = Max(0, varEnd - varStart);
                _vols[t] = Sqrt(fwdVariance / _timesteps.TimeSteps[t]);
                _drifts[t] = Log(spot / prevSpot) / _timesteps.TimeSteps[t];

                prevSpot = spot;
            }

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var previousStep = new Vector<double>(_forwardCurve(0));
                var steps = block.GetStepsForFactor(path, _factorIndex);
                var c = _fixings.Length;
                _fixings.AsSpan().CopyTo(steps);
                steps[c] = previousStep;

                if (_siegelInvert)
                {
                    for (var step = c + 1; step < block.NumberOfSteps; step++)
                    {
                        var W = steps[step];
                        var dt = new Vector<double>(_timesteps.TimeSteps[step]);
                        var bm = (_drifts[step] + _vols[step] * _vols[step] / 2.0) * dt + (_vols[step] * _timesteps.TimeStepsSqrt[step] * W);
                        previousStep *= bm.Exp();
                        steps[step] = previousStep;
                    }
                }
                else
                {
                    for (var step = c + 1; step < block.NumberOfSteps; step++)
                    {
                        var W = steps[step];
                        var dt = new Vector<double>(_timesteps.TimeSteps[step]);
                        var bm = (_drifts[step] - _vols[step] * _vols[step] / 2.0) * dt + (_vols[step] * _timesteps.TimeStepsSqrt[step] * W);
                        previousStep *= bm.Exp();
                        steps[step] = previousStep;
                    }
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _factorIndex = mappingFeature.AddDimension(_name);

            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            _timesteps.AddDate(_startDate);
            //_timesteps.AddDates(_pastFixings.Keys.Where(x => x < _startDate));

            var stepSize = (_expiryDate - _startDate).TotalDays / _numberOfSteps;
            var simDates = new List<DateTime>();
            for (var i = 0; i < _numberOfSteps; i++)
            {
                simDates.Add(_startDate.AddDays(i * stepSize).Date);
            }

            _timesteps.AddDates(simDates.Distinct());
        }
    }
}

