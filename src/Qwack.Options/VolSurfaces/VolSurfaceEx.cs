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
using System.Collections.Concurrent;
using Qwack.Transport.BasicTypes;

namespace Qwack.Options.VolSurfaces
{
    public static class VolSurfaceEx
    {
        public static IInterpolator1D GenerateCDF(this IVolSurface surface, int numSamples, DateTime expiry, double fwd, bool returnInverse = false)
        {
            var deltaKLow = 0.0000001;
            var deltaKHi = 0.9999999;
            var kStepD = (deltaKHi - deltaKLow) / (numSamples+3);
            var deltaKBump = deltaKLow / 10;

            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);

            var x = new double[numSamples + 2];
            var y = new double[numSamples + 2];


            for (var i = 0; i < x.Length; i++)
            {
                var deltaKNew = deltaKLow + i * kStepD;
                var mStrike = deltaKNew - deltaKBump/2;
                var pStrike = deltaKNew + deltaKBump/2;

                var mStrikeVol = surface.GetVolForDeltaStrike(mStrike, t, fwd);
                var mk = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -mStrike, 0, t, mStrikeVol);
                var pStrikeVol = surface.GetVolForDeltaStrike(pStrike, t, fwd);
                var pk = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -pStrike, 0, t, pStrikeVol);

                if (i == 0)
                {
                    x[0] = mk / 2.0;
                    y[0] = 0;
                    continue;
                }
                if (i == x.Length - 1)
                {
                    x[i] = pk * 2;
                    y[i] = 1;
                    continue;
                }

                var dkAbs = (pk - mk);

                var pPut = BlackFunctions.BlackPV(fwd, pk, 0, t, pStrikeVol, OptionType.P);
                var mPut = BlackFunctions.BlackPV(fwd, mk, 0, t, mStrikeVol, OptionType.P);
                

