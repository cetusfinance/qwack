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
using Qwack.Options;
using Xunit;
using Qwack.Core.Basic;


namespace Qwack.MonteCarlo.Test
{
    public class MCBlackVolFacts
    {
        [Fact]
        public void BlackMC_PathsGenerated()
        {
            var origin = DateTime.Now.Date;
            var engine = new PathEngine(2.IntPow(17)-1);
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
                    nTimeSteps:365,
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
            Assert.Equal(blackPv, pv, 0);
            var fwd = payoff2.AverageResult;
            Assert.True(System.Math.Abs(fwdCurve(1) / fwd - 1.0) < 0.001);
            //var output = new OutputPathsToImage(engine,2000,1000);

        }
    }
}
