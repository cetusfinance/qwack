using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Math.Extensions;
using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using static System.Math;

namespace Qwack.Paths.Processes
{
    public class SchwartzSmithTwoFactorModel : IPathProcess, IRequiresFinish
    {
        private readonly SchwartzSmithTwoFactorModelParameters _params;
        private readonly IATMVolSurface _surface;
        private readonly IATMVolSurface _adjSurface;
        private readonly double _correlation;

        private readonly DateTime _expiryDate;
        private readonly DateTime _startDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private readonly Func<double, double> _forwardCurve;

        private int _factorIndex1;
        private int _factorIndex2;
        private int _factorIndexPath;

        private ITimeStepsFeature _timesteps;
        private bool _isComplete;

        private double[] _drifts;
        private double[] _vols1;
        private double[] _vols2;
        private Vector<double>[] _fixings;

        //https://core.ac.uk/download/pdf/289952496.pdf

        public SchwartzSmithTwoFactorModel(SchwartzSmithTwoFactorModelParameters ssParams, IATMVolSurface volSurface, Func<double, double> forwardCurve, DateTime startDate, DateTime expiryDate, int nTimeSteps, string name, Dictionary<DateTime, double> pastFixings = null, IATMVolSurface fxAdjustSurface = null, double fxAssetCorrelation = 0.0)
        {
            _params = ssParams;
            _surface = volSurface;
            _forwardCurve = forwardCurve;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps == 0 ? 100 : nTimeSteps;
            _name = name;
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

            _drifts = new double[_timesteps.TimeStepCount];
            _vols1 = new double[_timesteps.TimeStepCount];
            _vols2 = new double[_timesteps.TimeStepCount];

            var dates = collection.GetFeature<ITimeStepsFeature>();
            var fixings = new List<Vector<double>>();
            var lastFixing = 0.0;
            for (var d = 0; d < dates.Dates.Length; d++)
            {
                var date = dates.Dates[d];
                if (date >= _startDate) break;
                if (!_pastFixings.ContainsKey(date.Date))
                {
                    if (lastFixing == 0 && d == 0)
                        continue;
                    fixings.Add(new Vector<double>(lastFixing));
                }
                else
                {
                    if (_pastFixings.TryGetValue(date, out var val))
                        lastFixing = val;
                    fixings.Add(new Vector<double>(lastFixing));
                }
            }
            _fixings = [.. fixings];

            var kappa2 = _params.Kappa_2;
            var sigma1 = _params.Sigma_1;
            var sigma2 = _params.Sigma_2;
            var rho = _params.Rho_1_2;

            var prevSpot = _forwardCurve(0);
            var six = Array.IndexOf(_timesteps.Dates, _startDate);
            var firstTime = _timesteps.Times[six];
            var surfaceTimeProvider = _surface.TimeProvider;

            for (var t = _fixings.Length + 1; t < _drifts.Length; t++)
            {
                var calendarTime = _timesteps.Times[t] - firstTime;
                var surfaceTime = surfaceTimeProvider.GetYearFraction(_startDate, _timesteps.Dates[t]);
                var surfacePrevTime = Max(0, surfaceTimeProvider.GetYearFraction(_startDate, _timesteps.Dates[t - 1]));
                var dt = _timesteps.TimeSteps[t];

                var atmVol = _surface.GetForwardATMVol(0, surfaceTime);
                var fxAtmVol = _adjSurface == null ? 0.0 : _adjSurface.GetForwardATMVol(0, _adjSurface.TimeProvider.GetYearFraction(_startDate, _timesteps.Dates[t]));
                var driftAdj = _adjSurface == null ? 1.0 : Exp(atmVol * fxAtmVol * calendarTime * _correlation);
                var spot = _forwardCurve(calendarTime) * driftAdj;

                var varStart = Pow(_surface.GetForwardATMVol(0, surfacePrevTime), 2) * surfacePrevTime;
                var varEnd = Pow(atmVol, 2) * surfaceTime;
                var fwdVariance = Max(0, varEnd - varStart);
                var totalVol = Sqrt(fwdVariance / dt);

                // SS analytical forward variance contributions per unit dt at time t:
                // factor 1 (GBM): σ₁²
                // factor 2 (OU):  σ₂² · e^{-2κ₂(T-t)} where T≈t for spot simulation
                // cross:          2·ρ·σ₁·σ₂ · e^{-κ₂(T-t)}
                // For spot simulation (T=t): all decay factors are 1
                var var1 = sigma1 * sigma1;
                var var2 = sigma2 * sigma2;
                var varCross = 2.0 * rho * sigma1 * sigma2;
                var totalModelVar = var1 + var2 + varCross;

                double w1, w2;
                if (totalModelVar > 0)
                {
                    // Scale factor to match market total variance
                    var scale = (totalVol * totalVol) / totalModelVar;
                    w1 = Sqrt(scale) * sigma1;
                    w2 = Sqrt(scale) * sigma2;
                }
                else
                {
                    w1 = totalVol;
                    w2 = 0;
                }

                _vols1[t] = w1;
                _vols2[t] = w2;
                _drifts[t] = Log(spot / prevSpot) / dt;
                prevSpot = spot;
            }

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var previousStep = new Vector<double>(_forwardCurve(0));
                var steps1 = block.GetStepsForFactor(path, _factorIndex1);
                var steps2 = block.GetStepsForFactor(path, _factorIndex2);
                var stepsOut = block.GetStepsForFactor(path, _factorIndexPath);

                var c = _fixings.Length;
                _fixings.AsSpan().CopyTo(stepsOut);
                if (c >= stepsOut.Length)
                    continue;
                stepsOut[c] = previousStep;

                for (var step = c + 1; step < block.NumberOfSteps; step++)
                {
                    var W1 = steps1[step];
                    var W2 = steps2[step];
                    var dt = new Vector<double>(_timesteps.TimeSteps[step]);
                    var dtRoot = new Vector<double>(_timesteps.TimeStepsSqrt[step]);
                    var v1 = _vols1[step];
                    var v2 = _vols2[step];
                    var totalVar = v1 * v1 + v2 * v2 + 2.0 * _params.Rho_1_2 * v1 * v2;
                    var bm = (_drifts[step] - totalVar / 2.0) * dt + (v1 * dtRoot * W1) + (v2 * dtRoot * W2);
                    previousStep *= bm.Exp();
                    stepsOut[step] = previousStep;
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _factorIndex1 = mappingFeature.AddDimension(_name + "~1");
            _factorIndex2 = mappingFeature.AddDimension(_name + "~2");
            _factorIndexPath = mappingFeature.AddDimension(_name);

            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            _timesteps.AddDate(_startDate);

            var periodSize = (_expiryDate - _startDate).TotalDays;
            var simDates = new List<DateTime>();

            for (double i = 0; i < _numberOfSteps; i++)
            {
                var linStep = i * 1.0 / _numberOfSteps;
                var coshStep = (Cosh(linStep) - 1) / (Cosh(1) - 1);
                var stepDays = coshStep * periodSize;
                simDates.Add(_startDate.AddDays(stepDays));
            }
            _timesteps.AddDates(simDates.Distinct());
        }
    }
}

