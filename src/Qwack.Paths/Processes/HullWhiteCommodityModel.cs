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
    /// Hull-White style Monte Carlo model for commodities.
    /// 
    /// Key features:
    /// - Exactly calibrates to the observed forward curve (forwards are martingales)
    /// - Options expiring into immediate delivery recover Black pricing
    /// - Volatility for an option on forward F(T) is taken from the vol surface at T
    ///   regardless of when the option expires (vol depends on delivery, not expiry)
    /// - Mean reversion parameter α controls correlation between forwards
    /// 
    /// The model simulates:
    ///   d(ln F(t,T)) = -½σ(T)²e^{-2α(T-t)}dt + σ(T)e^{-α(T-t)}dW(t)
    /// 
    /// For options expiring at τ on forward F(T), the effective vol is:
    ///   σ_eff = σ(T) * sqrt((1 - e^{-2α(T-τ)}) / (2α(T-τ)))  when T > τ
    ///   σ_eff = σ(T)                                          when T = τ (immediate delivery)
    /// </summary>
    public class HullWhiteCommodityModel : IPathProcess, IRequiresFinish
    {
        private readonly HullWhiteCommodityModelParameters _params;
        private readonly IATMVolSurface _surface;
        private readonly IATMVolSurface _adjSurface;
        private readonly double _fxCorrelation;

        private readonly DateTime _expiryDate;
        private readonly DateTime _startDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        private readonly Func<double, double> _forwardCurve;

        private int _factorIndex;
        private ITimeStepsFeature _timesteps;
        private bool _isComplete;

        private Vector<double>[] _fixings;
        private double[] _drifts;
        private double[] _vols;
        private double[] _volDecay;  // e^{-α(T-t)} factor for each timestep

        public HullWhiteCommodityModel(
            HullWhiteCommodityModelParameters hwParams,
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
            _params = hwParams ?? HullWhiteCommodityModelParameters.CreateDefault();
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
            _vols = new double[_timesteps.TimeStepCount];
            _volDecay = new double[_timesteps.TimeStepCount];

            var six = Array.IndexOf(_timesteps.Dates, _startDate);
            var firstTime = six >= 0 ? _timesteps.Times[six] : 0.0;
            var prevSpot = _forwardCurve(0);
            var alpha = _params.MeanReversion;

            // Total time to expiry (delivery date)
            var totalTime = (_expiryDate - _startDate).TotalDays / 365.25;

            // Get the Black vol at delivery date - this is what we need to match
            double blackVol;
            if (_params.CalibrateToVolSurface)
            {
                blackVol = _surface.GetForwardATMVol(0, totalTime);
            }
            else
            {
                blackVol = _params.Sigma;
            }

            // Compute the scaling factor to ensure HW integrated variance = Black variance
            // Black variance: σ_Black² * T
            // HW variance: σ² * ∫₀^T e^{-2α(T-t)} dt = σ² * (1 - e^{-2αT}) / (2α)
            // 
            // For these to match at delivery (τ = T):
            // σ = σ_Black * √(2αT / (1 - e^{-2αT}))
            //
            // This scaling factor → 1 as α → 0 (recovers Black exactly)
            double volScaleFactor;
            if (alpha < 1e-10 || totalTime < 1e-10)
            {
                volScaleFactor = 1.0;  // No scaling needed for Black-equivalent case
            }
            else
            {
                var hwVarianceFactor = (1 - Exp(-2 * alpha * totalTime)) / (2 * alpha);
                var blackVarianceFactor = totalTime;
                volScaleFactor = Sqrt(blackVarianceFactor / hwVarianceFactor);
                // This equals √(2αT / (1 - e^{-2αT}))
            }

            // Scaled base vol that will integrate to Black variance at delivery
            var scaledBaseVol = blackVol * volScaleFactor;

            for (var t = _fixings.Length + 1; t < _drifts.Length; t++)
            {
                var time = _timesteps.Times[t] - firstTime;
                var prevTime = Max(0, _timesteps.Times[t - 1] - firstTime);
                var dt = _timesteps.TimeSteps[t];

                // Time remaining to delivery from current simulation time
                var timeToDelivery = Max(0, totalTime - time);

                // FX adjustment if needed
                var fxAtmVol = _adjSurface == null ? 0.0 : _adjSurface.GetForwardATMVol(0, time);
                var driftAdj = _adjSurface == null ? 1.0 : Exp(blackVol * fxAtmVol * time * _fxCorrelation);
                var spot = _forwardCurve(time) * driftAdj;

                // Hull-White volatility decay factor: e^{-α(T-t)}
                // This captures how volatility "propagates" through the forward curve
                double volDecayFactor;
                if (alpha < 1e-10)
                {
                    volDecayFactor = 1.0;  // No mean reversion = no decay
                }
                else
                {
                    volDecayFactor = Exp(-alpha * timeToDelivery);
                }

                // For the forward variance over this step, we integrate σ² * e^{-2α(T-s)} from prevTime to time
                // Using the scaled base vol ensures total variance matches Black at delivery
                double stepVariance;
                if (alpha < 1e-10)
                {
                    stepVariance = scaledBaseVol * scaledBaseVol * dt;
                }
                else
                {
                    // Exact integration of variance over the step
                    var T = totalTime;
                    var t1 = prevTime;
                    var t2 = time;
                    // ∫_{t1}^{t2} e^{-2α(T-s)} ds = (e^{-2α(T-t2)} - e^{-2α(T-t1)}) / (2α)
                    var varIntegral = (Exp(-2 * alpha * (T - t2)) - Exp(-2 * alpha * (T - t1))) / (2 * alpha);
                    stepVariance = scaledBaseVol * scaledBaseVol * varIntegral;
                }

                _vols[t] = Sqrt(stepVariance / dt);
                _volDecay[t] = volDecayFactor;

                // Drift calibration: ensure forward is a martingale
                // drift = log(F(t)/F(t-1))/dt - this comes from the forward curve
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
                var steps = block.GetStepsForFactor(path, _factorIndex);
                
                var c = _fixings.Length;
                _fixings.AsSpan().CopyTo(steps);
                
                if (c >= steps.Length)
                    continue;
                    
                steps[c] = previousStep;

                for (var step = c + 1; step < block.NumberOfSteps; step++)
                {
                    var W = steps[step];
                    var dt = new Vector<double>(_timesteps.TimeSteps[step]);
                    var vol = _vols[step];
                    var drift = _drifts[step];

                    // Standard log-normal simulation with Ito correction
                    // d(ln F) = (drift - ½σ²)dt + σ*dW
                    var logReturn = (drift - vol * vol / 2.0) * dt 
                                  + (vol * _timesteps.TimeStepsSqrt[step] * W);
                    
                    previousStep *= logReturn.Exp();
                    steps[step] = previousStep;
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _factorIndex = mappingFeature.AddDimension(_name);

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
