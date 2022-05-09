using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using Qwack.Serialization;
using static System.Math;

namespace Qwack.Paths.Processes
{
    public class TurboSkewSingleAsset : IPathProcess, IRequiresFinish
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
        private bool _isComplete;
        private double[] _drifts;
        private double[] _vols;
        private double[] _spotDrifts;
        private double[] _spotVols;
        private double[] _spotTimesSqrt;
        private double _spot0;

        private IInterpolator1D[] _invCdfs;

        private readonly bool _siegelInvert;

        public TurboSkewSingleAsset(IATMVolSurface volSurface, DateTime startDate, DateTime expiryDate, int nTimeSteps, Func<double, double> forwardCurve, string name, Dictionary<DateTime, double> pastFixings = null, IATMVolSurface fxAdjustSurface = null, double fxAssetCorrelation = 0.0)
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
            _spotDrifts = new double[_timesteps.TimeStepCount];
            _spotVols = new double[_timesteps.TimeStepCount];
            _invCdfs = new IInterpolator1D[_timesteps.TimeStepCount];

            _spot0 = _forwardCurve(0);
            var prevSpot = _spot0;
            for (var t = 1; t < _drifts.Length; t++)
            {
                var atmVol = _surface.GetForwardATMVol(0, _timesteps.Times[t]);
                var fxAtmVol = _adjSurface == null ? 0.0 : _adjSurface.GetForwardATMVol(0, _timesteps.Times[t]);
                var driftAdj = _adjSurface == null ? 1.0 : Exp(atmVol * fxAtmVol * _timesteps.Times[t] * _correlation);
                var spot = _forwardCurve(_timesteps.Times[t]) * driftAdj;
                var varStart = Pow(_surface.GetForwardATMVol(0, _timesteps.Times[t - 1]), 2) * _timesteps.Times[t - 1];
                var varEnd = Pow(atmVol, 2) * _timesteps.Times[t];
                var fwdVariance = Max(0, varEnd - varStart);
                _vols[t] = Sqrt(fwdVariance / _timesteps.TimeSteps[t]);
                _drifts[t] = Log(spot / prevSpot) / _timesteps.TimeSteps[t];
                _invCdfs[t] = _surface.GenerateCDF2(500, _timesteps.Dates[t], spot, true, driftAdj);

                _spotVols[t] = atmVol;
                _spotDrifts[t] = Log(spot / _spot0) / _timesteps.Times[t];

                prevSpot = spot;
            }

            _spotTimesSqrt = _timesteps.Times.Select(x => Sqrt(x)).ToArray();

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var previousStep = new Vector<double>(_forwardCurve(0));
                var steps = block.GetStepsForFactor(path, _factorIndex);
                var c = 0;
                foreach (var kv in _pastFixings.Where(x => x.Key < _startDate))
                {
                    steps[c] = new Vector<double>(kv.Value);
                    c++;
                }
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

                    //transform
                    for (var step = c + 1; step < block.NumberOfSteps; step++)
                    {
                        var ws = new double[Vector<double>.Count];
                        for (var v = 0; v < ws.Length; v++)
                        {
                            var t1 = Log(steps[step][v] / _spot0);
                            var t2 = (_spotDrifts[step] + _spotVols[step] * _spotVols[step] / 2.0) * _timesteps.Times[step];
                            t1 -= t2;
                            t1 /= _spotVols[step] * _spotTimesSqrt[step];
                            t1 = Statistics.NormSDist(t1);
                            ws[v] = _invCdfs[step].Interpolate(t1);
                        }
                        steps[step] = new Vector<double>(ws);
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

                    //transform
                    for (var step = c + 1; step < block.NumberOfSteps; step++)
                    {
                        var ws = new double[Vector<double>.Count];
                        for (var v = 0; v < ws.Length; v++)
                        {
                            var t1 = Log(steps[step][v] / _spot0);
                            var t2 = (_spotDrifts[step] - _spotVols[step] * _spotVols[step] / 2.0) * _timesteps.Times[step];
                            t1 -= t2;
                            t1 /= _spotVols[step] * _spotTimesSqrt[step];
                            t1 = Statistics.NormSDist(t1);
                            ws[v] = _invCdfs[step].Interpolate(t1);
                        }
                        steps[step] = new Vector<double>(ws);
                    }
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
            for (var i = 0; i < _numberOfSteps; i++)
            {
                simDates.Add(_startDate.AddDays(i * stepSize).Date);
            }

            _timesteps.AddDates(simDates.Distinct());
        }
    }
}

