using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Numerics;
using Qwack.Math.Extensions;
using System.Linq;
using Qwack.Core.Models;
using Qwack.Serialization;

namespace Qwack.Paths.Processes
{
    public class BlackSingleAsset : IPathProcess, IRequiresFinish
    {
        private IATMVolSurface _surface;
        private readonly DateTime _expiryDate;
        private DateTime _startDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private int _factorIndex;
        private ITimeStepsFeature _timesteps;
        [SkipSerialization]
        private readonly Func<double, double> _forwardCurve;
        private bool _isComplete;
        private double[] _drifts;
        private double[] _vols;


        public BlackSingleAsset(IATMVolSurface volSurface, DateTime startDate, DateTime expiryDate, int nTimeSteps, Func<double, double> forwardCurve, string name, Dictionary<DateTime, double> pastFixings = null)
        {
            _surface = volSurface;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps;
            _name = name;
            _forwardCurve = forwardCurve;
            _pastFixings = pastFixings ?? (new Dictionary<DateTime, double>());
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

            var prevSpot = _forwardCurve(0);
            for (var t = 1; t < _drifts.Length; t++)
            {
                var spot = _forwardCurve(_timesteps.Times[t]);
                var varStart = System.Math.Pow(_surface.GetForwardATMVol(0, _timesteps.Times[t - 1]), 2) * _timesteps.Times[t - 1];
                var varEnd = System.Math.Pow(_surface.GetForwardATMVol(0, _timesteps.Times[t]), 2) * _timesteps.Times[t];
                var fwdVariance = (varEnd - varStart);
                _vols[t] = System.Math.Sqrt(fwdVariance / _timesteps.TimeSteps[t]);
                _drifts[t] = System.Math.Log(spot / prevSpot) / _timesteps.TimeSteps[t];

                prevSpot = spot;
            }
            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                //detect left over paths which dont dfit into the vector stride

                var previousStep = new Vector<double>(_forwardCurve(0));
                var steps = block.GetStepsForFactor(path, _factorIndex);
                var c = 0;
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
                    var bm = (_drifts[step] - _vols[step] * _vols[step] / 2.0) * dt + (_vols[step] * _timesteps.TimeStepsSqrt[step] * W);
                    previousStep *= bm.Exp();
                    steps[step] = previousStep;
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _factorIndex = mappingFeature.AddDimension(_name);

            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            _timesteps.AddDates(_pastFixings.Keys.Where(x => x < _startDate));

            var stepSize = (_expiryDate - _startDate).TotalDays / _numberOfSteps;
            var simDates = new List<DateTime>();
            for (var i = 0; i < _numberOfSteps - 1; i++)
            {
                simDates.Add(_startDate.AddDays(i * stepSize).Date);
            }

            _timesteps.AddDates(simDates.Distinct());

        }
    }
}

