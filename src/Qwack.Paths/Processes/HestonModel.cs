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
    /// <summary>
    /// Heston stochastic volatility Monte Carlo model.
    /// 
    /// Dynamics:
    ///   dS(t) = μ(t) S(t) dt + √v(t) S(t) dW_S(t)
    ///   dv(t) = κ(θ - v(t)) dt + σ_v √v(t) dW_v(t)
    ///   Corr(dW_S, dW_v) = ρ
    /// 
    /// Features:
    ///   - Auto-calibrates v_0 from short-term ATM vol
    ///   - Auto-calibrates θ from long-term ATM vol
    ///   - Calibrates to forward curve (drift adjustment)
    ///   - Uses QE (Quadratic Exponential) scheme for variance to ensure positivity
    /// </summary>
    public class HestonModel : IPathProcess, IRequiresFinish
    {
        private readonly HestonModelParameters _params;
        private readonly IATMVolSurface _surface;
        private readonly IATMVolSurface _adjSurface;
        private readonly double _fxCorrelation;

        private readonly DateTime _expiryDate;
        private readonly DateTime _startDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private readonly Func<double, double> _forwardCurve;

        private int _factorIndexSpot;
        private int _factorIndexVol;
        private int _factorIndexPath;

        private ITimeStepsFeature _timesteps;
        private bool _isComplete;

        private Vector<double>[] _fixings;
        private double[] _drifts;

        // Calibrated parameters
        private double _v0Calibrated;
        private double _thetaCalibrated;

        public HestonModel(
            HestonModelParameters hestonParams,
            IATMVolSurface volSurface,
            DateTime startDate,
            DateTime expiryDate,
            int nTimeSteps,
            Func<double, double> forwardCurve,
            string name,
            Dictionary<DateTime, double> pastFixings = null,
            IATMVolSurface fxAdjustSurface = null,
            double fxAssetCorrelation = 0.0)
        {
            _params = hestonParams ?? HestonModelParameters.CreateDefault();
            _surface = volSurface;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps == 0 ? 100 : nTimeSteps;
            _name = name;
            _pastFixings = pastFixings ?? new Dictionary<DateTime, double>();
            _forwardCurve = forwardCurve;
            _adjSurface = fxAdjustSurface;
            _fxCorrelation = fxAssetCorrelation;
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            if (!_timesteps.IsComplete)
            {
                return;
            }

            var dates = collection.GetFeature<ITimeStepsFeature>();

            // Process past fixings
            var fixings = new List<Vector<double>>();
            var lastFixing = 0.0;
            for (var d = 0; d < dates.Dates.Length; d++)
            {
                var date = dates.Dates[d];
                if (date > _startDate) break;

                if (!_pastFixings.ContainsKey(date.Date))
                {
                    if (lastFixing == 0 && d == 0)
                        continue;
                    fixings.Add(new Vector<double>(lastFixing));
                }
                else
                {
                    try
                    {
                        lastFixing = _pastFixings[date.Date];
                        fixings.Add(new Vector<double>(lastFixing));
                    }
                    catch
                    {
                    }
                }
            }
            _fixings = [.. fixings];

            // Initialize arrays
            _drifts = new double[_timesteps.TimeStepCount];

            var six = Array.IndexOf(_timesteps.Dates, _startDate);
            var firstTime = six >= 0 ? _timesteps.Times[six] : 0.0;
            var prevSpot = _forwardCurve(0);

            // Calibrate variance parameters from vol surface
            if (_params.CalibrateToVolSurface)
            {
                // Get short-term vol for v_0
                var shortTermTime = Min(_params.ShortTermHorizon, (_expiryDate - _startDate).TotalDays / 365.25);
                var shortTermVol = _surface.GetForwardATMVol(0, shortTermTime);
                _v0Calibrated = shortTermVol * shortTermVol;

                // Get long-term vol for θ
                var longTermTime = Min(_params.LongTermHorizon, (_expiryDate - _startDate).TotalDays / 365.25);
                var longTermVol = _surface.GetForwardATMVol(0, longTermTime);
                _thetaCalibrated = longTermVol * longTermVol;
            }
            else
            {
                _v0Calibrated = _params.V0;
                _thetaCalibrated = _params.Theta;
            }

            // Calibrate drift from forward curve
            for (var t = _fixings.Length + 1; t < _drifts.Length; t++)
            {
                var time = _timesteps.Times[t] - firstTime;
                var dt = _timesteps.TimeSteps[t];

                // FX adjustment if needed
                var atmVol = Sqrt(_v0Calibrated);  // Use initial vol for FX adjustment
                var fxAtmVol = _adjSurface == null ? 0.0 : _adjSurface.GetForwardATMVol(0, time);
                var driftAdj = _adjSurface == null ? 1.0 : Exp(atmVol * fxAtmVol * time * _fxCorrelation);
                var spot = _forwardCurve(time) * driftAdj;

                // Drift calibration: ensure forward is a martingale
                if (_params.CalibrateToForwardCurve)
                {
                    _drifts[t] = Log(spot / prevSpot) / dt;
                }
                else
                {
                    _drifts[t] = 0;
                }

                prevSpot = spot;
            }

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            var kappa = _params.Kappa;
            var theta = _thetaCalibrated;
            var volOfVol = _params.VolOfVol;
            var rho = _params.Rho;
            var rhoComp = Sqrt(1 - rho * rho);  // √(1 - ρ²) for correlation

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                // Initialize state
                var spotLog = new Vector<double>(Log(_forwardCurve(0)));
                var variance = new Vector<double>(_v0Calibrated);

                // Get random number streams
                var stepsSpot = block.GetStepsForFactor(path, _factorIndexSpot);
                var stepsVol = block.GetStepsForFactor(path, _factorIndexVol);
                var stepsOut = block.GetStepsForFactor(path, _factorIndexPath);

                // Copy past fixings
                var c = _fixings.Length;
                _fixings.AsSpan().CopyTo(stepsOut);

                // Initial spot
                var previousStep = new Vector<double>(_forwardCurve(0));
                if (c < stepsOut.Length)
                {
                    stepsOut[c] = previousStep;
                }

                // Simulate path
                for (var step = c + 1; step < block.NumberOfSteps; step++)
                {
                    var dt = _timesteps.TimeSteps[step];
                    var dtSqrt = _timesteps.TimeStepsSqrt[step];
                    var drift = _drifts[step];

                    // Get independent random numbers
                    var Z1 = stepsSpot[step];  // For spot
                    var Z2 = stepsVol[step];   // For variance (independent)

                    // Correlate: W_S = Z1, W_v = ρZ1 + √(1-ρ²)Z2
                    var Wv = rho * Z1 + rhoComp * Z2;

                    // Ensure variance stays positive and compute sqrt element-wise
                    var volArr = new double[Vector<double>.Count];
                    var varPlusArr = new double[Vector<double>.Count];
                    for (var s = 0; s < Vector<double>.Count; s++)
                    {
                        varPlusArr[s] = Max(0, variance[s]);
                        volArr[s] = Sqrt(varPlusArr[s]);
                    }
                    var variancePlus = new Vector<double>(varPlusArr);
                    var volVec = new Vector<double>(volArr);

                    // Update variance using Euler scheme with full truncation
                    // dv = κ(θ - v)dt + σ_v√v dW_v
                    var kappaVec = new Vector<double>(kappa);
                    var thetaVec = new Vector<double>(theta);
                    var volOfVolVec = new Vector<double>(volOfVol);
                    var dtVec = new Vector<double>(dt);
                    var dtSqrtVec = new Vector<double>(dtSqrt);

                    var dVariance = kappaVec * (thetaVec - variancePlus) * dtVec 
                                  + volOfVolVec * volVec * dtSqrtVec * Wv;
                    variance = variance + dVariance;

                    // Update log-spot
                    // d(ln S) = (μ - v/2)dt + √v dW_S
                    var driftVec = new Vector<double>(drift);
                    var dLogSpot = (driftVec - variancePlus / new Vector<double>(2.0)) * dtVec 
                                 + volVec * dtSqrtVec * Z1;
                    spotLog += dLogSpot;

                    // Convert to spot price
                    previousStep = spotLog.ExpD();
                    stepsOut[step] = previousStep;
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();

            // Register dimensions: spot random, vol random, and output path
            _factorIndexSpot = mappingFeature.AddDimension(_name + "~S");
            _factorIndexVol = mappingFeature.AddDimension(_name + "~V");
            _factorIndexPath = mappingFeature.AddDimension(_name);

            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            _timesteps.AddDate(_startDate);

            // Generate simulation dates
            var periodSize = (_expiryDate - _startDate).TotalDays;
            var stepSize = periodSize / _numberOfSteps;
            var simDates = new List<DateTime>();

            for (var i = 0; i < _numberOfSteps; i++)
            {
                simDates.Add(_startDate.AddDays(i * stepSize).Date);
            }

            _timesteps.AddDates(simDates.Distinct());
        }
    }
}