                var digital = (pPut - mPut) / dkAbs;
                y[i] = digital;
                x[i] = (mk + pk) / 2.0;
            }

            return returnInverse ? 
                InterpolatorFactory.GetInterpolator(y, x, Interpolator1DType.LinearFlatExtrap) : 
                InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.LinearFlatExtrap);
        }

        public static IInterpolator1D GenerateCDF2(this IVolSurface surface, int numSamples, DateTime expiry, double fwd, bool returnInverse = false, double strikeScale=1.0, bool logStrikes=false)
        {
            var premInterp = GeneratePremiumInterpolator(surface, numSamples, expiry, fwd, OptionType.P);
            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);

            var x = new double[numSamples];
            var y = new double[numSamples];

            var deltaKLow = 0.0000000001;
            var deltaKHi = 0.9999999999;
            var kStepD = (deltaKHi-deltaKLow) / numSamples;

            for (var i = 0; i < x.Length; i++)
            {
                var deltaKNew = -deltaKLow - i * kStepD;
                var newStrikeVol = surface.GetVolForDeltaStrike(-deltaKNew, t, fwd);
                var k = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaKNew, 0, t, newStrikeVol);
                var digital = premInterp.FirstDerivative(k);
                y[i] = digital;
                x[i] = k * strikeScale;
                if (logStrikes)
                    x[i] = Log(x[i]);
            }

            return returnInverse ?
                InterpolatorFactory.GetInterpolator(y, x, Interpolator1DType.MonotoneCubicSpline) :
                InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.MonotoneCubicSpline);
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

        public static double InverseCDFex(this IVolSurface surface, double t, double fwd, double p)
        {
            var deltaK = fwd * 1e-10;
            var lowGuess = fwd / 2;
            var highGuess = fwd * 2;

            var targetFunc = new Func<double, double>(k =>
                 {
                     var volM = surface.GetVolForAbsoluteStrike(k - deltaK, t, fwd);
                     var volP = surface.GetVolForAbsoluteStrike(k + deltaK, t, fwd);
                     var pvM = BlackFunctions.BlackPV(fwd, k - deltaK, 0.0, t, volM, OptionType.P);
                     var pvP = BlackFunctions.BlackPV(fwd, k + deltaK, 0.0, t, volP, OptionType.P);
                     var digi = (pvP - pvM) / (2 * deltaK);
                     //var digi = BlackFunctions.BlackDigitalPV(fwd, k, 0, t, surface.GetVolForAbsoluteStrike(k, t, fwd), OptionType.P);
                     return p - digi;
                 });

            var breakCount = 0;
            while (targetFunc(lowGuess) < 0)
            {
               // highGuess = lowGuess*2.0;
                lowGuess /= 2.0;
                breakCount++;
                if (breakCount == 10)
                    return lowGuess;
            }
            breakCount = 0;
            while (targetFunc(highGuess) > 0)
            {
                //lowGuess = highGuess/2.0;
                highGuess *= 2.0;
                breakCount++;
                if (breakCount == 10)
                    return highGuess;
            }

            var b = Math.Solvers.Brent.BrentsMethodSolve(targetFunc, lowGuess, highGuess, 1e-8);
            //var b = Math.Solvers.Newton1D.MethodSolve2(targetFunc, fwd, 1e-6, 1000, fwd * 0.00001);
            if (double.IsInfinity(b) || double.IsNaN(b))
                throw new Exception("Invalid strike found");
            //if (b==lowGuess || b==highGuess)
            //    throw new Exception("Strike outside of bounds");

            return b;
        }

        public static double InverseCDF(IInterpolator1D putPremiumInterp, double t, double fwd, double p)
        {
            var deltaK = fwd * 1e-8;
            var targetFunc = new Func<double, double>(k =>
            {
                var digi = putPremiumInterp.FirstDerivative(k);
                return p - digi;
            });
            var targetFunc2 = new Func<double, double>(k => -putPremiumInterp.SecondDerivative(k));

            //var b = Math.Solvers.Brent.BrentsMethodSolve(targetFunc, lowGuess, highGuess, 1e-6);
            var b = Math.Solvers.Newton1D.MethodSolve(targetFunc,targetFunc2, fwd, 1e-6, 1000);
            if (double.IsInfinity(b) || double.IsNaN(b))
                throw new Exception("Invalid strike found");

            return b;
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
            var lowStrikeVol = surface.GetVolForDeltaStrike(0.001, t, fwd);
            var lowStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.001, 0, t, lowStrikeVol);
            var hiStrikeVol = surface.GetVolForDeltaStrike(0.999, t, fwd);
            var hiStrike = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.999, 0, t, hiStrikeVol);

            var x = new double[numSamples + 2];
            var y = new double[numSamples + 2];

            var k = lowStrike;

            var kStep = (hiStrike - lowStrike) / (numSamples + 1.0);

            var vol = surface.GetVolForAbsoluteStrike(k/10, t, fwd);
            var call = BlackFunctions.BlackPV(fwd, k/10, 0, t, vol, cp);
            y[0] = call;
            x[0] = k/10;

            for (var i = 0; i < x.Length-1; i++)
            {
                vol = surface.GetVolForAbsoluteStrike(k, t, fwd);
                call = BlackFunctions.BlackPV(fwd, k, 0, t, vol, cp);
                y[i+1] = call;
                x[i+1] = k;
                k += kStep;
            }

            vol = surface.GetVolForAbsoluteStrike(k * 10, t, fwd);
            call = BlackFunctions.BlackPV(fwd, k * 10, 0, t, vol, cp);
            y[x.Length-1] = call;
            x[x.Length - 1] = k*10;

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

        public static IInterpolator1D GenerateCompositeSmile(this IVolSurface surface, IVolSurface fxSurface, int numSamples, DateTime expiry, double fwdAsset, double fwdFx, double rho, bool strikesInDeltaSpace = false)
        {
            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);

            var atmFx = fxSurface.GetVolForDeltaStrike(0.5, t, fwdFx);
            var atmA = surface.GetVolForDeltaStrike(0.5, t, fwdAsset);

            var compoFwd = fwdAsset / fwdFx;
            var atmCompo = Sqrt(atmFx * atmFx + atmA * atmA + 2.0 * rho * atmA * atmFx);
            var lowK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.01, 0, t, atmCompo);
            var hiK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.99, 0, t, atmCompo);

            var nuA = Sqrt(t) * atmA;
            var nuFx = Sqrt(t) * atmFx;

            var cdfFx = new Func<double, double>(k => fxSurface.CDF(expiry, fwdFx, Exp(k)));
            var cdfA = new Func<double, double>(k => surface.CDF(expiry, fwdAsset, Exp(k)));

            var fxCDFCache = new Dictionary<double, double>();
            var assetCDFCache = new Dictionary<double, double>();
            var yFx = new Func<double, double>(z =>
            {
                if (fxCDFCache.TryGetValue(z, out var K)) return K;
                K = Log(fxSurface.InverseCDF(expiry, fwdFx, Statistics.NormSDist(z)));
                fxCDFCache.Add(z, K);
                return K;
            });
            var yA = new Func<double, double>(z =>
            {
                if (assetCDFCache.TryGetValue(z, out var K)) return K;
                K = Log(surface.InverseCDF(expiry, fwdAsset, Statistics.NormSDist(z)));
                assetCDFCache.Add(z, K);
                return K;
            });

            //var zfxS = new Func<double, double, double>((zA, K) => Statistics.NormInv(Max(1e-18, Min(1.0 - 1e-18, cdfFx(yA(zA) - Log(K))))));
            //var zAs = new Func<double, double, double>((zFx, K) => Statistics.NormInv(Max(1e-18, Min(1.0 - 1e-18, cdfA(yFx(zFx) + Log(K))))));
            var zfxS = new Func<double, double, double>((zA, K) => Statistics.NormInv(cdfFx(yA(zA) - Log(K))));
            var zAs = new Func<double, double, double>((zFx, K) => Statistics.NormInv(cdfA(yFx(zFx) + Log(K))));

            var d = -1.0;
            var p2 = 1.0 / Sqrt(2.0 * PI);
            //var I1 = new Func<double, double, double>((zA, K) => 
            //p2*Exp(yA(zA) - (nuA * zA - nuA * nuA / 2)) * Statistics.NormSDist(d * (zfxS(zA, K) - rho * zA) / Sqrt(1 - rho * rho)) * Exp(-(zA - nuA) * (zA - nuA) / 2.0)
            //    );
            //var I2 = new Func<double, double, double>((zFx, K) => 
            //p2*Exp(yFx(zFx) - (nuFx * zFx - nuFx * nuFx / 2)) * Statistics.NormSDist(-d * (zAs(zFx, K) - rho * zFx) / Sqrt(1 - rho * rho)) * Exp(-(zFx - nuFx) * (zFx - nuFx) / 2.0)
            //    );
            var I1 = new Func<double, double, double>((zA, K) =>
                p2 * Exp(yA(zA)) * Statistics.NormSDist(d * (zfxS(zA, K) - rho * zA) / Sqrt(1 - rho * rho)) * Exp(-(zA * zA) / 2.0)
                );
            var I2 = new Func<double, double, double>((zFx, K) =>
            p2 * Exp(yFx(zFx)) * Statistics.NormSDist(-d * (zAs(zFx, K) - rho * zFx) / Sqrt(1 - rho * rho)) * Exp(-(zFx * zFx) / 2.0)
                );


            var kStep = (hiK - lowK) / numSamples;
            var ks = Enumerable.Range(0, numSamples).Select(kk => lowK + kk * kStep).ToArray();
            var premiums = new double[ks.Length];
            var vols = new double[ks.Length];
            for (var i = 0; i < ks.Length; i++)
            {
                var I1k = new Func<double, double>(z => I1(z, ks[i]));
                var I2k = new Func<double, double>(z => I2(z, ks[i]));

                //var i1 = Integration.GaussLegendre(I1k, -5, 5, 16);
                //var i2 = Integration.GaussLegendre(I2k, -5, 5, 16);
                var i1 = Integration.SimpsonsRule(I1k, -5, 5, numSamples);
                var i2 = Integration.SimpsonsRule(I2k, -5, 5, numSamples);
                var pk = d * (i1 - ks[i] * i2);
                pk /= fwdFx;
                var volK = BlackFunctions.BlackImpliedVol(compoFwd, ks[i], 0.0, t, pk, OptionType.P);
                vols[i] = volK;
                premiums[i] = pk;
            }


            if (strikesInDeltaSpace)
                ks = ks.Select((ak, ix) => -BlackFunctions.BlackDelta(compoFwd, ak, 0.0, t, vols[ix], OptionType.P)).ToArray();

            return InterpolatorFactory.GetInterpolator(ks, vols, Interpolator1DType.CubicSpline);
        }

        public static IInterpolator1D GenerateCompositeSmileB(this IVolSurface surface, IVolSurface fxSurface, int numSamples, DateTime expiry, double fwdAsset, double fwdFx, double correlation, bool strikesInDeltaSpace = false)
        {
            var t = surface.OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);
            var fxInv = new InverseFxSurface("fxInv", fxSurface as IATMVolSurface, null);

            var atmFx = fxSurface.GetVolForDeltaStrike(0.5, t, fwdFx);
            var atmA = surface.GetVolForDeltaStrike(0.5, t, fwdAsset);

            var compoFwd = fwdAsset * fwdFx;
            var atmCompo = Sqrt(atmFx * atmFx + atmA * atmA + 2.0 * correlation * atmA * atmFx);
            var lowK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.01, 0, t, atmCompo);
            var hiK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(compoFwd, -0.99, 0, t, atmCompo);

            //var cdfInvFx = fxSurface.GenerateCDF2(numSamples * 10, expiry, fwdFx, true);
            //var cdfInvAsset = surface.GenerateCDF2(numSamples * 10, expiry, fwdAsset, true);
            //var yFx = new Func<double, double>(z => cdfInvFx.Interpolate(Statistics.NormSDist(z)));
            //var yAsset = new Func<double, double>(z => cdfInvAsset.Interpolate(Statistics.NormSDist(z)));

            var fxCDFCache = new Dictionary<double, double>();
            var assetCDFCache = new Dictionary<double, double>();
            var yFx = new Func<double, double>(z =>
            {
                if (fxCDFCache.TryGetValue(z, out var K)) return K;
                K = fxInv.InverseCDF(expiry, 1.0/fwdFx, Statistics.NormSDist(z));
                fxCDFCache.Add(z, K);
                return K;
            });
            var yAsset = new Func<double, double>(z =>
            {
                if (assetCDFCache.TryGetValue(z, out var K)) return K;
                K = surface.InverseCDF(expiry, fwdAsset, Statistics.NormSDist(z));
                assetCDFCache.Add(z, K);
                return K;
            });

            //var fxCDFCache = new Dictionary<double, double>();
            //var assetCDFCache = new Dictionary<double, double>();
            //var putFx = fxInv.GeneratePremiumInterpolator(numSamples * 10, expiry, 1.0/fwdFx, OptionType.P);
            //var putAsset = surface.GeneratePremiumInterpolator(numSamples * 10, expiry, fwdAsset, OptionType.P);
            //var yFx = new Func<double, double>(z =>
            //{
            //    if (fxCDFCache.TryGetValue(z, out var K)) return K;
            //    K = InverseCDF(putFx, t, 1.0/fwdFx, Statistics.NormSDist(z));
            //    fxCDFCache.Add(z, K);
            //    return K;
            //});
            //var yAsset = new Func<double, double>(z =>
            //{
            //    if (assetCDFCache.TryGetValue(z, out var K)) return K;
            //    K = InverseCDF(putAsset, t, fwdAsset, Statistics.NormSDist(z));
            //    var kl = assetCDFCache.Keys.ToList();
            //    var closerIx = kl.BinarySearch(z);
            //    var keyIx = ~closerIx;
            //    if (closerIx < 0 && z < 0 && kl.Count > keyIx)
            //    {
            //        if (assetCDFCache[kl[keyIx]] < K)
            //            K = assetCDFCache[kl[keyIx]];
            //    }
            //    assetCDFCache.Add(z, K);
            //    return K;
            //});

            var payoff = new Func<double, double, double, double>((z1, z2, kQ) => Max(kQ * yFx(z2) - yAsset(z1), 0));
            var integrand = new Func<double, double, double, double>((z1, z2, kQ) => payoff(z1, z2, kQ) * BivariateNormal.PDF(z1, z2, -correlation));
                       
            var kStep = (hiK - lowK) / numSamples;
            var ks = Enumerable.Range(0, numSamples).Select(kk => lowK + kk * kStep).ToArray();
            var premiums = new double[ks.Length];
            var vols = new double[ks.Length];
            for (var i = 0; i < ks.Length; i++)
            {
                var ik = new Func<double, double, double>((z1, z2) => integrand(z1, z2, ks[i]));
                var pk = Integration.TwoDimensionalGaussLegendre(ik, -5, 5, -5, 5, 16);
                //var pk = Integration.TwoDimensionalSimpsons(ik, -5, 5, -5, 5, 100);
                pk *= fwdFx;
                var volK = BlackFunctions.BlackImpliedVol(compoFwd, ks[i], 0.0, t, pk, OptionType.P);
                vols[i] = volK;
                premiums[i] = pk;
            }


            if (strikesInDeltaSpace)
                ks = ks.Select((ak, ix) => -BlackFunctions.BlackDelta(compoFwd, ak, 0.0, t, vols[ix], OptionType.P)).ToArray();

            return InterpolatorFactory.GetInterpolator(ks, vols, Interpolator1DType.CubicSpline);
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
