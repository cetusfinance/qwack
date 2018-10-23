using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using static System.Math;

namespace Qwack.Core.Calibrators
{
    public class NewtonRaphsonAssetSmileSolverFromCDF
    {
        private const double _digiBump = 0.000001;
        public static double[] Solve(double[] digitalStrikes, double[] digitalPVs, DateTime buildDate, 
            DateTime expiry, double fwd, double[] strikesToFit, Interpolator1DType interpType, double initialGuess)
        {
            var tExp = (expiry - buildDate).TotalDays / 365.0;
            var objective = new Func<double[], double[]>(input => 
            {
                var o = new double[digitalPVs.Length];
                var smile = InterpolatorFactory.GetInterpolator(strikesToFit, input, interpType);
                for(var i=0;i<digitalStrikes.Length;i++)
                {
                    var volA = GetVolForAbsoluteStrike(digitalStrikes[i],tExp,fwd,smile);
                    var volAUp = GetVolForAbsoluteStrike(digitalStrikes[i]+_digiBump, tExp, fwd, smile);
                    var putA = BlackPV(fwd, digitalStrikes[i], 0.0, tExp, volA, OptionType.P);
                    var putAUp = BlackPV(fwd, digitalStrikes[i] + _digiBump, 0.0, tExp, volAUp, OptionType.P);
                    var smileDigi = (putAUp - putA) / _digiBump;
                    var error = (smileDigi - digitalPVs[i]);
                    var vega = BlackVega(fwd, digitalStrikes[i], 0.0, tExp, smile.Interpolate(digitalStrikes[i]));
                    o[i] = error * error * vega;
                }
                return o;
            });
            var solver = new Math.Solvers.GaussNewton
            {
                InitialGuess = Enumerable.Repeat(initialGuess, strikesToFit.Length).ToArray(),
                ObjectiveFunction = objective
            };
            var smileVols = solver.Solve();
            return smileVols;
        }

        private static double BlackVega(double forward, double strike, double riskFreeRate, double expTime, double volatility)
        {
            var d = (Log(forward / strike) + ((expTime / 2.0) * Pow(volatility, 2.0))) / (volatility * Sqrt(expTime));
            var num5 = Exp(-riskFreeRate * expTime);
            return (((forward * num5) * Statistics.Phi(d)) * Sqrt(expTime)) / 100.0;
        }

        private static double BlackPV(double forward, double strike, double riskFreeRate, double expTime, double volatility, OptionType CP)
        {
            var cpf = (CP == OptionType.Put) ? -1.0 : 1.0;

            var d1 = (Log(forward / strike) + (expTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expTime));
            var d2 = d1 - volatility * Sqrt(expTime);

            var num2 = (Log(forward / strike) + ((expTime / 2.0) * Pow(volatility, 2.0))) / (volatility * Sqrt(expTime));
            var num3 = num2 - (volatility * Sqrt(expTime));
            return (Exp(-riskFreeRate * expTime) * (((cpf * forward) * Statistics.NormSDist(num2 * cpf)) - ((cpf * strike) * Statistics.NormSDist(num3 * cpf))));
        }

        private static double AbsoluteStrikefromDeltaKAnalytic(double forward, double delta, double riskFreeRate, double expTime, double volatility)
        {
            double psi = Sign(delta);
            var sqrtT = Sqrt(expTime);
            var q = Statistics.NormInv(psi * delta);
            return forward * Exp(-psi * volatility * sqrtT * q + 0.5 * Pow(volatility, 2) * expTime);
        }

        private static double GetVolForAbsoluteStrike(double strike, double maturity, double forward, IInterpolator1D interp)
        {
            var fwd = forward;
            var cp = strike < 0 ? OptionType.Put : OptionType.Call;
            Func<double, double> testFunc = (deltaK =>
            {
                var vol = interp.Interpolate(deltaK);
                var absK = AbsoluteStrikefromDeltaKAnalytic(fwd, -deltaK, 0, maturity, vol);
                return absK - strike;
            });
            var solvedDeltaStrike = Qwack.Math.Solvers.Brent.BrentsMethodSolve(testFunc, 0.000000001, 0.999999999, 1e-8);
            return interp.Interpolate(solvedDeltaStrike);

        }
    }
}

