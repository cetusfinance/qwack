using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Underlyings;
using System.Numerics;
using Qwack.Math.Extensions;
using Qwack.Math.Interpolation;
using Qwack.Core.Basic;
using System.Linq;

namespace Qwack.Paths.Processes
{
    public class LVSingleAsset : IPathProcess, IRequiresFinish
    {
        private IVolSurface _surface;
        private readonly DateTime _expiryDate;
        private DateTime _startDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private int _factorIndex;
        private ITimeStepsFeature _timesteps;
        private readonly Func<double, double> _forwardCurve;
        private bool _isComplete;
        private double[] _drifts;
        private IInterpolator1D[] _lvInterps;

        private readonly Vector<double> _two = new Vector<double>(2.0);

        public LVSingleAsset(IVolSurface volSurface, DateTime startDate, DateTime expiryDate, int nTimeSteps, Func<double, double> forwardCurve, string name, Dictionary<DateTime,double> pastFixings=null)
        {
            _surface = volSurface;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps;
            _name = name;
            _pastFixings = pastFixings??(new Dictionary<DateTime,double>());
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
            _lvInterps = new IInterpolator1D[_timesteps.TimeStepCount - 1];

            var strikes = new double[_timesteps.TimeStepCount][];
            for (var t = 0; t < strikes.Length; t++)
            {
                var fwd = _forwardCurve(_timesteps.Times[t]);
                var atmVol = _surface.GetVolForDeltaStrike(0.5, _timesteps.Times[t], fwd);

                if (_timesteps.Times[t] == 0)
                {
                    strikes[t] = new double[] { fwd };
                    continue;
                }
                else
                {
                    strikes[t] = new double[98];
                    for (var k = 0; k < strikes[t].Length; k++)
                    {
                        var deltaK = -(0.01 + 0.01 * k);
                        strikes[t][k] = Options.BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaK, 0, _timesteps.Times[t], atmVol);
                    }
                }
            }

            double[][] lvSurface;
            if(_surface.LocalVolGrid!=null)
            {
                lvSurface = new double[_timesteps.TimeStepCount - 1][];
                for(var t=0;t<lvSurface.GetLength(0);t++)
                {
                    lvSurface[t] = new double[strikes[t].Length];
                    for(var k=0;k<lvSurface[t].Length;k++)
                    {
                        lvSurface[t][k] = _surface.LocalVolGrid.Interpolate(strikes[t][k], _timesteps.Times[t]);
                    }
                }
            }
            else
            {
                lvSurface = Options.LocalVol.ComputeLocalVarianceOnGrid(_surface, strikes, _timesteps.Times, _forwardCurve);
                _surface.LocalVolGrid = InterpolatorFactory.GetInterpolator(strikes, _timesteps.Times.SubArray(0, _timesteps.Times.Length - 1), lvSurface, Interpolator2DType.Bilinear);
            }

            
            for (var t = 0; t < _lvInterps.Length; t++)
            {
                _lvInterps[t] = InterpolatorFactory.GetInterpolator(strikes[t], lvSurface[t], t == 0 ? Interpolator1DType.DummyPoint : Interpolator1DType.LinearFlatExtrap);
            }

            var prevSpot = _forwardCurve(0);
            for (var t = 1; t < _drifts.Length; t++)
            {
                var spot = _forwardCurve(_timesteps.Times[t]);
                _drifts[t] = System.Math.Log(spot / prevSpot) / _timesteps.TimeSteps[t];
    
                prevSpot = spot;
            }
            _isComplete = true;
        }

        public void Process(PathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var previousStep = new Vector<double>(_forwardCurve(0));
                var steps = block.GetStepsForFactor(path, _factorIndex);
                var c = 0;
                foreach(var kv in _pastFixings.Where(x=>x.Key<_startDate))
                {
                    steps[c] = new Vector<double>(kv.Value);
                    c++;
                }
                steps[c] = previousStep;
                for (var step = c+1; step < block.NumberOfSteps; step++)
                {
                    var W = steps[step];
                    var dt = new Vector<double>(_timesteps.TimeSteps[step]);
                    var drifts = new Vector<double>(_drifts[step]);
                    var vols = new double[Vector<double>.Count];
                    for (var s=0;s<vols.Length;s++)
                    {
                        vols[s] = System.Math.Sqrt(
                            System.Math.Max(0,
                            _lvInterps[step - 1].Interpolate(previousStep[s])));
                        
                    }
                    var volVec = new Vector<double>(vols); 
                    var bm = (drifts - volVec * volVec / _two) * dt + (volVec * _timesteps.TimeStepsSqrt[step] * W);
                    previousStep *= bm.Exp();
                    steps[step] = previousStep;
                }
            }
        }

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _factorIndex = mappingFeature.AddDimension(_name);

            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            _timesteps.AddDates(_pastFixings.Keys.Where(x => x < _startDate));

            var stepSize = (_expiryDate - _startDate).TotalDays / _numberOfSteps;
            for (var i = 0; i < _numberOfSteps - 1; i++)
            {
                _timesteps.AddDate(_startDate.AddDays(i * stepSize));
            }
            _timesteps.AddDate(_expiryDate);

        }
    }
}
