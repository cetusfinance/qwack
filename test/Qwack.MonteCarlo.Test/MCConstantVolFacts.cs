using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Options;
using Qwack.Paths;
using Qwack.Paths.Payoffs;
using Qwack.Paths.Processes;
using Qwack.Math.Extensions;
using Xunit;
using Qwack.Transport.BasicTypes;

namespace Qwack.MonteCarlo.Test
{
    public class MCConstantVolFacts
    {
        static bool IsCoverageOnly => bool.TryParse(Environment.GetEnvironmentVariable("CoverageOnly"), out var coverageOnly) && coverageOnly;

        [Fact]
        public void PathsGenerated()
        {
            var vol = 0.32;

            using var engine = new PathEngine(IsCoverageOnly ? 2.IntPow(6) : 2 << 10);
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                 UseNormalInverse = true
            });
            var asset2 = new ConstantVolSingleAsset
                (
                    startDate: DateTime.Now.Date,
                    expiry: DateTime.Now.Date.AddYears(1),
                    vol: vol,
                    spot: 500,
                    drift: 0.00,
                    numberOfSteps: IsCoverageOnly ? 1 : 365,
                    name: "TestAsset2"
                );

            engine.AddPathProcess(asset2);
            var payoff = new EuropeanPut("TestAsset2", 500, DateTime.Now.Date.AddYears(1));
            engine.AddPathProcess(payoff);
            engine.SetupFeatures();
            engine.RunProcess();

            var pv = payoff.AverageResult;
            var blackPv = BlackFunctions.BlackPV(500, 500, 0, 1, vol, OptionType.P);
            if (!IsCoverageOnly)
                Assert.Equal(blackPv, pv, 0);
        }
    }
}
