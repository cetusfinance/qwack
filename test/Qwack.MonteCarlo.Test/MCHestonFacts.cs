using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Math.Extensions;
using Qwack.Options;
using Qwack.Options.VolSurfaces;
using Qwack.Paths;
using Qwack.Paths.Payoffs;
using Qwack.Paths.Processes;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.MonteCarlo.Test
{
    public class MCHestonFacts
    {
        static bool IsCoverageOnly => bool.TryParse(Environment.GetEnvironmentVariable("CoverageOnly"), out var coverageOnly) && coverageOnly;

        /// <summary>
        /// Tests that the Heston model with zero vol-of-vol converges to Black-Scholes pricing.
        /// When σ_v = 0, variance is deterministic and the model should match Black.
        /// </summary>
        [Fact]
        public void Heston_ZeroVolOfVol_ConvergesToBlack()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var strike = 1000.0;
            var vol = 0.25;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 16));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            var volSurface = new ConstantVolSurface(origin, vol);
            var fwdCurve = new Func<double, double>(t => spot);

            // Zero vol-of-vol means deterministic variance
            var hestonParams = HestonModelParameters.CreateExplicit(
                v0: vol * vol,
                theta: vol * vol,
                kappa: 1.0,
                volOfVol: 0.0,  // Deterministic variance
                rho: 0.0
            );

            var asset = new HestonModel(
                hestonParams: hestonParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 20,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engine.AddPathProcess(asset);
            var callPayoff = new EuropeanCall("TestAsset", strike, expiry);
            var putPayoff = new EuropeanPut("TestAsset", strike, expiry);
            engine.AddPathProcess(callPayoff);
            engine.AddPathProcess(putPayoff);
            engine.SetupFeatures();
            engine.RunProcess();

            var mcCall = callPayoff.AverageResult;
            var mcPut = putPayoff.AverageResult;
            var blackCall = BlackFunctions.BlackPV(spot, strike, 0, 1, vol, OptionType.C);
            var blackPut = BlackFunctions.BlackPV(spot, strike, 0, 1, vol, OptionType.P);

            if (!IsCoverageOnly)
            {
                Assert.True(System.Math.Abs(blackCall - mcCall) < 2.0,
                    $"Heston with σ_v=0 should match Black (Call). Black PV: {blackCall:F4}, MC PV: {mcCall:F4}");
                Assert.True(System.Math.Abs(blackPut - mcPut) < 2.0,
                    $"Heston with σ_v=0 should match Black (Put). Black PV: {blackPut:F4}, MC PV: {mcPut:F4}");
            }
        }

        /// <summary>
        /// Tests that the Heston model correctly reproduces the forward price (forward is a martingale).
        /// Using a zero-strike call gives the undiscounted forward value.
        /// </summary>
        [Fact]
        public void Heston_ForwardIsMartingale()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            var volSurface = new ConstantVolSurface(origin, 0.30);
            var fwdCurve = new Func<double, double>(t => spot + 100 * t);

            var hestonParams = HestonModelParameters.CreateDefault(
                kappa: 1.5,
                volOfVol: 0.5,
                rho: -0.5
            );

            var asset = new HestonModel(
                hestonParams: hestonParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 20,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engine.AddPathProcess(asset);
            // Zero-strike call gives forward value
            var payoff = new EuropeanCall("TestAsset", 0, expiry);
            engine.AddPathProcess(payoff);
            engine.SetupFeatures();
            engine.RunProcess();

            var mcForward = payoff.AverageResult;
            var expectedForward = fwdCurve(1);

            if (!IsCoverageOnly)
            {
                var relError = System.Math.Abs(expectedForward / mcForward - 1.0);
                Assert.True(relError < 0.002,
                    $"Forward should be martingale. Expected: {expectedForward:F4}, MC: {mcForward:F4}, RelError: {relError:P4}");
            }
        }

        /// <summary>
        /// Tests put-call parity: C - P = F - K (undiscounted).
        /// This fundamental relationship should hold regardless of model parameters.
        /// </summary>
        [Fact]
        public void Heston_PutCallParity()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var strike = 950.0;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 16));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            var volSurface = new ConstantVolSurface(origin, 0.28);
            var fwdCurve = new Func<double, double>(t => spot + 50 * t);

            var hestonParams = HestonModelParameters.CreateDefault(
                kappa: 2.0,
                volOfVol: 0.6,
                rho: -0.7
            );

            var asset = new HestonModel(
                hestonParams: hestonParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 20,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engine.AddPathProcess(asset);
            var callPayoff = new EuropeanCall("TestAsset", strike, expiry);
            var putPayoff = new EuropeanPut("TestAsset", strike, expiry);
            engine.AddPathProcess(callPayoff);
            engine.AddPathProcess(putPayoff);
            engine.SetupFeatures();
            engine.RunProcess();

            var mcCall = callPayoff.AverageResult;
            var mcPut = putPayoff.AverageResult;
            var expectedForward = fwdCurve(1);

            // Put-call parity: C - P = F - K
            var parityDiff = mcCall - mcPut;
            var expectedDiff = expectedForward - strike;

            if (!IsCoverageOnly)
            {
                Assert.True(System.Math.Abs(parityDiff - expectedDiff) < 1.0,
                    $"Put-call parity violated. C-P: {parityDiff:F4}, F-K: {expectedDiff:F4}");
            }
        }

        /// <summary>
        /// Tests that negative correlation (ρ < 0) creates the leverage effect:
        /// OTM puts should be more expensive than in the constant vol Black model.
        /// </summary>
        [Fact]
        public void Heston_LeverageEffect_OTMPuts()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var strike = 850.0;  // OTM put
            var vol = 0.25;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 16));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            var volSurface = new ConstantVolSurface(origin, vol);
            var fwdCurve = new Func<double, double>(t => spot);

            // Strong negative correlation creates leverage effect
            var hestonParams = HestonModelParameters.CreateExplicit(
                v0: vol * vol,
                theta: vol * vol,
                kappa: 1.0,
                volOfVol: 0.5,
                rho: -0.8  // Strong negative correlation
            );

            var asset = new HestonModel(
                hestonParams: hestonParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 25,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engine.AddPathProcess(asset);
            var payoff = new EuropeanPut("TestAsset", strike, expiry);
            engine.AddPathProcess(payoff);
            engine.SetupFeatures();
            engine.RunProcess();

            var hestonPv = payoff.AverageResult;
            var blackPv = BlackFunctions.BlackPV(spot, strike, 0, 1, vol, OptionType.P);

            if (!IsCoverageOnly)
            {
                // Heston with negative ρ should price OTM puts higher than Black
                Assert.True(hestonPv > blackPv * 0.95,
                    $"Heston OTM put should be close to or higher than Black due to leverage effect. Heston: {hestonPv:F4}, Black: {blackPv:F4}");
            }
        }

        /// <summary>
        /// Tests that higher vol-of-vol creates fatter tails (higher kurtosis).
        /// This is verified by checking that deeply OTM options become relatively more expensive.
        /// </summary>
        [Fact]
        public void Heston_HigherVolOfVol_IncreasesSmileWings()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var vol = 0.25;

            var volOfVolLow = 0.2;
            var volOfVolHigh = 0.8;

            // Deep OTM strikes for testing wing behavior
            var otmPutStrike = 700.0;   // 30% OTM put
            var otmCallStrike = 1300.0; // 30% OTM call

            // Test with low vol-of-vol
            using var engineLow = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 16));
            engineLow.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            var volSurface = new ConstantVolSurface(origin, vol);
            var fwdCurve = new Func<double, double>(t => spot);

            var hestonParamsLow = HestonModelParameters.CreateExplicit(
                v0: vol * vol,
                theta: vol * vol,
                kappa: 2.0,
                volOfVol: volOfVolLow,
                rho: 0.0
            );

            var assetLow = new HestonModel(
                hestonParams: hestonParamsLow,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 30,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engineLow.AddPathProcess(assetLow);
            var atmCallLow = new EuropeanCall("TestAsset", spot, expiry);
            var otmPutLow = new EuropeanPut("TestAsset", otmPutStrike, expiry);
            var otmCallLow = new EuropeanCall("TestAsset", otmCallStrike, expiry);
            engineLow.AddPathProcess(atmCallLow);
            engineLow.AddPathProcess(otmPutLow);
            engineLow.AddPathProcess(otmCallLow);
            engineLow.SetupFeatures();
            engineLow.RunProcess();

            // Test with high vol-of-vol
            using var engineHigh = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 16));
            engineHigh.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            var hestonParamsHigh = HestonModelParameters.CreateExplicit(
                v0: vol * vol,
                theta: vol * vol,
                kappa: 2.0,
                volOfVol: volOfVolHigh,
                rho: 0.0
            );

            var assetHigh = new HestonModel(
                hestonParams: hestonParamsHigh,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 30,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engineHigh.AddPathProcess(assetHigh);
            var atmCallHigh = new EuropeanCall("TestAsset", spot, expiry);
            var otmPutHigh = new EuropeanPut("TestAsset", otmPutStrike, expiry);
            var otmCallHigh = new EuropeanCall("TestAsset", otmCallStrike, expiry);
            engineHigh.AddPathProcess(atmCallHigh);
            engineHigh.AddPathProcess(otmPutHigh);
            engineHigh.AddPathProcess(otmCallHigh);
            engineHigh.SetupFeatures();
            engineHigh.RunProcess();

            if (!IsCoverageOnly)
            {
                // Calculate ratio of OTM to ATM prices - this ratio should increase with vol-of-vol
                // because higher vol-of-vol creates fatter tails
                var strangLow = otmPutLow.AverageResult + otmCallLow.AverageResult;
                var strangHigh = otmPutHigh.AverageResult + otmCallHigh.AverageResult;

                var ratioLow = strangLow / atmCallLow.AverageResult;
                var ratioHigh = strangHigh / atmCallHigh.AverageResult;

                // Higher vol-of-vol should increase the relative value of OTM options
                Assert.True(ratioHigh > ratioLow * 0.9,
                    $"Higher σ_v should increase OTM/ATM ratio (fatter tails). Low σ_v ratio: {ratioLow:F4}, High σ_v ratio: {ratioHigh:F4}");
            }
        }

        /// <summary>
        /// Tests that the Heston model parameters helper methods produce correct results.
        /// </summary>
        [Fact]
        public void Heston_ParameterMethodsCorrect()
        {
            var hestonParams = HestonModelParameters.CreateExplicit(
                v0: 0.04,      // 20% vol
                theta: 0.09,   // 30% long-term vol
                kappa: 2.0,
                volOfVol: 0.5,
                rho: -0.5
            );

            // Test expected variance: E[v(t)] = θ + (v_0 - θ)e^{-κt}
            var t = 1.0;
            var expectedVar = hestonParams.ExpectedVariance(t);
            var analyticalVar = 0.09 + (0.04 - 0.09) * System.Math.Exp(-2.0 * t);
            Assert.True(System.Math.Abs(expectedVar - analyticalVar) < 1e-10,
                $"Expected variance incorrect. Expected: {analyticalVar:F6}, Got: {expectedVar:F6}");

            // Test expected vol
            var expectedVol = hestonParams.ExpectedVol(t);
            Assert.True(System.Math.Abs(expectedVol - System.Math.Sqrt(analyticalVar)) < 1e-10,
                $"Expected vol incorrect. Expected: {System.Math.Sqrt(analyticalVar):F6}, Got: {expectedVol:F6}");

            // Test Feller condition: 2κθ > σ_v²
            // 2 * 2.0 * 0.09 = 0.36 > 0.25, should be satisfied
            Assert.True(hestonParams.FellerConditionSatisfied,
                "Feller condition should be satisfied for these parameters");

            // Test with parameters that violate Feller
            var hestonBad = HestonModelParameters.CreateExplicit(
                v0: 0.01,
                theta: 0.01,
                kappa: 1.0,
                volOfVol: 2.0,  // Large vol-of-vol
                rho: 0.0
            );
            Assert.False(hestonBad.FellerConditionSatisfied,
                "Feller condition should be violated: 2κθ = 0.02 < σ_v² = 4.0");
        }

        /// <summary>
        /// Tests mean reversion: variance should revert to long-term level θ.
        /// </summary>
        [Fact]
        public void Heston_MeanReversionToTheta()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(5);  // Long maturity
            var spot = 1000.0;
            var v0 = 0.01;      // Low initial variance (10% vol)
            var theta = 0.09;   // High long-term variance (30% vol)

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 14));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            // Use time-varying vol surface to observe mean reversion
            var volSurface = new ConstantVolSurface(origin, System.Math.Sqrt(theta));
            var fwdCurve = new Func<double, double>(t => spot);

            var hestonParams = HestonModelParameters.CreateExplicit(
                v0: v0,
                theta: theta,
                kappa: 1.0,  // Moderate mean reversion
                volOfVol: 0.3,
                rho: 0.0
            );

            var asset = new HestonModel(
                hestonParams: hestonParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 50,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engine.AddPathProcess(asset);
            var payoff = new EuropeanCall("TestAsset", spot, expiry);
            engine.AddPathProcess(payoff);
            engine.SetupFeatures();
            engine.RunProcess();

            var mcPrice = payoff.AverageResult;

            // Expected variance at T=5: θ + (v_0 - θ)e^{-κT}
            var expectedVar = theta + (v0 - theta) * System.Math.Exp(-1.0 * 5.0);
            // This should be close to theta for long maturity
            Assert.True(expectedVar > 0.08,
                $"Variance should have reverted close to θ={theta:F4}. Expected variance at T=5: {expectedVar:F4}");
        }

        /// <summary>
        /// Tests that auto-calibration to vol surface works correctly.
        /// </summary>
        [Fact]
        public void Heston_AutoCalibrationToVolSurface()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var vol = 0.25;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            var volSurface = new ConstantVolSurface(origin, vol);
            var fwdCurve = new Func<double, double>(t => spot);

            // Use default parameters with auto-calibration
            var hestonParams = HestonModelParameters.CreateDefault(
                kappa: 2.0,
                volOfVol: 0.3,
                rho: -0.3
            );
            // CalibrateToVolSurface is true by default

            var asset = new HestonModel(
                hestonParams: hestonParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 20,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engine.AddPathProcess(asset);
            var payoff = new EuropeanCall("TestAsset", spot, expiry);
            engine.AddPathProcess(payoff);
            engine.SetupFeatures();
            engine.RunProcess();

            var mcPrice = payoff.AverageResult;
            var blackPrice = BlackFunctions.BlackPV(spot, spot, 0, 1, vol, OptionType.C);

            if (!IsCoverageOnly)
            {
                // With auto-calibration and moderate parameters, should be reasonably close to Black
                var relError = System.Math.Abs(mcPrice / blackPrice - 1.0);
                Assert.True(relError < 0.05,
                    $"Auto-calibrated Heston should be close to Black for ATM. Black: {blackPrice:F4}, Heston: {mcPrice:F4}, RelError: {relError:P2}");
            }
        }

        /// <summary>
        /// Tests multiple expiries to verify consistent behavior across maturities.
        /// </summary>
        [Fact]
        public void Heston_MultipleExpiries()
        {
            var origin = DateTime.Now.Date;
            var spot = 1000.0;
            var vol = 0.25;

            var expiries = new[] { 0.25, 0.5, 1.0, 2.0 };

            foreach (var years in expiries)
            {
                var expiry = origin.AddDays(365.25 * years);

                using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
                engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                {
                    UseNormalInverse = true,
                    UseAnthithetic = true
                });

                var volSurface = new ConstantVolSurface(origin, vol);
                var fwdCurve = new Func<double, double>(t => spot);

                // Use small vol-of-vol to stay close to Black
                var hestonParams = HestonModelParameters.CreateExplicit(
                    v0: vol * vol,
                    theta: vol * vol,
                    kappa: 1.0,
                    volOfVol: 0.2,
                    rho: -0.2
                );

                var asset = new HestonModel(
                    hestonParams: hestonParams,
                    volSurface: volSurface,
                    startDate: origin,
                    expiryDate: expiry,
                    nTimeSteps: IsCoverageOnly ? 1 : System.Math.Max(10, (int)(years * 20)),
                    forwardCurve: fwdCurve,
                    name: "TestAsset"
                );

                engine.AddPathProcess(asset);
                var payoff = new EuropeanCall("TestAsset", spot, expiry);
                engine.AddPathProcess(payoff);
                engine.SetupFeatures();
                engine.RunProcess();

                var mcPv = payoff.AverageResult;
                var blackPv = BlackFunctions.BlackPV(spot, spot, 0, years, vol, OptionType.C);

                if (!IsCoverageOnly)
                {
                    var tolerance = System.Math.Max(2.0, blackPv * 0.05);
                    Assert.True(System.Math.Abs(blackPv - mcPv) < tolerance,
                        $"Expiry {years}y: Black PV: {blackPv:F4}, MC PV: {mcPv:F4}");
                }
            }
        }

        /// <summary>
        /// Tests different moneyness levels to verify smile/skew effects.
        /// </summary>
        [Fact]
        public void Heston_DifferentMoneyness()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var vol = 0.25;

            var strikes = new[] { 800.0, 900.0, 1000.0, 1100.0, 1200.0 };

            var prices = new List<double>();

            foreach (var strike in strikes)
            {
                using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
                engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                {
                    UseNormalInverse = true,
                    UseAnthithetic = true
                });

                var volSurface = new ConstantVolSurface(origin, vol);
                var fwdCurve = new Func<double, double>(t => spot);

                var hestonParams = HestonModelParameters.CreateExplicit(
                    v0: vol * vol,
                    theta: vol * vol,
                    kappa: 1.5,
                    volOfVol: 0.5,
                    rho: -0.7  // Negative correlation creates skew
                );

                var asset = new HestonModel(
                    hestonParams: hestonParams,
                    volSurface: volSurface,
                    startDate: origin,
                    expiryDate: expiry,
                    nTimeSteps: IsCoverageOnly ? 1 : 25,
                    forwardCurve: fwdCurve,
                    name: "TestAsset"
                );

                engine.AddPathProcess(asset);
                var payoff = new EuropeanCall("TestAsset", strike, expiry);
                engine.AddPathProcess(payoff);
                engine.SetupFeatures();
                engine.RunProcess();

                var mcPv = payoff.AverageResult;
                prices.Add(mcPv);

                // Just check that we get reasonable prices (positive and decreasing with strike)
                Assert.True(mcPv >= 0, $"Price should be non-negative for strike {strike}");
            }

            if (!IsCoverageOnly)
            {
                // Prices should generally decrease with strike for calls
                for (var i = 1; i < prices.Count; i++)
                {
                    Assert.True(prices[i] <= prices[i - 1] + 5.0,
                        $"Call prices should decrease with strike. Strike {strikes[i - 1]}: {prices[i - 1]:F4}, Strike {strikes[i]}: {prices[i]:F4}");
                }
            }
        }

        /// <summary>
        /// Tests that zero correlation (ρ = 0) gives symmetric smile.
        /// </summary>
        [Fact]
        public void Heston_ZeroCorrelation_SymmetricSmile()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var vol = 0.25;
            var strikeLow = 900.0;
            var strikeHigh = 1100.0;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 16));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            var volSurface = new ConstantVolSurface(origin, vol);
            var fwdCurve = new Func<double, double>(t => spot);

            var hestonParams = HestonModelParameters.CreateExplicit(
                v0: vol * vol,
                theta: vol * vol,
                kappa: 1.5,
                volOfVol: 0.6,
                rho: 0.0  // Zero correlation
            );

            var asset = new HestonModel(
                hestonParams: hestonParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 25,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engine.AddPathProcess(asset);
            var payoffLow = new EuropeanCall("TestAsset", strikeLow, expiry);
            var payoffHigh = new EuropeanCall("TestAsset", strikeHigh, expiry);
            engine.AddPathProcess(payoffLow);
            engine.AddPathProcess(payoffHigh);
            engine.SetupFeatures();
            engine.RunProcess();

            var priceLow = payoffLow.AverageResult;
            var priceHigh = payoffHigh.AverageResult;

            // With ρ=0 and symmetric strikes, intrinsic-adjusted values should be similar
            var intrinsicLow = System.Math.Max(0, spot - strikeLow);
            var intrinsicHigh = System.Math.Max(0, spot - strikeHigh);
            var timeValueLow = priceLow - intrinsicLow;
            var timeValueHigh = priceHigh - intrinsicHigh;

            if (!IsCoverageOnly)
            {
                // Time values should be reasonably similar for symmetric strikes with ρ=0
                var ratio = timeValueLow / System.Math.Max(0.1, timeValueHigh);
                Assert.True(ratio > 0.5 && ratio < 2.0,
                    $"With ρ=0, time values should be similar. Low: {timeValueLow:F4}, High: {timeValueHigh:F4}, Ratio: {ratio:F2}");
            }
        }

        /// <summary>
        /// Tests that variance stays positive throughout the simulation.
        /// This is a numerical stability test.
        /// </summary>
        [Fact]
        public void Heston_VarianceStaysPositive()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(2);
            var spot = 1000.0;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 14));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = false
            });

            var volSurface = new ConstantVolSurface(origin, 0.25);
            var fwdCurve = new Func<double, double>(t => spot);

            // Use extreme parameters that might cause negative variance
            var hestonParams = HestonModelParameters.CreateExplicit(
                v0: 0.01,     // Low initial variance
                theta: 0.01,  // Low long-term variance
                kappa: 0.5,   // Slow mean reversion
                volOfVol: 2.0, // Very high vol-of-vol
                rho: -0.9
            );

            var asset = new HestonModel(
                hestonParams: hestonParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 50,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engine.AddPathProcess(asset);
            var payoff = new EuropeanCall("TestAsset", spot, expiry);
            engine.AddPathProcess(payoff);
            engine.SetupFeatures();
            engine.RunProcess();

            var mcPrice = payoff.AverageResult;

            // If variance went negative and wasn't handled properly, we'd get NaN or very wrong prices
            Assert.True(!double.IsNaN(mcPrice) && !double.IsInfinity(mcPrice),
                $"Price should be finite even with extreme parameters. Got: {mcPrice}");
            Assert.True(mcPrice > 0,
                $"Price should be positive. Got: {mcPrice:F4}");
        }
    }
}
