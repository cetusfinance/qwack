using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Dates;
using Qwack.Core.Basic;
using Qwack.Options.VolSurfaces;

namespace Qwack.Options.Asians
{
    public static class TurnbullWakeman
    {
        public static double PV(double forward, double knownAverage, double sigma, double K, double tAvgStart, double tExpiry, double riskFree, OptionType callPut)
        {
            if(tExpiry<=0) //work out intrinsic
            {
                return callPut == OptionType.Call ? System.Math.Max(0, knownAverage - K) : System.Math.Max(0, K - knownAverage);
            }

            var M = 2 * (System.Math.Exp(sigma * sigma * tExpiry) - System.Math.Exp(sigma * sigma * tAvgStart) * (1 + sigma * sigma * (tExpiry - tAvgStart)));
            M /= System.Math.Pow(sigma, 4.0) * (tExpiry - tAvgStart) * (tExpiry - tAvgStart);

            var sigma_a = tExpiry == 0 ? 0.0 : System.Math.Sqrt(System.Math.Log(M) / tExpiry);

            if (tAvgStart < 0)
            {
                var t2 = tExpiry - tAvgStart;
                K = K * t2 / tExpiry - knownAverage * (t2 - tExpiry) / tExpiry;

                if (K <= 0)
                {
                    if (callPut == OptionType.P)
                        return 0;

                    var expAvg = knownAverage * (t2 - tExpiry) / t2 + forward * tExpiry / t2;
                    var df = System.Math.Exp(-riskFree * tExpiry);
                    return df * expAvg;
                }
            }

            var pv = BlackFunctions.BlackPV(forward, K, riskFree, tExpiry, sigma_a, callPut);
            return pv;
        }

        public static double PV(double forward, double knownAverage, double sigma, double K, DateTime evalDate, DateTime avgStartDate, DateTime avgEndDate, double riskFree, OptionType callPut)
        {
            var tAvgStart = (avgStartDate - evalDate).Days / 365.0;
            var tExpiry = (avgEndDate - evalDate).Days / 365.0;

            return PV(forward, knownAverage, sigma, K, tAvgStart, tExpiry, riskFree, callPut);
        }

        public static double Delta(double forward, double knownAverage, double sigma, double K, double tAvgStart, double tExpiry, double riskFree, OptionType callPut)
        {
            var M = 2 * (System.Math.Exp(sigma * sigma * tExpiry) - System.Math.Exp(sigma * sigma * tAvgStart) * (1 + sigma * sigma * (tExpiry - tAvgStart)));
            M /= System.Math.Pow(sigma, 4.0) * (tExpiry - tAvgStart) * (tExpiry - tAvgStart);

            var sigma_a = System.Math.Sqrt(System.Math.Log(M) / tExpiry);

            if (tAvgStart < 0)
            {
                var t2 = tExpiry - tAvgStart;
                K = K * t2 / tExpiry - knownAverage * (t2 - tExpiry) / tExpiry;

                if (K <= 0)
                {
                    if (callPut == OptionType.P)
                        return 0;

                    var expAvg = knownAverage * (t2 - tExpiry) / t2 + forward * tExpiry / t2;
                    var df = System.Math.Exp(-riskFree * tExpiry);
                    return df * expAvg;
                }
            }

            var pv = BlackFunctions.BlackDelta(forward, K, riskFree, tExpiry, sigma_a, callPut);
            return pv;
        }

        public static double Delta(double forward, double knownAverage, double sigma, double K, DateTime evalDate, DateTime avgStartDate, DateTime avgEndDate, double riskFree, OptionType callPut)
        {
            var tAvgStart = (avgStartDate - evalDate).Days / 365.0;
            var tExpiry = (avgEndDate - evalDate).Days / 365.0;

            return Delta(forward, knownAverage, sigma, K, tAvgStart, tExpiry, riskFree, callPut);
        }

        public static double StrikeForPV(double targetPV, double forward, double knownAverage, IVolSurface volSurface, DateTime evalDate, DateTime avgStartDate, DateTime avgEndDate, double riskFree, OptionType callPut)
        {
            var minStrike = forward / 100.0;
            var maxStrike = forward * 100.0;

            var volDate = avgStartDate.Average(avgEndDate);

            Func<double, double> testFunc = (absK =>
            {
                var vol = volSurface.GetVolForAbsoluteStrike(absK, volDate, forward);
                var pv = PV(forward, knownAverage, vol, absK, evalDate, avgStartDate, avgEndDate, riskFree, callPut);
                return targetPV - pv;
            });

            var solvedStrike = Qwack.Math.Solvers.Brent.BrentsMethodSolve(testFunc, minStrike, maxStrike, 1e-8);

            return solvedStrike;
        }
    }
}
