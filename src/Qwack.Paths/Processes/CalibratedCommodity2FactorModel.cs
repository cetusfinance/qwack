using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Math.Extensions;
using Qwack.Options;
using Qwack.Options.Calibrators;
using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using Qwack.Serialization;
using static System.Math;

namespace Qwack.Paths.Processes
{
    /// <summary>
    /// A two-factor commodity model (Schwartz-Smith style) that calibrates to market data:
    /// - ATM implied volatility term structure
    /// - Forward curve (matched exactly via drift adjustment)
    /// - Swaption volatilities (optional)
    ///
    /// Dynamics:
    /// dX1 = mu * dt + sigma1 * dW1           (GBM - long-term equilibrium)
    /// dX2 = -kappa * X2 * dt + sigma2 * dW2  (OU - short-term deviation)
    /// E[dW1 * dW2] = rho * dt
    /// ln(S) = X1 + X2
    /// S = exp(X1 + X2)
    /// </summary>
    public class CalibratedCommodity2FactorModel : IPathProcess, IRequiresFinish
    {
        private readonly IATMVolSurface _surface;
        private readonly IATMVolSurface _adjSurface;
        private readonly double _correlation;

        private readonly DateTime _expiryDate;
        private readonly DateTime _startDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private readonly Commodity2FactorCalibrationSettings _calibrationSettings;

        private int _factorIndex1;
        private int _factorIndex2;
        private int _factorIndexPath;

        private ITimeStepsFeature _timesteps;
        [SkipSerialization]
        private readonly Func<double, double> _forwardCurve;
        private bool _isComplete;

        // Calibrated parameters
        private Commodity2FactorCalibratedParams _calibratedParams;

        // Per-timestep values computed in Finish()
        private double[] _drifts;
        private double[] _sigma1Values;
        private double[] _sigma2Values;
        private double _kappa;
        private double _rho;
        private Vector<double>[] _fixings;

        public CalibratedCommodity2FactorModel(
            IATMVolSurface volSurface,
            DateTime startDate,
            DateTime expiryDate,
            int nTimeSteps,
            Func<double, double> forwardCurve,
            string name,
            Dictionary<DateTime, double> pastFixings = null,
            Commodity2FactorCalibrationSettings calibrationSettings = null,
            IATMVolSurface fxAdjustSurface = null,
            double fxAssetCorrelation = 0.0)
        {
            _surface = volSurface;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps == 0 ? 100 : nTimeSteps;
            _name = name;
            _forwardCurve = forwardCurve;
            _pastFixings = pastFixings ?? new Dictionary<DateTime, double>();
            _calibrationSettings = calibrationSettings ?? new Commodity2FactorCalibrationSettings();

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

            // Step 1: Run calibration
            var calibrator = new Commodity2FactorCalibrator
            {
                Tolerance = _calibrationSettings.Tolerance,
                MaxIterations = _calibrationSettings.MaxIterations
            };

            _calibratedParams = calibrator.Calibrate(
                _surface,
                _forwardCurve,
                _startDate,
                _calibrationSettings,
                _calibrationSettings.SwaptionVols);

            _kappa = _calibratedParams.Kappa;
            _rho = _calibratedParams.Rho;

            // Step 2: Set up past fixings
            var dates = collection.GetFeature<ITimeStepsFeature>();
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

                    var vect = new Vector<double>(lastFixing);
                    fixings.Add(vect);
                }
                else
                {
                    lastFixing = _pastFixings[date.Date];
                    var vect = new Vector<double>(lastFixing);
                    fixings.Add(vect);
                }
            }
            _fixings = fixings.ToArray();

            // Step 3: Compute per-timestep drifts and vols
            _drifts = new double[_timesteps.TimeStepCount];
            _sigma1Values = new double[_timesteps.TimeStepCount];
            _sigma2Values = new double[_timesteps.TimeStepCount];

            var six = Array.IndexOf(_timesteps.Dates, _startDate);
            var firstTime = _timesteps.Times[six];
            var prevSpot = _forwardCurve(0);

