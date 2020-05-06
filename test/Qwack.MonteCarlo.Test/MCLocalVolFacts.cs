using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Paths;
using Qwack.Paths.Payoffs;
using Qwack.Paths.Processes;
using Qwack.Options.VolSurfaces;
using Qwack.Math.Extensions;
using Qwack.Math.Interpolation;
using Qwack.Options;
using Qwack.Dates;
using Qwack.Core.Basic;
using Xunit;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Transport.BasicTypes;

namespace Qwack.MonteCarlo.Test
{
    [CollectionDefinition("MCTests", DisableParallelization = true)]
    public class MCBLocalVolFacts
    {
        static bool IsCoverageOnly => bool.TryParse(Environment.GetEnvironmentVariable("CoverageOnly"), out var coverageOnly) && coverageOnly;

        private static readonly string s_directionNumbers = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "SobolDirectionNumbers.txt");

        [Fact]
        public void LVMC_PathsGenerated()
        {
            var origin = DateTime.Now.Date;
            using var engine = new PathEngine(2.IntPow(IsCoverageOnly ? 6 : 12))
            {
                Parallelize = false
            };

            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            var tenorsStr = new[] { "1m", "2m", "3m", "6m" };
            var tenors = tenorsStr.Select(x => new Frequency(x));
            var expiries = tenors.Select(t => origin.AddPeriod(RollType.F, new Calendar(), t)).ToArray();
            var deltaKs = new[] { 0.1, 0.25, 0.5, 0.75, 0.9 };
            var smileVols = new[] { 0.32, 0.3, 0.29, 0.3, 0.32 };
            var vols = Enumerable.Repeat(smileVols, expiries.Length).ToArray();
            var tExp = (origin.AddMonths(6) - origin).TotalDays / 365.0;
            var volSurface = new GridVolSurface(origin, deltaKs, expiries, vols,
                StrikeType.ForwardDelta, Interpolator1DType.GaussianKernel,
                Interpolator1DType.LinearInVariance, DayCountBasis.Act365F);

            var fwdCurve = new Func<double, double>(t => { return 900 + 100 * t/ tExp; });
            var asset = new LVSingleAsset
                (
                    startDate: origin,
                    expiryDate: origin.AddMonths(6),
                    volSurface: volSurface,
                    forwardCurve: fwdCurve,
                    nTimeSteps: IsCoverageOnly ? 3 : 100,
                    name: "TestAsset"
                );
            engine.AddPathProcess(asset);
            var payoff = new EuropeanPut("TestAsset", 900, origin.AddMonths(6));
            var payoff2 = new EuropeanCall("TestAsset", 0, origin.AddMonths(6));
            engine.AddPathProcess(payoff);
            engine.AddPathProcess(payoff2);
            engine.SetupFeatures();
            engine.RunProcess();

            var pv = payoff.AverageResult;
            var blackVol = volSurface.GetVolForAbsoluteStrike(900, origin.AddMonths(6), fwdCurve(tExp));
            var blackPv = BlackFunctions.BlackPV(fwdCurve(tExp), 900, 0, tExp, blackVol, OptionType.P);

            if (!IsCoverageOnly)
            {
                Assert.True(System.Math.Abs(blackPv / pv - 1.0) < 0.02);
                var fwd = payoff2.AverageResult;
                Assert.True(System.Math.Abs(fwdCurve(tExp) / fwd - 1.0) < 0.005);
            }

        }
    }
}
