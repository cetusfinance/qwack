using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Underlyings;
using System.Numerics;

namespace Qwack.Paths.Processes
{
    public class BlackSingleAsset : IPathProcess, IRequiresFinish
    {
        private IATMVolSurface _surface;
        private DateTime _expiryDate;
        private DateTime _startDate;
        private int _numberOfSteps;
        private string _name;
        private int _factorIndex;
        private ITimeStepsFeature _timesteps;
        private Func<double, double> _forwardCurve;
        private bool _isComplete;
        private double[] _drifts;
        private double[] _vols;


        public BlackSingleAsset(IATMVolSurface volSurface, DateTime startDate, DateTime expiryDate, int nTimeSteps, Func<double, double> forwardCurve, string name)
        {
            _surface = volSurface;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps;
            _name = name;
            _forwardCurve = forwardCurve;
        }

        public bool IsComplete => _isComplete;

        public void Finish(FeatureCollection collection)
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
                //_vols[t] *= _timesteps.TimeStepsSqrt[t];
                _drifts[t] = System.Math.Log(spot / prevSpot) / _timesteps.TimeSteps[t];
                prevSpot = spot;
            }
            _isComplete = true;
        }

        public void Process(PathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                //This should be set to the spot price here
                var previousStep = new Vector<double>(_forwardCurve(0));
                var steps = block.GetStepsForFactor(path, _factorIndex);
                steps[0] = previousStep;
                for (var step = 1; step < block.NumberOfSteps; step++)
                {
                    var W = steps[step];// * _timesteps.TimeStepsSqrt[step];
                    var nextStep = new double[Vector<double>.Count];
                    for (var i = 0; i < nextStep.Length; i++)
                    {
                        var bm = (_drifts[step] - _vols[step] * _vols[step] / 2.0) * _timesteps.TimeSteps[step] + (_vols[step] * _timesteps.TimeStepsSqrt[step] * W[i]);
                        nextStep[i] = previousStep[i] * System.Math.Exp(bm);
                    }
                    previousStep = new Vector<double>(nextStep);
                    steps[step] = previousStep;
                }
            }
        }

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _factorIndex = mappingFeature.AddDimension(_name);

            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            var stepSize = (_expiryDate - _startDate).TotalDays / _numberOfSteps;
            for (var i = 0; i < _numberOfSteps - 1; i++)
            {
                _timesteps.AddDate(_startDate.AddDays(i * stepSize));
            }
            _timesteps.AddDate(_expiryDate);

        }
    }
}
