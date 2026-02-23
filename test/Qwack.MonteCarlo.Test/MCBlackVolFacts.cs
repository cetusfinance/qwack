using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Core.Curves.TimeProviders;
using Qwack.Dates;
using Qwack.Paths;
using Qwack.Paths.Payoffs;
using Qwack.Paths.Processes;
using Qwack.Options.VolSurfaces;
using Qwack.Math.Extensions;
using Qwack.Math.Interpolation;
using Qwack.Options;
using Xunit;
using Qwack.Transport.BasicTypes;

namespace Qwack.MonteCarlo.Test
{
    public class MCBlackVolFacts
    {
        static bool IsCoverageOnly => bool.TryParse(Environment.GetEnvironmentVariable("CoverageOnly"), out var coverageOnly) && coverageOnly;

        private static GridVolSurface BuildTermStructureSurface(DateTime origin, ITimeProvider timeProvider)
        {
            // Use an increasing vol term structure so that total variance is
            // monotonically increasing and forward variance stays positive.
            var expiries = new[] { origin.AddMonths(6), origin.AddYears(1) };
            var strikes = new[] { 0.25, 0.50, 0.75 };
            var vols = new[]
            {
                new[] { 0.20, 0.20, 0.20 },
                new[] { 0.25, 0.25, 0.25 },
            };
            var surface = new GridVolSurface
            {
                StrikeType = StrikeType.ForwardDelta,
                StrikeInterpolatorType = Interpolator1DType.LinearFlatExtrap,
                TimeInterpolatorType = Interpolator1DType.LinearInVariance,
                TimeBasis = DayCountBasis.Act365F,
                TimeProvider = timeProvider,
            };
            surface.Build(origin, strikes, expiries, vols);
            return surface;
        }

