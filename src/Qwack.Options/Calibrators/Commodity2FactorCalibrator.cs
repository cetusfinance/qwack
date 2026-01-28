using System;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Math.Solvers;
using Qwack.Options.VolSurfaces;
using static System.Math;

namespace Qwack.Options.Calibrators
{
    public class Commodity2FactorCalibrator
    {
        public double Tolerance { get; set; } = 1e-8;
        public int MaxIterations { get; set; } = 10000;

        /// <summary>
        /// Calibrate a 2-factor commodity model to ATM vol surface and optionally swaption vols.
        /// </summary>
        /// <param name="volSurface">ATM implied volatility surface</param>
        /// <param name="forwardCurve">Forward curve as function of time in years</param>
        /// <param name="originDate">Valuation date</param>
        /// <param name="settings">Calibration settings (optional)</param>
        /// <param name="swaptionVols">Swaption market data (optional)</param>
        /// <returns>Calibrated parameters</returns>
        public Commodity2FactorCalibratedParams Calibrate(
            IATMVolSurface volSurface,
            Func<double, double> forwardCurve,
            DateTime originDate,
            Commodity2FactorCalibrationSettings settings = null,
            SwaptionVolData swaptionVols = null)
        {
            settings ??= new Commodity2FactorCalibrationSettings();
            Tolerance = settings.Tolerance;
            MaxIterations = settings.MaxIterations;

            // Get calibration maturities from settings or generate defaults
            var maturities = settings.CalibrationMaturities;
            if (maturities == null || maturities.Length == 0)
            {
                // Default: calibrate to vols at 1M, 3M, 6M, 1Y, 2Y, 3Y
                maturities = new[]
                {
                    originDate.AddMonths(1),
                    originDate.AddMonths(3),
                    originDate.AddMonths(6),
                    originDate.AddYears(1),
                    originDate.AddYears(2),
                    originDate.AddYears(3)
                };
            }

            // Get market vols at calibration maturities
            var marketVols = new double[maturities.Length];
            var times = new double[maturities.Length];
            for (var i = 0; i < maturities.Length; i++)
            {
                times[i] = (maturities[i] - originDate).TotalDays / 365.0;
                if (times[i] > 0)
                {
                    marketVols[i] = volSurface.GetForwardATMVol(0, times[i]);
                }
                else
                {
                    marketVols[i] = volSurface.GetForwardATMVol(0, 1.0 / 365.0); // Use 1-day vol as proxy
                }
            }

            // Filter out any zero times
            var validIndices = times.Select((t, i) => new { t, i }).Where(x => x.t > 0).Select(x => x.i).ToArray();
            times = validIndices.Select(i => times[i]).ToArray();
            marketVols = validIndices.Select(i => marketVols[i]).ToArray();

            // Stage 1: ATM Vol Calibration
            var calibratedParams = CalibrateToATMVols(times, marketVols, settings);

            // Stage 2: Swaption Calibration (if provided)
            if (settings.CalibrateToSwaptions && swaptionVols != null)
            {
                calibratedParams = RefineWithSwaptionVols(calibratedParams, swaptionVols, originDate, times, marketVols);
            }

            // Store calibration quality metrics
            calibratedParams.MarketVols = marketVols;
            calibratedParams.ModelVols = new double[times.Length];
            for (var i = 0; i < times.Length; i++)
            {
                calibratedParams.ModelVols[i] = NFactorUtils.ImpliedVolForFuturesOption2F(
                    calibratedParams.Sigma1,
                    calibratedParams.Sigma2,
                    calibratedParams.Kappa,
                    calibratedParams.Rho,
                    times[i],
                    times[i]);
            }

            return calibratedParams;
        }

