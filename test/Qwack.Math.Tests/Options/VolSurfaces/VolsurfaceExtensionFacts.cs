using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Instruments.Asset;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Models.MCModels;
using Qwack.Options;
using Qwack.Options.VolSurfaces;
using Qwack.Paths;
using Qwack.Paths.Processes;
using Xunit;
using static System.Math;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Transport.BasicTypes;

namespace Qwack.Math.Tests.Options.VolSurfaces
{
    public class VolsurfaceExtensionFacts
    {
        static bool IsCoverageOnly => bool.TryParse(Environment.GetEnvironmentVariable("CoverageOnly"), out var coverageOnly) && coverageOnly;

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
            var surfaceCompo = surfaceAsset.GenerateCompositeSmileBasic(surfaceFx, 100, origin.AddYears(1), 100, 10, correl);

            var expectedVol = Sqrt(volFx * volFx + volAsset * volAsset + 2.0 * correl * volAsset * volFx);

            Assert.Equal(expectedVol, surfaceCompo.Interpolate(100.0 * 10.0), 6);
            Assert.Equal(expectedVol, surfaceCompo.Interpolate(200.0 * 10.0), 6);
            Assert.Equal(expectedVol, surfaceCompo.Interpolate(0.0 * 10.0), 6);
            Assert.Equal(expectedVol, surfaceCompo.Interpolate(101 * 10.0), 6);
        }

        [Fact]
        public void PremiumInterpolatorFacts()
        {
            var origin = new DateTime(2017, 02, 07);
            var expiry = origin.AddYears(1);
            var t = (expiry - origin).TotalDays / 365.0;
            var volAsset = 0.32;
            var fwd = 100.0;
            var surfaceAsset = new ConstantVolSurface(origin, volAsset);

            var premInterp = surfaceAsset.GeneratePremiumInterpolator(100, expiry, fwd, OptionType.P);

            var strike = fwd * 0.8;
            Assert.Equal(BlackFunctions.BlackPV(fwd, strike, 0.0, t, volAsset, OptionType.P), premInterp.Interpolate(strike),2);
        }

        [Fact(Skip ="Broken")]
        public void CompositeSmimleFacts_LocalVol()
        {
            var origin = new DateTime(2017, 02, 07);
            var expiry = origin.AddMonths(2);
            var tExp = origin.CalculateYearFraction(expiry, DayCountBasis.Act365F);
            var fwdCurveAsset = new Func<double, double>(t => { return 100; });
            var fwdCurveFx = new Func<double, double>(t => { return 15; });
            var volAsset = 0.32;
            var volFx = 0.16;
            var correl = 0.25;

            var surfaceAsset = new RiskyFlySurface(origin, new[] { volAsset }, new[] { expiry }, new[] { 0.25, 0.1 }, new[] { new[] { 0.02, 0.03 } }, new[] { new[] { 0.005, 0.007 } }, new[] { 100.0 }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.GaussianKernel, Interpolator1DType.Linear) { FlatDeltaSmileInExtreme=true};
            var surfaceFx = new RiskyFlySurface(origin, new[] { volFx }, new[] { expiry }, new[] { 0.25, 0.1 }, new[] { new[] { 0.015,0.025 } }, new[] { new[] { 0.005, 0.007 } }, new[] { 0.1 }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.GaussianKernel, Interpolator1DType.Linear) { FlatDeltaSmileInExtreme = true };



            //var surfaceAsset = new SabrVolSurface(origin, new[] { volAsset }, new[] { expiry }, new[] { 0.25, 0.1 }, new[] { new[] { 0.02, 0.03 } }, new[] { new[] { 0.005, 0.007 } }, new[] { 100.0 }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.Linear);
            //var surfaceFx = new SabrVolSurface(origin, new[] { volFx }, new[] { expiry }, new[] { 0.25, 0.1 }, new[] { new[] { 0.015, 0.025 } }, new[] { new[] { 0.005, 0.007 } }, new[] { 0.1 }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.Linear);

            //var surfaceAsset = new SVIVolSurface(origin, new[] { volAsset }, new[] { expiry }, new[] { 0.25, 0.1 }, new[] { new[] { 0.02, 0.03 } }, new[] { new[] { 0.005, 0.007 } }, new[] { 100.0 }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.Linear);
            //var surfaceFx = new SVIVolSurface(origin, new[] { volFx }, new[] { expiry }, new[] { 0.25, 0.1 }, new[] { new[] { 0.015, 0.025 } }, new[] { new[] { 0.005, 0.007 } }, new[] { 0.1 }, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.Linear);

            var invFx = new InverseFxSurface("inv", surfaceFx, TestProviderHelper.CurrencyProvider);
            var surfaceCompo = surfaceAsset.GenerateCompositeSmile(invFx, 200, expiry, 100, 1.0/15, correl);
        
            //setup MC
            using var engine = new PathEngine(2.IntPow(IsCoverageOnly?5:15));
            engine.AddPathProcess(
                new Qwack.Random.MersenneTwister.MersenneTwister64
                { UseNormalInverse = true });


            var correlMatrix = new double[][]
            {
                new double[] { 1.0, correl },
                new double[] { correl, 1.0 },
            };
            engine.AddPathProcess(new Cholesky(correlMatrix));


            var asset1 = new TurboSkewSingleAsset
                (
                    startDate: origin,
                    expiryDate: expiry,
                    volSurface: surfaceAsset,
                    forwardCurve: fwdCurveAsset,
                    nTimeSteps: 1,
                    name: "Asset"
                );

            var asset2 = new TurboSkewSingleAsset
                (
                    startDate: origin,
                    expiryDate: expiry,
                    volSurface: surfaceFx,
                    forwardCurve: fwdCurveFx,
                    nTimeSteps: 1,
                    name: "USD/ZAR"
                );
            engine.AddPathProcess(asset1);
            engine.AddPathProcess(asset2);

            var strike = 1500;
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
                AssetId = "USD/ZAR",
                CallPut = OptionType.C,
                ExpiryDate = expiry,
                PaymentCurrency = TestProviderHelper.CurrencyProvider["ZAR"],
                PaymentDate = expiry,
                Notional = 1.0,
                SpotLag = new Frequency("0b"),
                Strike = 0.1,
                FxConversionType = FxConversionType.None
            };
            var pathProduct = new AssetPathPayoff(product, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider, TestProviderHelper.CurrencyProvider["ZAR"]);
            var pathProductAsset = new AssetPathPayoff(productAsset, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider, TestProviderHelper.CurrencyProvider["ZAR"]);
            var pathProductFx = new AssetPathPayoff(productFx, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider, TestProviderHelper.CurrencyProvider["ZAR"]);
            engine.AddPathProcess(pathProduct);
            engine.AddPathProcess(pathProductAsset);
            engine.AddPathProcess(pathProductFx);

            engine.SetupFeatures();
            engine.RunProcess();
            var q = pathProduct.ResultsByPath;
            var qq = q.Average();
            var productIv = BlackFunctions.BlackImpliedVol(1500, strike, 0.0, tExp, pathProduct.AverageResult, OptionType.C);
            var productAssetIv = BlackFunctions.BlackImpliedVol(100, 100, 0.0, tExp, pathProductAsset.AverageResult, OptionType.C);
            var productFxIv = BlackFunctions.BlackImpliedVol(10, 10, 0.0, tExp, pathProductFx.AverageResult, OptionType.C);

            Assert.True(Abs(productIv - surfaceCompo.Interpolate(strike)) < 0.01);
        }
    }
}
