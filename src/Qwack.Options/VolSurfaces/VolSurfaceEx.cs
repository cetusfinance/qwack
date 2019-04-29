using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Math;
using static System.Math;
using Qwack.Core.Models;
using Qwack.Utils.Parallel;
using Qwack.Math.Distributions;

namespace Qwack.Options.VolSurfaces
{
    public static class VolSurfaceEx
    {
        public static IInterpolator1D GenerateCDF(this IVolSurface surface, int numSamples, DateTime expiry, double fwd, bool returnInverse = false)
        {

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);
            var lowStrikeVol = surface.GetVolForDeltaStrike(0.00001, t, fwd);
            var lowStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.00091, 0, t, lowStrikeVol);
            var hiStrikeVol = surface.GetVolForDeltaStrike(0.99999, t, fwd);
            var hiStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.99999, 0, t, hiStrikeVol);

            var x = new double[numSamples + 2];
            var y = new double[numSamples + 2];

            var kStepD = (0.9998) / (numSamples + 1.0);

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

            return returnInverse ? 
                InterpolatorFactory.GetInterpolator(y, x, Interpolator1DType.MonotoneCubicSpline) : 
                InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.MonotoneCubicSpline);
        }

        public static IInterpolator1D GenerateCDF2(this IVolSurface surface, int numSamples, DateTime expiry, double fwd, bool returnInverse = false, double strikeScale=1.0)
        {
            var premInterp = GeneratePremiumInterpolator(surface, numSamples, expiry, fwd, OptionType.P);
            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);
            var lowStrikeVol = surface.GetVolForDeltaStrike(0.00001, t, fwd);
            var lowStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.00091, 0, t, lowStrikeVol);
            var hiStrikeVol = surface.GetVolForDeltaStrike(0.99999, t, fwd);
            var hiStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.99999, 0, t, hiStrikeVol);

            var x = new double[numSamples + 2];
            var y = new double[numSamples + 2];

            var kStepD = (0.9998) / (numSamples + 1.0);

            for (var i = 0; i < x.Length; i++)
            {
                var deltaKNew = -0.0001 - i * kStepD;
                var newStrikeVol = surface.GetVolForDeltaStrike(-deltaKNew, t, fwd);
                var k = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaKNew, 0, t, newStrikeVol);
                var digital = premInterp.FirstDerivative(k);
                y[i] = digital;
                x[i] = k * strikeScale;
            }

            return returnInverse ?
                InterpolatorFactory.GetInterpolator(y, x, Interpolator1DType.MonotoneCubicSpline) :
                InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.MonotoneCubicSpline);
        }

        public static IInterpolator1D GenerateMapper(this IVolSurface surface, int numSamples, DateTime expiry, double fwd, double blackVol)
        {

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);
            var lowStrikeVol = surface.GetVolForDeltaStrike(0.0001, t, fwd);
            var lowStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.0001, 0, t, lowStrikeVol);
            var hiStrikeVol = surface.GetVolForDeltaStrike(0.9999, t, fwd);
            var hiStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.9999, 0, t, hiStrikeVol);

            var x = new double[numSamples + 2];
            var y = new double[numSamples + 2];

            var kStepD = (0.9998) / (numSamples + 1.0);

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

            var rt = Sqrt(t);
            var stdCdfIntep = InterpolatorFactory.GetInterpolator(x.Select(q => Statistics.CumulativeNormalDistribution(Log((q-fwd)/(blackVol * fwd * rt)))).ToArray(), x, Interpolator1DType.MonotoneCubicSpline);
            for (var i = 0; i < y.Length; i++)
            {
                y[i] = stdCdfIntep.Interpolate(y[i]);
            }

            return InterpolatorFactory.GetInterpolator(y, x, Interpolator1DType.MonotoneCubicSpline);
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
                    x[0] = lowStrike / 2.0;
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
                y[i] = (digitalLo - digitalHi) / deltaK;
                x[i] = k;
                k += kStep;
            }

            var firstPass = InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.LinearFlatExtrap);
            var totalY = ((IIntegrableInterpolator)firstPass).DefiniteIntegral(x.First(), x.Last());
            y = y.Select(v => v / totalY).ToArray();
            return InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.LinearFlatExtrap);
        }

        public static IInterpolator1D GeneratePDF(this IInterpolator1D smile, int numSamples, double t, double fwd)
        {
            var deltaK = fwd * 0.0001;

            var atmVol = smile.Interpolate(fwd);
            var lowStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.0001, 0, t, atmVol);
            var hiStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.9999, 0, t, atmVol);

            var x = new double[numSamples + 2];
            var y = new double[numSamples + 2];

            var k = lowStrike;

            var kStep = (hiStrike - lowStrike) / numSamples;
            for (var i = 0; i < x.Length; i++)
            {
                if (i == 0)
                {
                    x[0] = lowStrike / 2.0;
                    y[0] = 0;
                    continue;
                }
                if (i == x.Length - 1)
                {
                    x[i] = k * 2;
                    y[i] = 0;
                    continue;
                }

                var volLow = smile.Interpolate(k - deltaK);
                var callLow = BlackFunctions.BlackPV(fwd, k - deltaK, 0, t, volLow, OptionType.C);
                var volMid = smile.Interpolate(k);
                var callMid = BlackFunctions.BlackPV(fwd, k, 0, t, volMid, OptionType.C);
                var volHi = smile.Interpolate(k + deltaK);
                var callHi = BlackFunctions.BlackPV(fwd, k + deltaK, 0, t, volHi, OptionType.C);
                var digitalLo = (callLow - callMid) / deltaK;
                var digitalHi = (callMid - callHi) / deltaK;
                y[i] = (digitalLo - digitalHi) / deltaK;
                x[i] = k;
                k += kStep;
            }

            var firstPass = InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.LinearFlatExtrap);
            var totalY = ((IIntegrableInterpolator)firstPass).DefiniteIntegral(x.First(), x.Last());
            y = y.Select(v => v / totalY).ToArray();
            return InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.LinearFlatExtrap);
        }

        public static IInterpolator1D GenerateCDF(this IInterpolator1D smile, int numSamples, double t, double fwd)
        {
            var deltaK = fwd * 0.0001;

            var atmVol = smile.Interpolate(fwd);
            var lowStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.0001, 0, t, atmVol);
            var hiStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.9999, 0, t, atmVol);

            var x = new double[numSamples + 2];
            var y = new double[numSamples + 2];

            var k = lowStrike;

            var kStep = (hiStrike - lowStrike) / numSamples;
            for (var i = 0; i < x.Length; i++)
            {
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
                var volLow = smile.Interpolate(k - deltaK / 2.0);
                var putLow = BlackFunctions.BlackPV(fwd, k - deltaK / 2.0, 0, t, volLow, OptionType.P);
                var volHi = smile.Interpolate(k + deltaK / 2.0);
                var putHi = BlackFunctions.BlackPV(fwd, k + deltaK / 2.0, 0, t, volHi, OptionType.P);
                var digital = (putHi - putLow) / deltaK;
                y[i] = digital;
                x[i] = k;
                k += kStep;
            }

            return InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.Linear);
        }

        public static IInterpolator1D GeneratePremiumInterpolator(this IVolSurface surface, int numSamples, DateTime expiry, double fwd, OptionType cp) 
            => GeneratePremiumInterpolator(surface, numSamples, surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F), fwd, cp);

        public static IInterpolator1D GeneratePremiumInterpolator(this IVolSurface surface, int numSamples, double t, double fwd, OptionType cp)
        {
            var lowStrikeVol = surface.GetVolForDeltaStrike(0.00000001, t, fwd);
            var lowStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.000001, 0, t, lowStrikeVol);
            var hiStrikeVol = surface.GetVolForDeltaStrike(0.99999999, t, fwd);
            var hiStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.999999, 0, t, hiStrikeVol);

            var x = new double[numSamples];
            var y = new double[numSamples];

            var k = lowStrike;

            var kStep = (hiStrike - lowStrike) / (numSamples + 1.0);

            for (var i = 0; i < x.Length; i++)
            {
                var vol = surface.GetVolForAbsoluteStrike(k, t, fwd);
                var call = BlackFunctions.BlackPV(fwd, k, 0, t, vol, cp);
                y[i] = call;
                x[i] = k;
                k += kStep;
            }

            return InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.MonotoneCubicSpline);
        }


        public static IInterpolator1D GenerateCompositeSmileBasic(this IVolSurface surface, IVolSurface fxSurface, int numSamples, DateTime expiry, double fwdAsset, double fwdFx, double correlation, bool deltaStrikeOutput=false)
        {
            var deltaKa = fwdAsset * 0.00001;
            var deltaKfx = fwdFx * 0.00001;

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);

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
            var kStepFx = (hiK / hiKA - lowK / lowKA) / numSamples;

            for (var i = 0; i < x.Length; i++)
            {
                x[i] = k;
                var kA = lowKA;
                var totalP = 0.0;
                for (var j = 0; j < numSamples; j++)
                {
                    var kFx = k / kA;
                    var volFx = fxSurface.GetVolForAbsoluteStrike(kFx, t, fwdFx);
                    var volA = surface.GetVolForAbsoluteStrike(kA, t, fwdAsset);
                    var volC = Sqrt(volFx * volFx + volA * volA + 2.0 * correlation * volA * volFx);
                    var fxBucketLow = kFx - deltaKfx / 2.0;
                    var fxBucketHi = kFx + deltaKfx / 2.0;
                    var assetBucketLow = kA - deltaKa / 2.0;
                    var assetBucketHi = kA + deltaKa / 2.0;
                    var weight = 1.0;
                    y[i] += weight * volC;
                    totalP += weight;
                    kA += kStepA;
                }

                y[i] /= totalP;
                k += kStep;
            }

            if(deltaStrikeOutput)
            {
                x = x.Select((q, ix) => -BlackFunctions.BlackDelta(compoFwd, q, 0.0, t, y[ix], OptionType.P)).ToArray();
            }

            return InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.LinearFlatExtrap);
        }


        public static IInterpolator1D GenerateCompositeSmile(this IVolSurface surface, IVolSurface fxSurface, int numSamples, DateTime expiry, double fwdAsset, double fwdFx, double correlation, bool strikesInDeltaSpace = false)
        {
            var deltaKa = fwdAsset * 0.00001;
            var deltaKfx = fwdFx * 0.00001;

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);

            var premFx = fxSurface.GeneratePremiumInterpolator(numSamples * 2, expiry, fwdFx, OptionType.P);
            var premA = surface.GeneratePremiumInterpolator(numSamples * 2, expiry, fwdAsset, OptionType.P);

            var atmFx = fxSurface.GetVolForAbsoluteStrike(fwdFx, t, fwdFx);
            var atmA = surface.GetVolForAbsoluteStrike(fwdAsset, t, fwdAsset);

            var compoFwd = fwdAsset * fwdFx;
            var atmCompo = Sqrt(atmFx * atmFx + atmA * atmA + 2.0 * correlation * atmA * atmFx);
            var lowK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.000001, 0, t, atmCompo);
            var hiK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.999999, 0, t, atmCompo);
            var lowKA = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwdAsset, -0.000001, 0, t, atmA);
            var hiKA = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwdAsset, -0.999999, 0, t, atmA);


            var x = new double[numSamples];
            var y = new double[numSamples];

            var k = lowK;

            var kStep = (hiK - lowK) / numSamples;
            var kStepA = (hiKA - lowKA) / numSamples;
            var kStepFx = (hiK / hiKA - lowK / lowKA) / numSamples;

            for (var i = 0; i < x.Length; i++)
            {
                var kA = lowKA;
                for (var j = 0; j < numSamples; j++)
                {
                    var kFx = k / kA;
                    var pFx = premFx.SecondDerivative(kFx);
                    var pA = premA.SecondDerivative(kA);
                    pFx = Statistics.NormInv(Min(1.0 - 1e-16, Max(pFx, 1e-16)));
                    pA = Statistics.NormInv(Min(1.0 - 1e-16, Max(pA, 1e-16)));
                    var p = Math.Distributions.BivariateNormal.PDF(pFx, pA, correlation);
                    y[i] += p;
                    kA += kStepA;
                }

                x[i] = k;

                k += kStep;
            }

            var pdf = InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.CubicSpline) as IIntegrableInterpolator;
            var pdfScale = pdf.DefiniteIntegral(x[0], x.Last());
            y = y.Select(q => q / pdfScale).ToArray();
            pdf = InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.CubicSpline) as IIntegrableInterpolator;
            var cdfy = new double[y.Length];
            for (var i=0;i<cdfy.Length;i++)
            {
                cdfy[i] = Max(0.0,pdf.DefiniteIntegral(0.0, x[i]));
            }
            //for (var i = 1; i < cdfy.Length-1; i++)
            //{
            //    if (cdfy[i] < cdfy[i - 1])
            //        cdfy[i] = cdfy[i - 1] + cdfy[i + 1] / 2;
            //}

            cdfy = cdfy.Select(c => c / cdfy.Max()).ToArray();

            var cdf = InterpolatorFactory.GetInterpolator(x, cdfy, Interpolator1DType.MonotoneCubicSpline) as IIntegrableInterpolator;
            var premy = new double[y.Length];
            for (var i = 0; i < cdfy.Length; i++)
            {
                premy[i] = cdf.DefiniteIntegral(0.0, x[i]);
            }

            //var premScale = premy.Select((p, ix) => p/(x[ix] - compoFwd)).Max();
            //premy = premy.Select(p => p * premScale).ToArray();

            //var offset = premy.Select((p, ix) => x[ix] - compoFwd - p).Max();
            //for (var i = 0; i < cdfy.Length; i++)
            //{
            //    premy[i] += offset;
            //}


            var prem = InterpolatorFactory.GetInterpolator(x, premy, Interpolator1DType.CubicSpline);
            var vols = new double[y.Length];
            for (var i = 0; i < vols.Length; i++)
            {
                vols[i] = BlackFunctions.BlackImpliedVol(compoFwd, x[i], 0.0, t, prem.Interpolate(x[i]), OptionType.P);
            }

            if (strikesInDeltaSpace)
                x = x.Select((ak, ix) => -BlackFunctions.BlackDelta(compoFwd, ak, 0.0, t, vols[ix], OptionType.P)).ToArray();

            return InterpolatorFactory.GetInterpolator(x, vols, Interpolator1DType.CubicSpline);
        }

        public static IInterpolator1D GenerateCompositeSmileViaCDF(this IVolSurface surface, IVolSurface fxSurface, int numSamples, DateTime expiry, double fwdAsset, double fwdFx, double correlation, bool strikesInDeltaSpace = false)
        {
            var deltaKa = fwdAsset * 0.00001;
            var deltaKfx = fwdFx * 0.00001;

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);

            var premFx = fxSurface.GeneratePremiumInterpolator(numSamples * 2, expiry, fwdFx, OptionType.C);
            var premA = surface.GeneratePremiumInterpolator(numSamples * 2, expiry, fwdAsset, OptionType.C);

            var atmFx = fxSurface.GetVolForAbsoluteStrike(fwdFx, t, fwdFx);
            var atmA = surface.GetVolForAbsoluteStrike(fwdAsset, t, fwdAsset);

            var compoFwd = fwdAsset * fwdFx;
            var atmCompo = Sqrt(atmFx * atmFx + atmA * atmA + 2.0 * correlation * atmA * atmFx);
            var lowK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.0001, 0, t, atmCompo) / 2.0;
            var hiK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.9999, 0, t, atmCompo) * 2.0;
            var lowKA = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwdAsset, -0.0001, 0, t, atmA) / 2.0;
            var hiKA = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwdAsset, -0.9999, 0, t, atmA) * 2.0;


            var x = new double[numSamples];
            var y = new double[numSamples];

            var k = lowK;

            var kStep = (hiK - lowK) / numSamples;
            var kStepA = (hiKA - lowKA) / numSamples;
            var kStepFx = (hiK / hiKA - lowK / lowKA) / numSamples;

            for (var i = 0; i < x.Length; i++)
            {
                var kA = lowKA;
                for (var j = 0; j < numSamples; j++)
                {
                    var kFx = k / kA;
                    var pFx = premFx.SecondDerivative(kFx);
                    var pA = premA.SecondDerivative(kA);
                    pFx = Statistics.NormInv(Min(1.0 - 1e-10, Max(pFx, 1e-10)));
                    pA = Statistics.NormInv(Min(1.0 - 1e-10, Max(pA, 1e-10)));
                    var p = BivariateNormal.CDF(pFx, pA, correlation);
                    y[i] += p;

                    kA += kStepA;
                }

                y[i] /= numSamples;
                x[i] = k;

                k += kStep;
            }


            var cdfy = y.Select(c => c / y.Max()).ToArray();
            var cdf = InterpolatorFactory.GetInterpolator(x, cdfy, Interpolator1DType.CubicSpline) as IIntegrableInterpolator;
            var premy = new double[y.Length];
            for (var i = 0; i < cdfy.Length; i++)
            {
                premy[i] = cdf.DefiniteIntegral(x[0], x[i]);
            }

            var prem = InterpolatorFactory.GetInterpolator(x, premy, Interpolator1DType.CubicSpline);
            var vols = new double[y.Length];
            for (var i = 0; i < vols.Length; i++)
            {
                vols[i] = BlackFunctions.BlackImpliedVol(compoFwd, x[i], 0.0, t, prem.Interpolate(x[i]), OptionType.P);
            }

            if (strikesInDeltaSpace)
                x = x.Select((ak, ix) => -BlackFunctions.BlackDelta(compoFwd, ak, 0.0, t, vols[ix], OptionType.P)).ToArray();

            return InterpolatorFactory.GetInterpolator(x, vols, Interpolator1DType.CubicSpline);
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
                    var pC = BivariateNormal.CDF(zA, zFx, correlation);

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

        public static IVolSurface GenerateCompositeSurface(this IAssetFxModel model, string AssetId, string FxPair, int numSamples, double correlation, bool newMethod=false)
        {
            var inSurface = model.GetVolSurface(AssetId);
            var inFxSurface = model.FundingModel.GetVolSurface(FxPair);
            var priceCurve = model.GetPriceCurve(AssetId);
            var fxPair = model.FundingModel.FxMatrix.GetFxPair(FxPair);

            var compoInterpolators = new Dictionary<DateTime, IInterpolator1D>();
            var fwds = new Dictionary<DateTime, double>();

            var locker = new object();
            ParallelUtils.Instance.Foreach(inSurface.Expiries, expiry =>
            {
                var assetFwd = priceCurve.GetPriceForFixingDate(expiry);
                var fxFwd = model.FundingModel.GetFxRate(fxPair.SpotDate(expiry), FxPair);
                var compoSmile = newMethod ?
                GenerateCompositeSmile(inSurface, inFxSurface, numSamples, expiry, assetFwd, fxFwd, correlation, true) :
                GenerateCompositeSmileBasic(inSurface, inFxSurface, numSamples, expiry, assetFwd, fxFwd, correlation, true);

                lock (locker)
                {
                    compoInterpolators.Add(expiry, compoSmile);
                    fwds.Add(expiry, assetFwd * fxFwd);
                }
            }).Wait();

            var ATMs = compoInterpolators.OrderBy(x=>x.Key).Select(x => x.Value.Interpolate(0.5)).ToArray();
            var wingDeltas = new[] { 0.25, 0.1 };
            var riskies = compoInterpolators.OrderBy(x => x.Key).Select(x => wingDeltas.Select(w => x.Value.Interpolate(1 - w) - x.Value.Interpolate(w)).ToArray()).ToArray();
            var flies = compoInterpolators.OrderBy(x => x.Key).Select(x => wingDeltas.Select(w => (x.Value.Interpolate(1 - w) + x.Value.Interpolate(w)) / 2.0 - x.Value.Interpolate(0.5)).ToArray()).ToArray();
            var outSurface = new RiskyFlySurface(model.BuildDate, ATMs, inSurface.Expiries, wingDeltas, riskies, flies, fwds.OrderBy(x=>x.Key).Select(x=>x.Value).ToArray(), WingQuoteType.Simple, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.GaussianKernel, Interpolator1DType.LinearInVariance);

            return outSurface;
        }
    }
}
