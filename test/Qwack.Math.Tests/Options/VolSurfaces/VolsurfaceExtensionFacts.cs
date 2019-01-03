using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Instruments.Asset;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Core.Basic;
using Qwack.Models.MCModels;
using Qwack.Options;
using Qwack.Options.VolSurfaces;
using Qwack.Paths;
using Qwack.Paths.Processes;
using Xunit;
using static System.Math;
using Microsoft.Extensions.PlatformAbstractions;

namespace Qwack.Math.Tests.Options.VolSurfaces
{
    public class VolsurfaceExtensionFacts
    {
        private static readonly string s_directionNumbers = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "SobolDirectionNumbers.txt");

        [Fact]
        public void CompositeSmimleFacts_Trivial()
        {
            var origin = new DateTime(2017, 02, 07);
            var volAsset = 0.32;
            var volFx = 0.16;
            var correl = 0.4;
            var surfaceAsset = new ConstantVolSurface(origin, volAsset);
            var surfaceFx = new ConstantVolSurface(origin, volFx);
            var surfaceCompo = surfaceAsset.GenerateCompositeSmile(surfaceFx, 100, origin.AddYears(1), 100, 10, correl);

            var expectedVol = Sqrt(volFx * volFx + volAsset * volAsset + 2.0 * correl * volAsset * volFx);

            Assert.Equal(expectedVol, surfaceCompo.Interpolate(100.0 * 10.0), 6);
            Assert.Equal(expectedVol, surfaceCompo.Interpolate(200.0 * 10.0), 6);
            Assert.Equal(expectedVol, surfaceCompo.Interpolate(0.0 * 10.0), 6);
            Assert.Equal(expectedVol, surfaceCompo.Interpolate(101 * 10.0), 6);
        }

        [Fact(Skip ="Broken")]
        public void CompositeSmimleFacts_LocalVol()
        {
            var origin = new DateTime(2017, 02, 07);
            var expiry = origin.AddMonths(2);
            var tExp = origin.CalculateYearFraction(expiry, DayCountBasis.Act365F);
            var volAsset = 0.32;
            var volFx = 0.16;
            var correl = 0.0;
            var surfaceAsset = new RiskyFlySurface(origin, new[] { volAsset }, new[] { expiry }, new[] { 0.25 }, new[] { new[] { 0.02 } }, new[] { new[] { 0.005 } }, new[] { 100.0 }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Math.Interpolation.Interpolator1DType.CubicSpline, Math.Interpolation.Interpolator1DType.Linear);
            //var surfaceFx = new RiskyFlySurface(origin, new[] { volFx }, new[] { expiry }, new[] { 0.25 }, new[] { new[] { 0.015 } }, new[] { new[] { 0.005 } }, new[] { 0.1 }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Math.Interpolation.Interpolator1DType.CubicSpline, Math.Interpolation.Interpolator1DType.Linear);
            var surfaceFx = new ConstantVolSurface(origin, volFx);
            var surfaceCompo = surfaceAsset.GenerateCompositeSmile(surfaceFx, 200, expiry, 100, 0.10, correl);
        
            //setup MC
            var engine = new PathEngine(2.IntPow(16));
            engine.AddPathProcess(
                new Qwack.Random.MersenneTwister.MersenneTwister64
                { UseNormalInverse = true });


            var correlMatrix = new double[][]
            {
                new double[] { 1.0, correl },
                new double[] { correl, 1.0 },
            };
            engine.AddPathProcess(new Cholesky(correlMatrix));
            //engine.AddPathProcess(new SimpleCholesky(correl));

            var fwdCurveAsset = new Func<double, double>(t => { return 100; });
            var asset1 = new LVSingleAsset
                (
                    startDate: origin,
                    expiryDate: expiry,
                    volSurface: surfaceAsset,
                    forwardCurve: fwdCurveAsset,
                    nTimeSteps: 50,
                    name: "Asset"
                );
            var fwdCurveFx = new Func<double, double>(t => { return 0.1; });
            var asset2 = new LVSingleAsset
                (
                    startDate: origin,
                    expiryDate: expiry,
                    volSurface: surfaceFx,
                    forwardCurve: fwdCurveFx,
                    nTimeSteps: 50,
                    name: "ZAR/USD"
                );
            engine.AddPathProcess(asset1);
            engine.AddPathProcess(asset2);

            var strike = 1000   ;
            var product = new EuropeanOption
            {
                AssetId = "Asset",
                CallPut = OptionType.C,
                ExpiryDate = expiry,
                PaymentCurrency = TestProviderHelper.CurrencyProvider["ZAR"],
                PaymentDate = expiry,
                Notional = 1.0,
                SpotLag = new Frequency("0b"),
                Strike = strike,
                FxConversionType = FxConversionType.ConvertThenAverage
            };
            var productAsset = new EuropeanOption
            {
                AssetId = "Asset",
                CallPut = OptionType.C,
                ExpiryDate = expiry,
                PaymentCurrency = TestProviderHelper.CurrencyProvider["USD"],
                PaymentDate = expiry,
                Notional = 1.0,
                SpotLag = new Frequency("0b"),
                Strike = 100,
                FxConversionType = FxConversionType.None
            };
            var productFx = new EuropeanOption
            {
                AssetId = "ZAR/USD",
                CallPut = OptionType.C,
                ExpiryDate = expiry,
                PaymentCurrency = TestProviderHelper.CurrencyProvider["ZAR"],
                PaymentDate = expiry,
                Notional = 1.0,
                SpotLag = new Frequency("0b"),
                Strike = 0.1,
                FxConversionType = FxConversionType.None
            };
            var pathProduct = new AssetPathPayoff(product);
            var pathProductAsset = new AssetPathPayoff(productAsset);
            var pathProductFx = new AssetPathPayoff(productFx);
            engine.AddPathProcess(pathProduct);
            engine.AddPathProcess(pathProductAsset);
            engine.AddPathProcess(pathProductFx);

            engine.SetupFeatures();
            engine.RunProcess();
            var q = pathProduct.ResultsByPath;
            var qq = q.Average();
            var productIv = BlackFunctions.BlackImpliedVol(1000, 1000, 0.0, tExp, pathProduct.AverageResult, OptionType.C);
            var productAssetIv = BlackFunctions.BlackImpliedVol(100, 100, 0.0, tExp, pathProductAsset.AverageResult, OptionType.C);
            var productFxIv = BlackFunctions.BlackImpliedVol(10, 10, 0.0, tExp, pathProductFx.AverageResult, OptionType.C);

            Assert.Equal(productIv, surfaceCompo.Interpolate(strike),2);
        }
    }
}
