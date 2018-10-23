using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Math;
using static System.Math;

namespace Qwack.Options.VolSurfaces
{
    public static class VolSurfaceEx
    {
        public static IInterpolator1D GenerateCDF(this IVolSurface surface, int numSamples, DateTime expiry, double fwd)
        {

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);
            var lowStrikeVol = surface.GetVolForDeltaStrike(0.0001, t, fwd);
            var lowStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.0001, 0, t, lowStrikeVol);
            var hiStrikeVol = surface.GetVolForDeltaStrike(0.9999, t, fwd);
            var hiStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.9999, 0, t, hiStrikeVol);

            var x = new double[numSamples + 2];
            var y = new double[numSamples + 2];

            var kStepD = (0.9998) / (numSamples+1.0);

            for (var i = 0; i < x.Length; i++)
            {
                var deltaKNew = -0.0001 - i * kStepD;
                var newStrikeVol = surface.GetVolForDeltaStrike(-deltaKNew, t, fwd);
                var k = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaKNew, 0, t, newStrikeVol);
                var newStrikeVol2 = surface.GetVolForDeltaStrike(-(deltaKNew + 0.00001), t, fwd);
                var k2 = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaKNew - 0.00001, 0, t, newStrikeVol2);
                var deltaK = k2 - k;

                if (i == 0)
                {
                    x[0] = k / 2.0;
                    y[0] = 0;
                    continue;
                }
                if (i == x.Length - 1)
                {
                    x[i] = k * 2;
                    y[i] = 1;
                    continue;
                }
                var volLow = surface.GetVolForAbsoluteStrike(k - deltaK / 2.0, t, fwd);
                var putLow = BlackFunctions.BlackPV(fwd, k - deltaK / 2.0, 0, t, volLow, OptionType.P);
                var volHi = surface.GetVolForAbsoluteStrike(k + deltaK / 2.0, t, fwd);
                var putHi = BlackFunctions.BlackPV(fwd, k + deltaK / 2.0, 0, t, volHi, OptionType.P);
                var digital = (putHi - putLow) / deltaK;
                y[i] = digital;
                x[i] = k;
            }

            return InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.LinearFlatExtrap);
        }

        public static IInterpolator1D GeneratePDF(this IVolSurface surface, int numSamples, DateTime expiry, double fwd)
        {
            var deltaK = fwd * 0.0001;

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);
            var lowStrikeVol = surface.GetVolForDeltaStrike(0.0001, t, fwd);
            var lowStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.0001, 0, t, lowStrikeVol);
            var hiStrikeVol = surface.GetVolForDeltaStrike(0.9999, t, fwd);
            var hiStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.9999, 0, t, hiStrikeVol);

            var x = new double[numSamples + 2];
            var y = new double[numSamples + 2];

            var k = lowStrike;

            var kStep = (hiStrike - lowStrike) / numSamples;
            for (var i = 0; i < x.Length; i++)
            {
                if (i == 0)
                {
                    x[0] = lowStrike/2.0;
                    y[0] = 0;
                    continue;
                }
                if (i == x.Length - 1)
                {
                    x[i] = k * 2;
                    y[i] = 0;
                    continue;
                }

                var volLow = surface.GetVolForAbsoluteStrike(k - deltaK, t, fwd);
                var callLow = BlackFunctions.BlackPV(fwd, k - deltaK, 0, t, volLow, OptionType.C);
                var volMid = surface.GetVolForAbsoluteStrike(k, t, fwd);
                var callMid = BlackFunctions.BlackPV(fwd, k, 0, t, volMid, OptionType.C);
                var volHi = surface.GetVolForAbsoluteStrike(k + deltaK, t, fwd);
                var callHi = BlackFunctions.BlackPV(fwd, k + deltaK, 0, t, volHi, OptionType.C);
                var digitalLo = (callLow - callMid) / deltaK;
                var digitalHi = (callMid - callHi) / deltaK;
                y[i] = digitalLo - digitalHi;
                x[i] = k;
                k += kStep;
            }

            var totalY = y.Sum();
            y = y.Select(v => v / totalY).ToArray();

            return InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.CubicSpline);
        }

        public static IInterpolator1D GeneratePremiumInterpolator(this IVolSurface surface, int numSamples, DateTime expiry, double fwd, OptionType cp)
        {

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);
            var lowStrikeVol = surface.GetVolForDeltaStrike(0.0001, t, fwd);
            var lowStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.0001, 0, t, lowStrikeVol);
            var hiStrikeVol = surface.GetVolForDeltaStrike(0.9999, t, fwd);
            var hiStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.9999, 0, t, hiStrikeVol);

            var x = new double[numSamples];
            var y = new double[numSamples];

            var k = lowStrike;

            var kStep = (hiStrike - lowStrike) / (numSamples+1.0);

            for (var i = 0; i < x.Length; i++)
            {
                var vol= surface.GetVolForAbsoluteStrike(k, t, fwd);
                var call = BlackFunctions.BlackPV(fwd, k, 0, t, vol, cp);
                y[i] = call;
                x[i] = k;
                k += kStep;
            }

            return InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.CubicSpline);
        }

        public static IInterpolator1D GenerateCompositeSmile(this IVolSurface surface, IVolSurface fxSurface, int numSamples, DateTime expiry, double fwdAsset, double fwdFx, double correlation)
        {
            var deltaKa = fwdAsset * 0.00001;
            var deltaKfx = fwdFx * 0.00001;

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);

            var cdfFx = fxSurface.GeneratePremiumInterpolator(numSamples*2, expiry, fwdFx, OptionType.C);
            var cdfA = surface.GeneratePremiumInterpolator(numSamples*2, expiry, fwdAsset, OptionType.C);

            var atmFx = fxSurface.GetVolForAbsoluteStrike(fwdFx, t, fwdFx);
            var atmA = surface.GetVolForAbsoluteStrike(fwdAsset, t, fwdAsset);

            var compoFwd = fwdAsset * fwdFx;
            var atmCompo = Sqrt(atmFx * atmFx + atmA * atmA + 2.0 * correlation * atmA * atmFx);
            var lowK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.0001, 0, t, atmCompo);
            var hiK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.9999, 0, t, atmCompo);
            var lowKA = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwdAsset, -0.0001, 0, t, atmA);
            var hiKA = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwdAsset, -0.9999, 0, t, atmA);


            var x = new double[numSamples];
            var y = new double[numSamples];

            var k = lowK;

            var kStep = (hiK - lowK) / numSamples;
            var kStepA = (hiKA - lowKA) / numSamples;
            var kStepFx = (hiK/hiKA - lowK/lowKA) / numSamples;

            for (var i = 0; i < x.Length; i++)
            {
                x[i] = k;
                var kA = lowKA;
                var totalP = 0.0;
                for(var j=0;j<numSamples;j++)
                {
                    var kFx = k / kA;
                    var volFx = fxSurface.GetVolForAbsoluteStrike(kFx, t, fwdFx);
                    var volA = surface.GetVolForAbsoluteStrike(kA, t, fwdAsset);
                    var volC = Sqrt(volFx * volFx + volA * volA + 2.0 * correlation * volA * volFx);
                    var fxBucketLow = kFx - deltaKfx / 2.0;
                    var fxBucketHi = kFx + deltaKfx / 2.0;
                    var assetBucketLow = kA - deltaKa / 2.0;
                    var assetBucketHi = kA + deltaKa / 2.0;
                    var pFx = cdfFx.FirstDerivative(fxBucketHi) - cdfFx.FirstDerivative(fxBucketLow);
                    var pA = cdfA.FirstDerivative(assetBucketHi) - cdfA.FirstDerivative(assetBucketLow);
                    var weight = pFx * pA;
                    y[i] += weight * volC;
                    totalP += weight;
                    kA += kStepA;
                }

                y[i] /= totalP;
                k += kStep;
            }

            return InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.LinearFlatExtrap);
        }

        public static IInterpolator1D GenerateCompositeSmile2(this IVolSurface surface, IVolSurface fxSurface, int numSamples, DateTime expiry, double fwdAsset, double fwdFx, double correlation)
        {
            var deltaKa = fwdAsset * 0.00001;
            var deltaKfx = fwdFx * 0.00001;

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);

            var cdfFx = fxSurface.GeneratePremiumInterpolator(numSamples * 2, expiry, fwdFx, OptionType.P);
            var cdfA = surface.GeneratePremiumInterpolator(numSamples * 2, expiry, fwdAsset, OptionType.P);

            var atmFx = fxSurface.GetVolForAbsoluteStrike(fwdFx, t, fwdFx);
            var atmA = surface.GetVolForAbsoluteStrike(fwdAsset, t, fwdAsset);

            var compoFwd = fwdAsset * fwdFx;
            var atmCompo = Sqrt(atmFx * atmFx + atmA * atmA + 2.0 * correlation * atmA * atmFx);
            var lowK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.0001, 0, t, atmCompo);
            var hiK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.9999, 0, t, atmCompo);
            var lowKA = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwdAsset, -0.0001, 0, t, atmA);
            var hiKA = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwdAsset, -0.9999, 0, t, atmA);


            var x = new double[numSamples + 2];
            var y = new double[numSamples + 2];

            x[0] = lowK / 2.0;
            y[0] = 0.0;
            x[numSamples + 1] = hiK * 2.0;
            y[numSamples + 1] = 0.0;

            var k = lowK;

            var kStep = (hiK - lowK) / numSamples;
            var kStepA = (hiKA - lowKA) / numSamples;
            var kStepFx = (hiK / hiKA - lowK / lowKA) / numSamples;

            for (var i = 0; i < numSamples; i++)
            {
                x[i + 1] = k;
                var kA = lowKA;
                for (var j = 0; j < numSamples; j++)
                {
                    var kFx = k / kA;

                    //var fxBucketLow = kFx - deltaKfx / 2.0;
                    //var fxBucketHi = kFx + deltaKfx / 2.0;
                    //var assetBucketLow = kA - deltaKa / 2.0;
                    //var assetBucketHi = kA + deltaKa / 2.0;
                    //var pFx = Max(1e-10, cdfFx.FirstDerivative(fxBucketHi) - cdfFx.FirstDerivative(fxBucketLow))/deltaKfx;
                    //var pA = Max(1e-10, cdfA.FirstDerivative(assetBucketHi) - cdfA.FirstDerivative(assetBucketLow))/deltaKa;

                    var pFx = cdfFx.FirstDerivative(kFx);
                    var pA = cdfA.FirstDerivative(kA);

                    var zA = Statistics.NormInv(pA);
                    var zFx = Statistics.NormInv(pFx);
                    var pC = Math.Distributions.BivariateNormal.CDF(zA, zFx, correlation);

                    y[i + 1] += pC / numSamples;
                    kA += kStepA;
                }
                k += kStep;
            }

           
            //var pdf = (LinearInterpolatorFlatExtrap)InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.LinearFlatExtrap);
            //var cdfSamples = x.Select(v => pdf.DefiniteIntegral(lowK / 2.0, v)).ToArray();
            //var maxCDF = cdfSamples.Max();
            //cdfSamples = cdfSamples.Select(v => v / maxCDF).ToArray();

            var deltaKs = new[] { 0.1, 0.25, 0.5, 0.75, 0.9 };
            var vols = Qwack.Core.Calibrators.NewtonRaphsonAssetSmileSolverFromCDF.Solve(x, y, surface.OriginDate, expiry, compoFwd, deltaKs, Interpolator1DType.CubicSpline, atmCompo);
            var compoSurface = new GridVolSurface(surface.OriginDate, deltaKs, new[] { expiry }, new double[][] { vols }, StrikeType.ForwardDelta, Interpolator1DType.CubicSpline, Interpolator1DType.Linear, DayCountBasis.Act365F);
            //var vols = cdfSamples.Select((v, ix) => BlackFunctions.BlackDigitalImpliedVol(compoFwd, x[ix], 0.0, t, v, OptionType.P)).ToArray();
            //var cdf = (LinearInterpolatorFlatExtrap)InterpolatorFactory.GetInterpolator(x, vols, Interpolator1DType.LinearFlatExtrap);
            var vols2 = x.Select(v => compoSurface.GetVolForAbsoluteStrike(v, expiry, compoFwd)).ToArray();
            var cdf = (LinearInterpolatorFlatExtrap)InterpolatorFactory.GetInterpolator(x, vols2, Interpolator1DType.LinearFlatExtrap);
            return cdf;
        }

    }
}
