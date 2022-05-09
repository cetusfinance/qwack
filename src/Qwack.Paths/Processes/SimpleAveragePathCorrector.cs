using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Models;
using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using Qwack.Serialization;
using static System.Math;

namespace Qwack.Paths.Processes
{
    public class SimpleAveragePathCorrector : IPathProcess, IRequiresFinish
    {
        public SimpleAveragePathCalculator PathCalc { get; private set; }
        private readonly IATMVolSurface _surface;
        private ITimeStepsFeature _timesteps;
        private readonly IATMVolSurface _adjSurface;
        private readonly double _correlation;

        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private int _factorIndex;

        [SkipSerialization]
        private readonly Func<double, double> _forwardCurve;
        private bool _isComplete;
        private double[] _fwds;
        private Vector<double>[] _correctionFactors;
        private Dictionary<int, Vector<double>[]> _correctionFactorsByBlock;

        public SimpleAveragePathCorrector(SimpleAveragePathCalculator pathCalc, IATMVolSurface volSurface, Func<double, double> forwardCurve, string name, Dictionary<DateTime, double> pastFixings = null, IATMVolSurface fxAdjustSurface = null, double fxAssetCorrelation = 0.0)
        {
            PathCalc = pathCalc;
            _surface = volSurface;
            _name = name;
            _forwardCurve = forwardCurve;
            _pastFixings = pastFixings ?? (new Dictionary<DateTime, double>());

            _adjSurface = fxAdjustSurface;
            _correlation = fxAssetCorrelation;
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            if (!_timesteps.IsComplete)
            {
                return;
            }

            _fwds = new double[_timesteps.TimeStepCount];

            for (var t = 0; t < _fwds.Length; t++)
            {
                var atmVol = _surface.GetForwardATMVol(0, _timesteps.Times[t]);
                var fxAtmVol = _adjSurface == null ? 0.0 : _adjSurface.GetForwardATMVol(0, _timesteps.Times[t]);
                var driftAdj = _adjSurface == null ? 1.0 : Exp(atmVol * fxAtmVol * _timesteps.Times[t] * _correlation);
                var spot = _forwardCurve(_timesteps.Times[t]) * driftAdj;
                _fwds[t] = spot;
            }
            _isComplete = true;
        }

        private readonly object _threadLock = new();
        private void SetupFactors()
        {
            if (PathCalc.CompactMode)
            {
                if (_correctionFactorsByBlock == null)
                {
                    lock (_threadLock)
                    {
                        if (_correctionFactorsByBlock == null)
                        {
                            _correctionFactorsByBlock = new Dictionary<int, Vector<double>[]>();
                        }
                    }
                }
            }
            else
            {
                if (_correctionFactors == null)
                {
                    lock (_threadLock)
                    {
                        if (_correctionFactors == null)
                        {
                            _correctionFactors = _fwds.Select((f, ix) => new Vector<double>(f / PathCalc.PathAvg[ix])).ToArray();
                        }
                    }
                }
            }
        }


        public void Process(IPathBlock block)
        {
            SetupFactors();
            if (PathCalc.CompactMode)
            {
                var averagesForThisBlock = PathCalc.PathSumsByBlock[block.GlobalPathIndex].Select(a => a / PathCalc.PathCountsByBlock[block.GlobalPathIndex]).ToArray();
                var factorsForThisBlock = _fwds.Select((f, ix) => new Vector<double>(f / averagesForThisBlock[ix])).ToArray();
                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var steps = block.GetStepsForFactor(path, _factorIndex);

                    for (var step = 0; step < block.NumberOfSteps; step++)
                    {
                        steps[step] *= factorsForThisBlock[step];
                    }
                }
            }
            else
            {
                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var steps = block.GetStepsForFactor(path, _factorIndex);

                    for (var step = 0; step < block.NumberOfSteps; step++)
                    {
                        steps[step] *= _correctionFactors[step];
                    }
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _factorIndex = mappingFeature.GetDimension(_name);
            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();

        }
    }
}

