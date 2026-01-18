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
        private Vector<double>[] _fixings;

        private readonly Vector<double> _two = new(2.0);

        private readonly bool _siegelInvert;

        public LVSingleAsset(IVolSurface volSurface, DateTime startDate, DateTime expiryDate, int nTimeSteps, Func<double, double> forwardCurve, string name, Dictionary<DateTime, double> pastFixings = null, IATMVolSurface fxAdjustSurface = null, double fxAssetCorrelation = 0.0)
        {
            _surface = volSurface;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps == 0 ? 100 : nTimeSteps;
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

            //fixings first
            var dates = collection.GetFeature<ITimeStepsFeature>();
            var fixings = new List<Vector<double>>();
            var lastFixing = 0.0;
            for (var d = 0; d < dates.Dates.Length; d++)
            {
                var date = dates.Dates[d];
                if (date > _startDate) break;

                if (!_pastFixings.ContainsKey(date.Date))
                {
                    var vect = new Vector<double>(lastFixing);
                    fixings.Add(vect);
                }
                else
                {
                    try
                    {
                        lastFixing = _pastFixings[date.Date];
                        var vect = new Vector<double>(lastFixing);
                        fixings.Add(vect);
                    }
                    catch (Exception e)
                    {
                    }
                }

            }
            _fixings = [.. fixings];
            var firstTime = _timesteps.Times[_fixings.Length];

            //drifts and vols...
            _drifts = new Vector<double>[_timesteps.TimeStepCount];
            _lvInterps = new IInterpolator1D[_timesteps.TimeStepCount - 1];

            var strikes = new double[_timesteps.TimeStepCount][];
            var atmVols = new double[_timesteps.TimeStepCount];
            var adjTimes = new double[_timesteps.TimeStepCount];
            for (var t = _fixings.Length; t < strikes.Length; t++)
            {
                var time = _timesteps.Times[t] - firstTime;
                adjTimes[t] = time;
                var fwd = _forwardCurve(time);
                atmVols[t] = _surface.GetVolForDeltaStrike(0.5, time, fwd);

                if (time == 0)
                {
                    strikes[t] = new double[] { fwd };
                    continue;
                }
                else
                {
                    var nStrikes = 200;
                    var strikeStep = 1.0 / nStrikes;
                    strikes[t] = new double[nStrikes-1];
                    for (var k = 0; k < strikes[t].Length; k++)
                    {
                        var deltaK = -(strikeStep + strikeStep * k);
                        strikes[t][k] = Options.BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaK, 0, time, atmVols[t]);
                    }
                }
            }

            var lvSurface = Options.LocalVol.ComputeLocalVarianceOnGridFromCalls2(_surface, strikes, adjTimes, _forwardCurve, _fixings.Length);

            for (var t = _fixings.Length; t < _lvInterps.Length; t++)
            {
                _lvInterps[t] = InterpolatorFactory.GetInterpolator(strikes[t], lvSurface[t], t == _fixings.Length ? Interpolator1DType.DummyPoint : Interpolator1DType.LinearFlatExtrap);
            }

            var prevSpot = _forwardCurve(0);

            for (var t = _fixings.Length + 1; t < _drifts.Length; t++)
            {
                var time = _timesteps.Times[t] - firstTime;
                var fxAtmVol = _adjSurface == null ? 0.0 : _adjSurface.GetForwardATMVol(0, time);
                var driftAdj = _adjSurface == null ? 1.0 : Exp(atmVols[t] * fxAtmVol * time * _correlation);
                var spot = _forwardCurve(time) * driftAdj;

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
                var c = _fixings.Length;
                _fixings.AsSpan().CopyTo(steps);
                steps[c] = previousStep;

                //foreach (var kv in _pastFixings.Where(x => x.Key < _startDate).OrderBy(x => x.Key))
                //{
                //    steps[c] = new Vector<double>(kv.Value);
                //    c++;
                //}
                //steps[c] = previousStep;

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

            var periodSize = (_expiryDate - _startDate).TotalDays;
            var stepSizeLinear = periodSize / _numberOfSteps;
            var simDates = new List<DateTime>();

            for (double i = 0; i < _numberOfSteps; i++)
            {
                var linStep = i * 1.0/_numberOfSteps;
                var coshStep = (Cosh(linStep) - 1) / (Cosh(1) - 1);
                var stepDays = coshStep * periodSize;
                simDates.Add(_startDate.AddDays(stepDays));
            }

            _timesteps.AddDates(simDates.Distinct());

        }
    }
}