        [Fact]
        public void BlackMC_PathsGenerated()
        {
            var origin = DateTime.Now.Date;
            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                 UseNormalInverse = true,
                 UseAnthithetic = false
            });
            var volSurface = new ConstantVolSurface(origin, 0.32);
            var fwdCurve = new Func<double, double>(t => { return 900 + 100*t; });
            var asset = new BlackSingleAsset
                (
                    startDate : origin,
                    expiryDate: origin.AddYears(1),
                    volSurface: volSurface,
                    forwardCurve: fwdCurve,
                    nTimeSteps: IsCoverageOnly ? 1 : 10,
                    name: "TestAsset"
                );
            engine.AddPathProcess(asset);
            var payoff = new EuropeanPut("TestAsset", 900, origin.AddYears(1));
            var payoff2 = new EuropeanCall("TestAsset", 0, origin.AddYears(1));
            engine.AddPathProcess(payoff);
            engine.AddPathProcess(payoff2);
            engine.SetupFeatures();
            engine.RunProcess();
            var pv = payoff.AverageResult;
            var blackPv = BlackFunctions.BlackPV(1000, 900, 0, 1, 0.32, OptionType.P);
            if (!IsCoverageOnly)
            {
                Assert.True(System.Math.Abs(blackPv-pv)<1.0);
                var fwd = payoff2.AverageResult;
                Assert.True(System.Math.Abs(fwdCurve(1) / fwd - 1.0) < 0.001);
            }            

        }

        [Fact]
        public void BlackMC_CalendarTimeProvider_PriceRecovered()
        {
            var origin = new DateTime(2024, 1, 2); // Tuesday
            var expiry = new DateTime(2025, 1, 2); // Thursday
            var spot = 1000.0;
            var strike = 900.0;

            var calProvider = new CalendarTimeProvider();
            var volSurface = BuildTermStructureSurface(origin, calProvider);
            var fwdCurve = new Func<double, double>(t => spot);

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = false
            });

            var asset = new BlackSingleAsset
                (
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
            var surfaceTime = calProvider.GetYearFraction(origin, expiry);
            var atmVol = volSurface.GetForwardATMVol(0, surfaceTime);
            var blackPv = BlackFunctions.BlackPV(spot, strike, 0, surfaceTime, atmVol, OptionType.P);

            if (!IsCoverageOnly)
            {
                Assert.True(System.Math.Abs(blackPv - mcPv) < 1.0,
                    $"CalendarTimeProvider: MC PV {mcPv:F4} vs Black PV {blackPv:F4}, diff={System.Math.Abs(blackPv - mcPv):F4}");
            }
        }

        [Fact]
        public void BlackMC_BusinessDayTimeProvider_PriceRecovered()
        {
            var origin = new DateTime(2024, 1, 2); // Tuesday
            var expiry = new DateTime(2025, 1, 2); // Thursday
            var spot = 1000.0;
            var strike = 900.0;

            var cal = new Calendar
            {
                Name = "WeekdaysOnly",
                DaysToAlwaysExclude = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
            };
            var bdProvider = new BusinessDayTimeProvider(cal);
            var volSurface = BuildTermStructureSurface(origin, bdProvider);
            var fwdCurve = new Func<double, double>(t => spot);

            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = false
            });

            var asset = new BlackSingleAsset
                (
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
            var surfaceTime = bdProvider.GetYearFraction(origin, expiry);
            var atmVol = volSurface.GetForwardATMVol(0, surfaceTime);
            var blackPv = BlackFunctions.BlackPV(spot, strike, 0, surfaceTime, atmVol, OptionType.P);

            if (!IsCoverageOnly)
            {
                Assert.True(System.Math.Abs(blackPv - mcPv) < 1.0,
                    $"BusinessDayTimeProvider: MC PV {mcPv:F4} vs Black PV {blackPv:F4}, diff={System.Math.Abs(blackPv - mcPv):F4}");

                // BusinessDay time (zero weekend weight) should be less than calendar time,
                // so the Black PV with BD time should be smaller than with calendar time.
                var calProvider = new CalendarTimeProvider();
                var calSurfaceTime = calProvider.GetYearFraction(origin, expiry);
                Assert.True(surfaceTime < calSurfaceTime,
                    $"BD surface time {surfaceTime:F6} should be less than calendar surface time {calSurfaceTime:F6}");
            }
        }

        [Fact]
        public void BlackMC_DifferentTimeProviders_GiveDifferentPrices()
        {
            var origin = new DateTime(2024, 1, 2); // Tuesday
            var expiry = new DateTime(2025, 1, 2); // Thursday
            var spot = 1000.0;
            var strike = 900.0;

            // Calendar time provider
            var calProvider = new CalendarTimeProvider();
            var calSurface = BuildTermStructureSurface(origin, calProvider);

            // Business day time provider (zero weekend weight)
            var cal = new Calendar
            {
                Name = "WeekdaysOnly",
                DaysToAlwaysExclude = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
            };
            var bdProvider = new BusinessDayTimeProvider(cal);
            var bdSurface = BuildTermStructureSurface(origin, bdProvider);

            var fwdCurve = new Func<double, double>(t => spot);

            // Run MC with CalendarTimeProvider
            double calMcPv;
            {
                using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
                engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                {
                    UseNormalInverse = true,
                    UseAnthithetic = false
                });
                engine.AddPathProcess(new BlackSingleAsset(
                    volSurface: calSurface, startDate: origin, expiryDate: expiry,
                    nTimeSteps: IsCoverageOnly ? 1 : 10, forwardCurve: fwdCurve, name: "TestAsset"));
                var payoff = new EuropeanPut("TestAsset", strike, expiry);
                engine.AddPathProcess(payoff);
                engine.SetupFeatures();
                engine.RunProcess();
                calMcPv = payoff.AverageResult;
            }

            // Run MC with BusinessDayTimeProvider
            double bdMcPv;
            {
                using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 15));
                engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                {
                    UseNormalInverse = true,
                    UseAnthithetic = false
                });
                engine.AddPathProcess(new BlackSingleAsset(
                    volSurface: bdSurface, startDate: origin, expiryDate: expiry,
                    nTimeSteps: IsCoverageOnly ? 1 : 10, forwardCurve: fwdCurve, name: "TestAsset"));
                var payoff = new EuropeanPut("TestAsset", strike, expiry);
                engine.AddPathProcess(payoff);
                engine.SetupFeatures();
                engine.RunProcess();
                bdMcPv = payoff.AverageResult;
            }

            if (!IsCoverageOnly)
            {
                // Both should converge to their respective analytical prices
                var calTime = calProvider.GetYearFraction(origin, expiry);
                var bdTime = bdProvider.GetYearFraction(origin, expiry);
                var calAtmVol = calSurface.GetForwardATMVol(0, calTime);
                var bdAtmVol = bdSurface.GetForwardATMVol(0, bdTime);
                var calBlackPv = BlackFunctions.BlackPV(spot, strike, 0, calTime, calAtmVol, OptionType.P);
                var bdBlackPv = BlackFunctions.BlackPV(spot, strike, 0, bdTime, bdAtmVol, OptionType.P);

                Assert.True(System.Math.Abs(calBlackPv - calMcPv) < 1.0,
                    $"Calendar: MC {calMcPv:F4} vs Black {calBlackPv:F4}");
                Assert.True(System.Math.Abs(bdBlackPv - bdMcPv) < 1.0,
                    $"BusinessDay: MC {bdMcPv:F4} vs Black {bdBlackPv:F4}");

                // The two analytical prices should differ because surface times differ
                Assert.NotEqual(calBlackPv, bdBlackPv, 2);
                // And the MC prices should reflect that difference
                Assert.True(calMcPv > bdMcPv,
                    $"Calendar MC PV {calMcPv:F4} should exceed BD MC PV {bdMcPv:F4} due to higher effective time");
            }
        }
    }
}