        private Commodity2FactorCalibratedParams CalibrateToATMVols(
            double[] times,
            double[] marketVols,
            Commodity2FactorCalibrationSettings settings)
        {
            // Initial guesses
            var avgVol = marketVols.Average();
            var sigma1Init = settings.InitialSigma1 ?? avgVol * 0.7;
            var sigma2Init = settings.InitialSigma2 ?? avgVol * 0.5;
            var kappaInit = settings.InitialKappa ?? 1.0;
            var rhoInit = settings.InitialRho ?? 0.5;

            var startingPoint = new[] { sigma1Init, sigma2Init, kappaInit, rhoInit };
            var initialStep = new[] { avgVol * 0.2, avgVol * 0.2, 0.5, 0.2 };

            // Objective function: sum of squared vol errors
            var objectiveFunc = new Func<double[], double>(x =>
            {
                var sigma1 = x[0];
                var sigma2 = x[1];
                var kappa = x[2];
                var rho = x[3];

                // Penalty for invalid parameters
                if (sigma1 < 0 || sigma2 < 0 || kappa < 0.01 || rho < -0.999 || rho > 0.999)
                    return 1e20;

                var totalError = 0.0;
                for (var i = 0; i < times.Length; i++)
                {
                    var modelVol = NFactorUtils.ImpliedVolForFuturesOption2F(sigma1, sigma2, kappa, rho, times[i], times[i]);
                    var error = modelVol - marketVols[i];
                    totalError += error * error;
                }

                return totalError;
            });

            // Optimize using Nelder-Mead
            var optimal = NelderMead.MethodSolve(objectiveFunc, startingPoint, initialStep, Tolerance, MaxIterations);

            var result = new Commodity2FactorCalibratedParams
            {
                Sigma1 = Max(0.001, optimal[0]),
                Sigma2 = Max(0.001, optimal[1]),
                Kappa = Max(0.01, optimal[2]),
                Rho = Max(-0.999, Min(0.999, optimal[3])),
                X1_0 = 0,
                X2_0 = 0,
                CalibrationError = objectiveFunc(optimal)
            };

            return result;
        }

        private Commodity2FactorCalibratedParams RefineWithSwaptionVols(
            Commodity2FactorCalibratedParams currentParams,
            SwaptionVolData swaptionVols,
            DateTime originDate,
            double[] atmTimes,
            double[] atmMarketVols)
        {
            if (swaptionVols.OptionExpiries == null || swaptionVols.OptionExpiries.Length == 0)
                return currentParams;

            // Use current calibration as starting point
            var startingPoint = new[]
            {
                currentParams.Sigma1,
                currentParams.Sigma2,
                currentParams.Kappa,
                currentParams.Rho
            };
            var initialStep = new[] { 0.05, 0.05, 0.2, 0.1 };

            // Get swaption data
            var swaptionWeights = swaptionVols.Weights ?? Enumerable.Repeat(1.0, swaptionVols.OptionExpiries.Length).ToArray();

            // Objective function: weighted sum of ATM vol errors + swaption vol errors
            var objectiveFunc = new Func<double[], double>(x =>
            {
                var sigma1 = x[0];
                var sigma2 = x[1];
                var kappa = x[2];
                var rho = x[3];

                // Penalty for invalid parameters
                if (sigma1 < 0 || sigma2 < 0 || kappa < 0.01 || rho < -0.999 || rho > 0.999)
                    return 1e20;

                // ATM vol errors (weight = 1.0)
                var atmError = 0.0;
                for (var i = 0; i < atmTimes.Length; i++)
                {
                    var modelVol = NFactorUtils.ImpliedVolForFuturesOption2F(sigma1, sigma2, kappa, rho, atmTimes[i], atmTimes[i]);
                    var error = modelVol - atmMarketVols[i];
                    atmError += error * error;
                }

                // Swaption vol errors
                var swaptionError = 0.0;
                for (var i = 0; i < swaptionVols.OptionExpiries.Length; i++)
                {
                    var tOption = (swaptionVols.OptionExpiries[i] - originDate).TotalDays / 365.0;
                    if (tOption <= 0) continue;

                    // Build averaging dates for the swap period
                    var swapStart = swaptionVols.SwapStartDates[i];
                    var swapEnd = swaptionVols.SwapEndDates[i];
                    var swapDays = (int)(swapEnd - swapStart).TotalDays;
                    var nDates = Max(1, swapDays / 30); // Approximate monthly averaging
                    var avgDates = new DateTime[nDates];
                    for (var j = 0; j < nDates; j++)
                    {
                        avgDates[j] = swapStart.AddDays(j * swapDays / nDates);
                    }

                    var modelVol = NFactorUtils.ImpliedVolForSwaption2F(sigma1, sigma2, kappa, rho, tOption, avgDates, originDate);
                    var marketVol = swaptionVols.ImpliedVols[i];
                    var error = modelVol - marketVol;
                    swaptionError += swaptionWeights[i] * error * error;
                }

                // Combined objective with equal weighting between ATM and swaption fits
                return atmError + swaptionError;
            });

            // Optimize using Nelder-Mead
            var optimal = NelderMead.MethodSolve(objectiveFunc, startingPoint, initialStep, Tolerance, MaxIterations);

            return new Commodity2FactorCalibratedParams
            {
                Sigma1 = Max(0.001, optimal[0]),
                Sigma2 = Max(0.001, optimal[1]),
                Kappa = Max(0.01, optimal[2]),
                Rho = Max(-0.999, Min(0.999, optimal[3])),
                X1_0 = 0,
                X2_0 = 0,
                CalibrationError = objectiveFunc(optimal)
            };
        }
    }
}