            for (var t = _fixings.Length + 1; t < _drifts.Length; t++)
            {
                var time = _timesteps.Times[t] - firstTime;
                var prevTime = Max(0, _timesteps.Times[t - 1] - firstTime);

                // FX adjustment if present
                var fxAtmVol = _adjSurface == null ? 0.0 : _adjSurface.GetForwardATMVol(0, time);
                var atmVol = _surface.GetForwardATMVol(0, time);
                var driftAdj = _adjSurface == null ? 1.0 : Exp(atmVol * fxAtmVol * time * _correlation);
                var spot = _forwardCurve(time) * driftAdj;

                // Forward curve matching drift (like BlackSingleAsset)
                _drifts[t] = Log(spot / prevSpot) / _timesteps.TimeSteps[t];
                prevSpot = spot;

                // Compute forward vols for this timestep
                // We use the calibrated constant vols but could extend to term-dependent
                var varStart = Pow(_surface.GetForwardATMVol(0, prevTime), 2) * prevTime;
                var varEnd = Pow(atmVol, 2) * time;
                var fwdVariance = Max(0, varEnd - varStart);
                var fwdVol = Sqrt(fwdVariance / _timesteps.TimeSteps[t]);

                // Distribute the variance between the two factors proportionally
                var totalModelVar = _calibratedParams.Sigma1 * _calibratedParams.Sigma1
                                  + _calibratedParams.Sigma2 * _calibratedParams.Sigma2;

                if (totalModelVar > 0)
                {
                    var scaleFactor = fwdVol / Sqrt(totalModelVar);
                    _sigma1Values[t] = _calibratedParams.Sigma1 * scaleFactor;
                    _sigma2Values[t] = _calibratedParams.Sigma2 * scaleFactor;
                }
                else
                {
                    _sigma1Values[t] = _calibratedParams.Sigma1;
                    _sigma2Values[t] = _calibratedParams.Sigma2;
                }
            }

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                // Initialize state variables
                var x1 = new Vector<double>(_calibratedParams.X1_0);
                var x2 = new Vector<double>(_calibratedParams.X2_0);
                var previousStep = (x1 + x2).Exp();

                var steps1 = block.GetStepsForFactor(path, _factorIndex1);
                var steps2 = block.GetStepsForFactor(path, _factorIndex2);
                var stepsOut = block.GetStepsForFactor(path, _factorIndexPath);

                var c = _fixings.Length;
                _fixings.AsSpan().CopyTo(stepsOut);
                if (c >= stepsOut.Length)
                    continue;
                stepsOut[c] = previousStep;

                var kappaVec = new Vector<double>(_kappa);

                for (var step = c + 1; step < block.NumberOfSteps; step++)
                {
                    var W1 = steps1[step];
                    var W2 = steps2[step];
                    var dt = new Vector<double>(_timesteps.TimeSteps[step]);
                    var dtRoot = new Vector<double>(_timesteps.TimeStepsSqrt[step]);

                    var sigma1 = new Vector<double>(_sigma1Values[step]);
                    var sigma2 = new Vector<double>(_sigma2Values[step]);
                    var drift = new Vector<double>(_drifts[step]);

                    // Evolve X1 (GBM-like factor with drift adjustment for forward matching)
                    // dX1 = drift * dt + sigma1 * dW1
                    // Using log-normal discretization for numerical stability
                    var halfVec = new Vector<double>(0.5);
                    var dx1 = (drift - sigma1 * sigma1 * halfVec) * dt + sigma1 * dtRoot * W1;
                    x1 += dx1;

                    // Evolve X2 (OU mean-reverting factor)
                    // dX2 = -kappa * X2 * dt + sigma2 * dW2
                    var dx2 = -kappaVec * x2 * dt + sigma2 * dtRoot * W2;
                    x2 += dx2;

                    // Compute spot price
                    previousStep = (x1 + x2).Exp();
                    stepsOut[step] = previousStep;
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();

            // Register 3 dimensions: two factors and the combined output
            _factorIndex1 = mappingFeature.AddDimension(_name + "~1");
            _factorIndex2 = mappingFeature.AddDimension(_name + "~2");
            _factorIndexPath = mappingFeature.AddDimension(_name);

            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            _timesteps.AddDate(_startDate);

            // Hyperbolic cosine spacing for simulation dates (like SchwartzSmithTwoFactorModel)
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
