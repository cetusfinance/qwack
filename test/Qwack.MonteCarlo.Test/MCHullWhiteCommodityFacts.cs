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
    public class MCHullWhiteCommodityFacts
    {
        static bool IsCoverageOnly => bool.TryParse(Environment.GetEnvironmentVariable("CoverageOnly"), out var coverageOnly) && coverageOnly;

        /// <summary>
        /// Tests that the Hull-White model with zero mean reversion (α=0) converges to Black-Scholes pricing.
        /// This is the fundamental limiting case where HW should exactly match Black.
        /// </summary>
        [Fact]
        public void HullWhite_ZeroMeanReversion_ConvergesToBlack()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var strike = 900.0;
            var vol = 0.32;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = false
            });

            var volSurface = new ConstantVolSurface(origin, vol);
            var fwdCurve = new Func<double, double>(t => spot + 100 * t);

            // Use zero mean reversion to get Black-equivalent behavior
            var hwParams = HullWhiteCommodityModelParameters.CreateBlackEquivalent();
            var asset = new HullWhiteCommodityModel(
                hwParams: hwParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 10,
                forwardCurve: fwdCurve,
                name: "TestAsset"
            );

            engine.AddPathProcess(asset);
            var payoff = new EuropeanPut("TestAsset", strike, expiry);
            engine.AddPathProcess(payoff);
            engine.SetupFeatures();
            engine.RunProcess();

            var mcPv = payoff.AverageResult;
            var blackPv = BlackFunctions.BlackPV(fwdCurve(1), strike, 0, 1, vol, OptionType.P);

            if (!IsCoverageOnly)
            {
                Assert.True(System.Math.Abs(blackPv - mcPv) < 1.0,
                    $"HW with α=0 should match Black. Black PV: {blackPv:F4}, MC PV: {mcPv:F4}");
            }
        }

        /// <summary>
        /// Tests that the Hull-White model correctly reproduces the forward price (forward is a martingale).
        /// Using a zero-strike call gives the undiscounted forward value.
        /// </summary>
        [Fact]
        public void HullWhite_ForwardIsMartingale()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 900.0;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = false
            });

            var volSurface = new ConstantVolSurface(origin, 0.25);
            var fwdCurve = new Func<double, double>(t => spot + 100 * t);

            var hwParams = HullWhiteCommodityModelParameters.CreateDefault(meanReversion: 0.5);
            var asset = new HullWhiteCommodityModel(
                hwParams: hwParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 10,
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
                Assert.True(relError < 0.001,
                    $"Forward should be martingale. Expected: {expectedForward:F4}, MC: {mcForward:F4}, RelError: {relError:P4}");
            }
        }

        /// <summary>
        /// Tests Hull-White model with positive mean reversion.
        /// With mean reversion, the effective volatility for options with T > τ is reduced.
        /// </summary>
        [Fact]
        public void HullWhite_WithMeanReversion_ConvergesToAnalytical()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var strike = 1000.0; // ATM
            var vol = 0.30;
            var meanReversion = 0.5;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = false
            });

            var volSurface = new ConstantVolSurface(origin, vol);
            var fwdCurve = new Func<double, double>(t => spot);

            var hwParams = HullWhiteCommodityModelParameters.CreateDefault(meanReversion: meanReversion);
            var asset = new HullWhiteCommodityModel(
                hwParams: hwParams,
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

            // For immediate delivery (τ = T), HW should recover Black pricing
            // The model is calibrated to match Black variance at delivery
            var blackCall = BlackFunctions.BlackPV(spot, strike, 0, 1, vol, OptionType.C);
            var blackPut = BlackFunctions.BlackPV(spot, strike, 0, 1, vol, OptionType.P);

            if (!IsCoverageOnly)
            {
                Assert.True(System.Math.Abs(blackCall - mcCall) < 2.0,
                    $"HW Call should be close to Black at delivery. Black: {blackCall:F4}, MC: {mcCall:F4}");
                Assert.True(System.Math.Abs(blackPut - mcPut) < 2.0,
                    $"HW Put should be close to Black at delivery. Black: {blackPut:F4}, MC: {mcPut:F4}");
            }
        }

        /// <summary>
        /// Tests put-call parity: C - P = F - K (undiscounted).
        /// This fundamental relationship should hold regardless of model parameters.
        /// </summary>
        [Fact]
        public void HullWhite_PutCallParity()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var strike = 950.0;

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = false
            });

            var volSurface = new ConstantVolSurface(origin, 0.28);
            var fwdCurve = new Func<double, double>(t => spot + 50 * t);

            var hwParams = HullWhiteCommodityModelParameters.CreateDefault(meanReversion: 1.0);
            var asset = new HullWhiteCommodityModel(
                hwParams: hwParams,
                volSurface: volSurface,
                startDate: origin,
                expiryDate: expiry,
                nTimeSteps: IsCoverageOnly ? 1 : 10,
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
        /// Tests that the Hull-White parameters helper methods produce correct analytical results.
        /// </summary>
        [Fact]
        public void HullWhite_AnalyticalFormulaTests()
        {
            var hwParams = new HullWhiteCommodityModelParameters
            {
                MeanReversion = 0.5,
                CalibrateToVolSurface = true
            };

            // Test B(t,T) function
            var B = hwParams.B(0, 1.0);
            var expectedB = (1 - System.Math.Exp(-0.5 * 1.0)) / 0.5;
            Assert.True(System.Math.Abs(B - expectedB) < 1e-10,
                $"B(0,1) incorrect. Expected: {expectedB}, Got: {B}");

            // Test forward correlation
            var corr = hwParams.ForwardCorrelation(0, 1.0, 2.0);
            var expectedCorr = System.Math.Exp(-0.5 * 1.0); // α * |T1-T2|
            Assert.True(System.Math.Abs(corr - expectedCorr) < 1e-10,
                $"Forward correlation incorrect. Expected: {expectedCorr}, Got: {corr}");

            // Test zero mean reversion gives perfect correlation
            var hwZero = HullWhiteCommodityModelParameters.CreateBlackEquivalent();
            var corrZero = hwZero.ForwardCorrelation(0, 1.0, 5.0);
            Assert.Equal(1.0, corrZero);

            // Test vol scaling factor approaches 1 as α → 0
            var scalingZero = hwZero.BlackVolScalingFactor(1.0);
            Assert.True(System.Math.Abs(scalingZero - 1.0) < 1e-10,
                $"Scaling factor should be 1 for α=0. Got: {scalingZero}");
        }

        /// <summary>
        /// Tests multiple expiries to verify consistent behavior.
        /// </summary>
        [Fact]
        public void HullWhite_MultipleExpiries()
        {
            var origin = DateTime.Now.Date;
            var spot = 1000.0;
            var vol = 0.25;

            var expiries = new[] { 0.25, 0.5, 1.0 };

            foreach (var years in expiries)
            {
                var expiry = origin.AddDays(365.25 * years);

                using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 14));
                engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                {
                    UseNormalInverse = true,
                    UseAnthithetic = false
                });

                var volSurface = new ConstantVolSurface(origin, vol);
                var fwdCurve = new Func<double, double>(t => spot);

                var hwParams = HullWhiteCommodityModelParameters.CreateBlackEquivalent();
                var asset = new HullWhiteCommodityModel(
                    hwParams: hwParams,
                    volSurface: volSurface,
                    startDate: origin,
                    expiryDate: expiry,
                    nTimeSteps: IsCoverageOnly ? 1 : 10,
                    forwardCurve: fwdCurve,
                    name: "TestAsset"
                );

                engine.AddPathProcess(asset);
                var payoff = new EuropeanCall("TestAsset", spot, expiry); // ATM call
                engine.AddPathProcess(payoff);
                engine.SetupFeatures();
                engine.RunProcess();

                var mcPv = payoff.AverageResult;
                var blackPv = BlackFunctions.BlackPV(spot, spot, 0, years, vol, OptionType.C);

                if (!IsCoverageOnly)
                {
                    var tolerance = System.Math.Max(1.0, blackPv * 0.02);
                    Assert.True(System.Math.Abs(blackPv - mcPv) < tolerance,
                        $"Expiry {years}y: Black PV: {blackPv:F4}, MC PV: {mcPv:F4}");
                }
            }
        }

        /// <summary>
        /// Tests OTM and ITM options to verify the model handles different moneyness correctly.
        /// </summary>
        [Fact]
        public void HullWhite_DifferentMoneyness()
        {
            var origin = DateTime.Now.Date;
            var expiry = origin.AddYears(1);
            var spot = 1000.0;
            var vol = 0.30;

            var strikes = new[] { 800.0, 900.0, 1000.0, 1100.0, 1200.0 }; // OTM to ITM puts

            foreach (var strike in strikes)
            {
                using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
                engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                {
                    UseNormalInverse = true,
                    UseAnthithetic = false
                });

                var volSurface = new ConstantVolSurface(origin, vol);
                var fwdCurve = new Func<double, double>(t => spot);

                var hwParams = HullWhiteCommodityModelParameters.CreateBlackEquivalent();
                var asset = new HullWhiteCommodityModel(
                    hwParams: hwParams,
                    volSurface: volSurface,
                    startDate: origin,
                    expiryDate: expiry,
                    nTimeSteps: IsCoverageOnly ? 1 : 10,
                    forwardCurve: fwdCurve,
                    name: "TestAsset"
                );

                engine.AddPathProcess(asset);
                var payoff = new EuropeanPut("TestAsset", strike, expiry);
                engine.AddPathProcess(payoff);
                engine.SetupFeatures();
                engine.RunProcess();

                var mcPv = payoff.AverageResult;
                var blackPv = BlackFunctions.BlackPV(spot, strike, 0, 1, vol, OptionType.P);

                if (!IsCoverageOnly)
                {
                    var tolerance = System.Math.Max(1.0, blackPv * 0.03);
                    Assert.True(System.Math.Abs(blackPv - mcPv) < tolerance,
                        $"Strike {strike}: Black PV: {blackPv:F4}, MC PV: {mcPv:F4}");
                }
            }
        }

        /// <summary>
        /// Tests that higher mean reversion reduces forward correlation appropriately.
        /// </summary>
        [Fact]
        public void HullWhite_MeanReversionEffect()
        {
            var hwLow = HullWhiteCommodityModelParameters.CreateDefault(meanReversion: 0.1);
            var hwHigh = HullWhiteCommodityModelParameters.CreateDefault(meanReversion: 2.0);

            // Forward correlation between 1Y and 2Y forwards
            var corrLow = hwLow.ForwardCorrelation(0, 1.0, 2.0);
            var corrHigh = hwHigh.ForwardCorrelation(0, 1.0, 2.0);

            // Higher mean reversion should give lower correlation
            Assert.True(corrHigh < corrLow,
                $"Higher α should reduce correlation. Low α corr: {corrLow:F4}, High α corr: {corrHigh:F4}");

            // Verify the formula: corr = exp(-α|T1-T2|)
            var expectedLow = System.Math.Exp(-0.1 * 1.0);
            var expectedHigh = System.Math.Exp(-2.0 * 1.0);

            Assert.True(System.Math.Abs(corrLow - expectedLow) < 1e-10);
            Assert.True(System.Math.Abs(corrHigh - expectedHigh) < 1e-10);
        }
    }
}
