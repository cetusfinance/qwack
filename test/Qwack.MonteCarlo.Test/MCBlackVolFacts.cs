using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Paths;
using Qwack.Paths.Output;
using Qwack.Paths.Payoffs;
using Qwack.Paths.Processes;
using Qwack.Options.VolSurfaces;
using Xunit;

namespace Qwack.MonteCarlo.Test
{
    public class MCBlackVolFacts
    {
        [Fact]
        public void BlackMC_PathsGenerated()
        {
            var origin = DateTime.Now.Date;
            var engine = new PathEngine(2 << 8);
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                 UseNormalInverse = true
            });
            var volSurface = new ConstantVolSurface(origin, 0.32);
            var fwdCurve = new Func<double, double>(t => { return 1000; });
            var asset = new BlackSingleAsset
                (
                    startDate : origin,
                    expiryDate: origin.AddYears(1),
                    volSurface: volSurface,
                    forwardCurve: fwdCurve,
                    nTimeSteps: 100,
                    name: "TestAsset"
                );
            engine.AddPathProcess(asset);
            var payoff = new Put("TestAsset", 500, origin.AddYears(1));
            engine.AddPathProcess(payoff);
            engine.SetupFeatures();
            engine.RunProcess();


            var output = new OutputPathsToImage(engine,2000,1000);

        }
    }
}
