using System;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using static System.Math;

namespace Qwack.Options.Asians
{
    public static class TurnbullWakeman
    {
        public static double PV(double forward, double knownAverage, double sigma, double K, double tAvgStart, double tExpiry, double riskFree, OptionType callPut)
        {
            if (tExpiry <= 0) //work out intrinsic
            {
                return callPut == OptionType.Call ? Max(0, knownAverage - K) : Max(0, K - knownAverage);
            }

            var tau = Max(0, tAvgStart);
            var M = 2 * (Exp(sigma * sigma * tExpiry) - Exp(sigma * sigma * tau) * (1 + sigma * sigma * (tExpiry - tau)));
            M /= Pow(sigma, 4.0) * (tExpiry - tau) * (tExpiry - tau);

            //hack to fix deep ITM options have imaginary vol


            var sigma_a = tExpiry == 0 ? 0.0 : Sqrt(Log(M) / tExpiry);

            K = AsianUtils.AdjustedStrike(K, knownAverage, tExpiry, tAvgStart);

            if (K <= 0 && tAvgStart < 0)
            {
                if (callPut == OptionType.P)
                    return 0;
                var t2 = tExpiry - tAvgStart;
                var expAvg = knownAverage * (t2 - tExpiry) / t2 + forward * tExpiry / t2;
                var df = Exp(-riskFree * tExpiry);
                return df * expAvg;
            }


            var pv = BlackFunctions.BlackPV(forward, K, riskFree, tExpiry, sigma_a, callPut);

            if (tAvgStart < 0)
            {
                pv *= tExpiry / (tExpiry - tAvgStart);
            }
            return pv;
        }

        public static double PV(double forward, double knownAverage, double sigma, double K, DateTime evalDate, DateTime avgStartDate, DateTime avgEndDate, double riskFree, OptionType callPut)
        {
            var tAvgStart = (avgStartDate - evalDate).Days / 365.0;
            var tExpiry = (avgEndDate - evalDate).Days / 365.0;

            return PV(forward, knownAverage, sigma, K, tAvgStart, tExpiry, riskFree, callPut);
        }

        public static double PV(double[] forwards, DateTime[] fixingDates, DateTime evalDate, DateTime payDate, double[] sigmas, double K, double riskFree, OptionType callPut, bool todayFixed = false)
        {
            if (payDate < evalDate) return 0.0;

            if (forwards.Length != fixingDates.Length || fixingDates.Length != sigmas.Length)
                throw new DataMisalignedException();



            var nFixed = evalDate < fixingDates.First() ? 0 :
                fixingDates.Where(x => (todayFixed ? x <= evalDate : x < evalDate)).Count();
            var nFloat = fixingDates.Length - nFixed;

            var m1 = forwards.Skip(nFixed).Average();
            var wholeAverage = forwards.Average();

            var tExpiry = evalDate.CalculateYearFraction(fixingDates.Last(), DayCountBasis.Act365F, false);
            var tPay = evalDate.CalculateYearFraction(payDate, DayCountBasis.Act365F, false);
            var df = Exp(-riskFree * tPay);

            if (tExpiry <= 0) //work out intrinsic
            {
                return df * (callPut == OptionType.Call ? Max(0, wholeAverage - K) : Max(0, K - wholeAverage));
            }

            var m2 = 0.0;
            var ts = fixingDates.Select(x => Max(0, evalDate.CalculateYearFraction(x, DayCountBasis.Act365F, false))).ToArray();

            for (var i = nFixed; i < fixingDates.Length; i++)
                for (var j = nFixed; j < fixingDates.Length; j++)
                    m2 += forwards[i] * forwards[j] * Exp(sigmas[i] * sigmas[j] * ts[Min(i, j)]);

            m2 /= nFloat * nFloat;
            var sigma_a = Sqrt(1 / tExpiry * Log(m2 / (m1 * m1)));

            var tAvgStart = evalDate.CalculateYearFraction(fixingDates.First(), DayCountBasis.Act365F, false);
            var knownAverage = nFixed == 0 ? 0.0 : forwards.Take(nFixed).Average();

            var k0 = K;
            K = AsianUtils.AdjustedStrike(K, knownAverage, tExpiry, tAvgStart);

            if (K <= 0)
            {
                return (callPut == OptionType.P) ? 0.0 : df * Max(wholeAverage - k0, 0);
            }

            var pv = BlackFunctions.BlackPV(m1, K, 0.0, tExpiry, sigma_a, callPut);

            if (tAvgStart < 0)
            {
                pv *= tExpiry / (tExpiry - tAvgStart);
            }
            return df * pv;
        }

