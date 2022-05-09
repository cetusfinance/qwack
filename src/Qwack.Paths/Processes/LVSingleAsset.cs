using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Math.Interpolation;
using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using Qwack.Transport.BasicTypes;
using static System.Math;

namespace Qwack.Paths.Processes
{
    public class LVSingleAsset : IPathProcess, IRequiresFinish
    {
        private readonly IVolSurface _surface;

        private readonly IATMVolSurface _adjSurface;
        private readonly double _correlation;

        private readonly DateTime _expiryDate;
        private readonly DateTime _startDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private int _factorIndex;
        private ITimeStepsFeature _timesteps;
        private readonly Func<double, double> _forwardCurve;
        private bool _isComplete;
        private Vector<double>[] _drifts;
        private IInterpolator1D[] _lvInterps;

        private readonly Vector<double> _two = new(2.0);

        private readonly bool _siegelInvert;

        public LVSingleAsset(IVolSurface volSurface, DateTime startDate, DateTime expiryDate, int nTimeSteps, Func<double, double> forwardCurve, string name, Dictionary<DateTime, double> pastFixings = null, IATMVolSurface fxAdjustSurface = null, double fxAssetCorrelation = 0.0)
        {
            _surface = volSurface;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps;
            _name = name;
            _pastFixings = pastFixings ?? (new Dictionary<DateTime, double>());
            _forwardCurve = forwardCurve;

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
            _drifts = new Vector<double>[_timesteps.TimeStepCount];
            _lvInterps = new IInterpolator1D[_timesteps.TimeStepCount - 1];

            var strikes = new double[_timesteps.TimeStepCount][];
            var atmVols = new double[_timesteps.TimeStepCount];
            for (var t = 0; t < strikes.Length; t++)
            {
                var fwd = _forwardCurve(_timesteps.Times[t]);
                atmVols[t] = _surface.GetVolForDeltaStrike(0.5, _timesteps.Times[t], fwd);

                if (_timesteps.Times[t] == 0)
                {
                    strikes[t] = new double[] { fwd };
                    continue;
                }
                else
                {
                    var nStrikes = 200;
                    var strikeStep = 1.0 / nStrikes;
                    strikes[t] = new double[nStrikes];
                    for (var k = 0; k < strikes[t].Length; k++)
                    {
                        var deltaK = -(strikeStep + strikeStep * k);
                        strikes[t][k] = Options.BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaK, 0, _timesteps.Times[t], atmVols[t]);
                    }
                }
            }

            var lvSurface = Options.LocalVol.ComputeLocalVarianceOnGridFromCalls(_surface, strikes, _timesteps.Times, _forwardCurve);

            for (var t = 0; t < _lvInterps.Length; t++)
            {
                _lvInterps[t] = InterpolatorFactory.GetInterpolator(strikes[t], lvSurface[t], t == 0 ? Interpolator1DType.DummyPoint : Interpolator1DType.LinearFlatExtrap);
            }

            var prevSpot = _forwardCurve(0);
            for (var t = 1; t < _drifts.Length; t++)
            {
                var fxAtmVol = _adjSurface == null ? 0.0 : _adjSurface.GetForwardATMVol(0, _timesteps.Times[t]);
                var driftAdj = _adjSurface == null ? 1.0 : Exp(atmVols[t] * fxAtmVol * _timesteps.Times[t] * _correlation);
                var spot = _forwardCurve(_timesteps.Times[t]) * driftAdj;

                _drifts[t] = new Vector<double>(Log(spot / prevSpot) / _timesteps.TimeSteps[t]);

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
                        var vols = new double[Vector<double>.Count];
                        for (var s = 0; s < vols.Length; s++)
                        {
                            vols[s] = Sqrt(Max(0, _lvInterps[step - 1].Interpolate(previousStep[s])));
                        }
                        var volVec = new Vector<double>(vols);
                        var bm = (_drifts[step] + volVec * volVec / _two) * dt + (volVec * _timesteps.TimeStepsSqrt[step] * W);
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
                        var vols = new double[Vector<double>.Count];
                        for (var s = 0; s < vols.Length; s++)
                        {
                            vols[s] = Sqrt(Max(0, _lvInterps[step - 1].Interpolate(previousStep[s])));
                        }
                        var volVec = new Vector<double>(vols);
                        var bm = (_drifts[step] - volVec * volVec / _two) * dt + (volVec * _timesteps.TimeStepsSqrt[step] * W);
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
            _timesteps.AddDates(_pastFixings.Keys.Where(x => x < _startDate));

            var stepSize = (_expiryDate - _startDate).TotalDays / _numberOfSteps;
            var simDates = new List<DateTime> { _startDate };
            for (var i = 0; i < _numberOfSteps; i++)
            {
                simDates.Add(_startDate.AddDays(i * stepSize).Date);
            }

            _timesteps.AddDates(simDates.Distinct());

        }
    }
}
