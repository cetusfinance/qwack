using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Numerics;
using Qwack.Math.Extensions;
using System.Linq;
using Qwack.Core.Models;
using Qwack.Futures;

namespace Qwack.Paths.Processes
{
    public class BlackFuturesCurve : IPathProcess, IRequiresFinish
    {
        private IATMVolSurface _surface;
        private IFutureSettingsProvider _futureSettingsProvider;
        private readonly DateTime _expiryDate;
        private DateTime _startDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private readonly List<string> _codes;
        private readonly List<DateTime> _futuresExpiries;
        private int[] _factorIndices;
        private int _mainFactorIndex;
        private ITimeStepsFeature _timesteps;
        private readonly Func<DateTime, double> _forwardCurve;
        private bool _isComplete;
        //drifts are zero for individual futures
        private double[][] _vols;


        public BlackFuturesCurve(IATMVolSurface volSurface, DateTime startDate, DateTime expiryDate, int nTimeSteps, Func<DateTime, double> forwardCurve, string name, IFutureSettingsProvider futureSettingsProvider, Dictionary<DateTime, double> pastFixings = null)
        {
            _surface = volSurface;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps;
            _name = name;
            _forwardCurve = forwardCurve;
            _pastFixings = pastFixings ?? (new Dictionary<DateTime, double>());
            _futureSettingsProvider = futureSettingsProvider;

            if (startDate > expiryDate)
                throw new Exception("Start date must be before expiry date");

            _codes = new List<string>();
            _futuresExpiries = new List<DateTime>();

            var fCode = new FutureCode(name, _futureSettingsProvider);
            var currentCode = fCode.GetFrontMonth(startDate);
            _codes.Add(currentCode);
            _futuresExpiries.Add(fCode.GetRollDate());

            fCode = new FutureCode(currentCode, DateTime.Today.Year - 2, _futureSettingsProvider);
            var targetCode = fCode.GetFrontMonth(expiryDate);
            while (currentCode != targetCode)
            {
                currentCode = fCode.GetNextCode(false);
                _futuresExpiries.Add(fCode.GetRollDate());
                _codes.Add(currentCode);
            }
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

            for (var t = 1; t < _vols.Length; t++)
            {
                _vols[t] = new double[_codes.Count];
                for (var c = 0; c < _vols[t].Length; c++)
                {
                    _vols[t][c] = _surface.GetVolForDeltaStrike(0.5, _futuresExpiries[c], 1.0);
                }
            }
            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var stepsMain = block.GetStepsForFactor(path, _mainFactorIndex);

                for (var f = 0; f < _factorIndices.Length; f++)
                {
                    var previousStep = new Vector<double>(_forwardCurve(_futuresExpiries[f]));
                    var steps = block.GetStepsForFactor(path, _factorIndices[f]);

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
                        var bm = (_vols[f][step] * _vols[f][step] / 2.0) * dt + (_vols[f][step] * _timesteps.TimeStepsSqrt[step] * W);
                        previousStep *= bm.Exp();
                        steps[step] = previousStep;
                    }
                }

                var t = 0;
                for (var f = 0; f < _factorIndices.Length; f++)
                {
                    if (_futuresExpiries[f] <= _timesteps.Dates[t])
                        continue;
                    var steps = block.GetStepsForFactor(path, _factorIndices[f]);
                    stepsMain[t] = steps[t];
                    t++;
                    if (t >= stepsMain.Length)
                        break;
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            _factorIndices = new int[_codes.Count];
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            for (var c = 0; c < _factorIndices.Length; c++)
            {
                _factorIndices[c] = mappingFeature.AddDimension(_codes[c]);
            }
            _mainFactorIndex = mappingFeature.AddDimension(_name);

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