        public static double Theta(double[] forwards, DateTime[] fixingDates, DateTime evalDate, DateTime payDate, double[] sigmas, double K, double riskFree, OptionType callPut)
        {
            if (payDate < evalDate) return 0.0;

            if (forwards.Length != fixingDates.Length || fixingDates.Length != sigmas.Length)
                throw new DataMisalignedException();

            var m1 = forwards.Average();
            var tExpiry = evalDate.CalculateYearFraction(fixingDates.Last(), DayCountBasis.Act365F);
            var tPay = evalDate.CalculateYearFraction(payDate, DayCountBasis.Act365F);
            var df = Exp(-riskFree * tPay);

            if (tExpiry <= 0) //work out intrinsic
            {
                return -riskFree * df * (callPut == OptionType.Call ? Max(0, m1 - K) : Max(0, K - m1));
            }

            var pv1 = PV(forwards, fixingDates, evalDate, payDate, sigmas, K, riskFree, callPut);
            var pv2 = PV(forwards, fixingDates, evalDate.AddDays(1), payDate, sigmas, K, riskFree, callPut);

            return (pv2 - pv1) * 365;
        }

        public static double Delta(double forward, double knownAverage, double sigma, double K, double tAvgStart, double tExpiry, double riskFree, OptionType callPut)
        {
            var tau = Max(0, tAvgStart);
            var M = 2 * (Exp(sigma * sigma * tExpiry) - Exp(sigma * sigma * tau) * (1 + sigma * sigma * (tExpiry - tau)));
            M /= Pow(sigma, 4.0) * (tExpiry - tau) * (tExpiry - tau);

            var sigma_a = Sqrt(Log(M) / tExpiry);
            K = AsianUtils.AdjustedStrike(K, knownAverage, tExpiry, tAvgStart);

            if (K <= 0)
            {
                if (callPut == OptionType.P)
                    return 0;
                var t2 = tExpiry - tAvgStart;
                var expAvg = knownAverage * (t2 - tExpiry) / t2 + forward * tExpiry / t2;
                var df = Exp(-riskFree * tExpiry);
                return df * (expAvg - K);
            }

            var delta = BlackFunctions.BlackDelta(forward, K, riskFree, tExpiry, sigma_a, callPut);
            if (tAvgStart < 0)
            {
                delta *= tExpiry / (tExpiry - tAvgStart);
            }
            return delta;
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

            var solvedStrike = Math.Solvers.Brent.BrentsMethodSolve(testFunc, minStrike, maxStrike, 1e-8);

            return solvedStrike;
        }

        public static double StrikeForPV(double targetPV, double[] forwards, DateTime[] fixingDates, IVolSurface volSurface, DateTime evalDate, DateTime payDate, double riskFree, OptionType callPut)
        {
            var minStrike = forwards.Min() / 100.0;
            var maxStrike = forwards.Max() * 100.0;

            var ixMin = Array.BinarySearch(fixingDates, evalDate);
            if (ixMin < 0) ixMin = ~ixMin;

            Func<double, double> testFunc = (absK =>
            {
                var vols = fixingDates.Select((d, ix) => ix >= ixMin ? volSurface.GetVolForAbsoluteStrike(absK, d, forwards[ix]) : 0.0).ToArray();
                var pv = PV(forwards, fixingDates, evalDate, payDate, vols, absK, riskFree, callPut);
                return targetPV - pv;
            });

            var solvedStrike = Math.Solvers.Brent.BrentsMethodSolve(testFunc, minStrike, maxStrike, 1e-8);

            return solvedStrike;
        }
    }
}
