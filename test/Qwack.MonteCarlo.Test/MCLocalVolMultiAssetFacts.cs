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
using Qwack.Dates;
using Xunit;

namespace Qwack.MonteCarlo.Test
{
    public class MCLocalVolMultiAssetFacts
    {
        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        [InlineData(-1.0)]
        [InlineData(0.5)]
        [InlineData(-0.5)]
        public void LVMCDualPathsGenerated(double correlation)
        {
            var origin = DateTime.Now.Date;
            var engine = new PathEngine(2.IntPow(12))
            {
                Parallelize = true
            };

            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
            {
                 UseNormalInverse = true,
                 UseAnthithetic = false,
            });

            engine.IncrementDepth();

            var correlMatrix = new double[][]
            {
                new double[] { 1.0, correlation },
                new double[] { correlation, 1.0 },

            };
            engine.AddPathProcess(new Cholesky(correlMatrix));

            engine.IncrementDepth();

            var tenorsStr = new[] { "1m", "2m", "3m", "6m", "9m", "1y" };
            var tenors = tenorsStr.Select(x => new Frequency(x));
            var expiries = tenors.Select(t => origin.AddPeriod(RollType.F, new Calendar(), t)).ToArray();
            var deltaKs = new[] { -0.1, -0.25, -0.5, -0.75, -0.9 };
            var smileVols = new[] { 0.32, 0.3, 0.29, 0.3, 0.32 };
            var vols = Enumerable.Repeat(smileVols, expiries.Length).ToArray();

            var volSurface = new GridVolSurface(origin, deltaKs, expiries, vols, 
                Core.Basic.StrikeType.ForwardDelta, Interpolator1DType.LinearFlatExtrap, 
                Interpolator1DType.LinearInVariance, DayCountBasis.Act365F);

            var fwdCurve1 = new Func<double, double>(t => { return 1000; });
            var asset1 = new LVSingleAsset
                (
                    startDate : origin,
                    expiryDate: origin.AddYears(1),
                    volSurface: volSurface,
                    forwardCurve: fwdCurve1,
                    nTimeSteps:365,
                    name: "TestAsset1"
                );
            var fwdCurve2 = new Func<double, double>(t => { return 1000; });
            var asset2 = new LVSingleAsset
                (
                    startDate: origin,
                    expiryDate: origin.AddYears(1),
                    volSurface: volSurface,
                    forwardCurve: fwdCurve2,
                    nTimeSteps: 365,
                    name: "TestAsset2"
                );
            engine.AddPathProcess(asset1);
            engine.AddPathProcess(asset2);

            engine.IncrementDepth();

            var correl = new Correlation("TestAsset1", "TestAsset2");  
            engine.AddPathProcess(correl);


            engine.SetupFeatures();
            engine.RunProcess();
         
            var corr = correl.AverageResult;
            var errCorr = correl.ResultStdError;

            Assert.Equal(correlation, corr, 2);
        }

        //[Theory(Skip = "Broken")]
        //[InlineData(0.3, 0.4, 0.5)]
        //public void LVMC_TriplePathsGenerated(double correlationAB, double correlationAC, double correlationBC)
        //{
        //    var origin = DateTime.Now.Date;
        //    var engine = new PathEngine(2.IntPow(15));
        //    engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
        //    {
        //        UseNormalInverse = true,
        //        UseAnthithetic = false
        //    });

        //    var correlMatrix = new double[][]
        //    {
        //        new double[] { 1.0, correlationAB, correlationAC },
        //        new double[] { correlationAB, 1.0,correlationBC },
        //        new double[] { correlationAC, correlationBC, 1.0 },
        //    };
        //    engine.AddPathProcess(new Cholesky(correlMatrix));

        //    var tenorsStr = new[] { "1m", "2m", "3m", "6m", "9m", "1y" };
        //    var tenors = tenorsStr.Select(x => new Frequency(x));
        //    var expiries = tenors.Select(t => origin.AddPeriod(RollType.F, new Calendar(), t)).ToArray();
        //    var deltaKs = new[] { -0.1, -0.25, -0.5, -0.75, -0.9 };
        //    var smileVols = new[] { 0.32, 0.3, 0.29, 0.3, 0.32 };
        //    var vols = Enumerable.Repeat(smileVols, expiries.Length).ToArray();

        //    var volSurface = new GridVolSurface(origin, deltaKs, expiries, vols,
        //        Core.Basic.StrikeType.ForwardDelta, Interpolator1DType.LinearFlatExtrap,
        //        Interpolator1DType.LinearInVariance, DayCountBasis.Act365F);

        //    var fwdCurve1 = new Func<double, double>(t => { return 1000; });
        //    var fwdCurve2 = new Func<double, double>(t => { return 1000; });
        //    var fwdCurve3 = new Func<double, double>(t => { return 1000; });

        //    var assetA = new LVSingleAsset
        //        (
        //            startDate: origin,
        //            expiryDate: origin.AddYears(1),
        //            volSurface: volSurface,
        //            forwardCurve: fwdCurve1,
        //            nTimeSteps: 365,
        //            name: "TestAssetA"
        //        );
        //    var assetB = new LVSingleAsset
        //        (
        //            startDate: origin,
        //            expiryDate: origin.AddYears(1),
        //            volSurface: volSurface,
        //            forwardCurve: fwdCurve2,
        //            nTimeSteps: 365,
        //            name: "TestAssetB"
        //        );
        //    var assetC = new LVSingleAsset
        //        (
        //            startDate: origin,
        //            expiryDate: origin.AddYears(1),
        //            volSurface: volSurface,
        //            forwardCurve: fwdCurve3,
        //            nTimeSteps: 365,
        //            name: "TestAssetC"
        //        );

        //    engine.AddPathProcess(assetA);
        //    engine.AddPathProcess(assetB);
        //    engine.AddPathProcess(assetC);

        //    var correlAB = new Correlation("TestAssetA", "TestAssetB");
        //    var correlAC = new Correlation("TestAssetA", "TestAssetC");
        //    var correlBC = new Correlation("TestAssetB", "TestAssetC");
        //    engine.AddPathProcess(correlAB);
        //    engine.AddPathProcess(correlAC);
        //    engine.AddPathProcess(correlBC);

        //    engine.SetupFeatures();
        //    engine.RunProcess();

        //    Assert.Equal(correlationAB, correlAB.AverageResult, 2);
        //    Assert.Equal(correlationAC, correlAC.AverageResult, 2);
        //    Assert.Equal(correlationBC, correlBC.AverageResult, 2);
        //}
    }
}
