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


namespace Qwack.MonteCarlo.Test
{
    public class MCTurboSkewFacts
    {
        [Fact]
        public void TSMC_PathsGenerated()
        {
            var origin = DateTime.Now.Date;
            var engine = new PathEngine(2.IntPow(17));
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                UseNormalInverse = true,
                UseAnthithetic = true
            });

            var tenorsStr = new[] { "1m", "2m", "3m", "6m", "9m", "1y" };
            var tenors = tenorsStr.Select(x => new Frequency(x));
            var expiries = tenors.Select(t => origin.AddPeriod(RollType.F, new Calendar(), t)).ToArray();
            var deltaKs = new[] { 0.1, 0.25, 0.5, 0.75, 0.9 };
            var smileVols = new[] { 0.32, 0.3, 0.29, 0.3, 0.32 };
            var vols = Enumerable.Repeat(smileVols, expiries.Length).ToArray();

            var volSurface = new GridVolSurface(origin, deltaKs, expiries, vols,
                StrikeType.ForwardDelta, Interpolator1DType.GaussianKernel,
                Interpolator1DType.LinearInVariance, DayCountBasis.Act365F);

            var fwdCurve = new Func<double, double>(t => { return 900 + 100 * t; });
            var asset = new TurboSkewSingleAsset
                (
                    startDate: origin,
                    expiryDate: origin.AddYears(1),
                    volSurface: volSurface,
                    forwardCurve: fwdCurve,
                    nTimeSteps: 1,
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
            var blackVol = volSurface.GetVolForAbsoluteStrike(900, origin.AddYears(1), fwdCurve(1.0));
            var blackPv = BlackFunctions.BlackPV(1000, 900, 0, 1, blackVol, OptionType.P);
            Assert.Equal(blackPv, pv, 0);
            var fwd = payoff2.AverageResult;
            Assert.True(System.Math.Abs(fwdCurve(1) / fwd - 1.0) < 0.001);
        }
    }
}
