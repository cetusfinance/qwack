using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Math
{
    public static class BondUtils
    {
        public static double YieldToMaturity(double couponRate, double faceValue, double cleanPrice, double t)
            => (couponRate + (faceValue - cleanPrice) / t) / ((faceValue + cleanPrice) / 2.0);

        public static double YieldToWorst(double couponRate, double faceValue, double cleanPrice, double tMaturity, double tCall, double callPrice)
            => System.Math.Min(
                YieldToMaturity(couponRate, faceValue, cleanPrice, tMaturity),
                YieldToMaturity(couponRate, callPrice, cleanPrice, tCall)
               );

        public static double PriceFromYtm(double couponRate, double faceValue, double ytm, double t)
            => (couponRate * 2 + faceValue * (2 / t - ytm)) / (ytm + 2 / t);


        public static double PriceFromYTC(double couponRate, double callPrice, double tCall, double ytc) =>
            couponRate / 2 * ((1 - System.Math.Pow(1 + ytc / 2, -2 * tCall)) / (ytc / 2)) + callPrice / System.Math.Pow((1 + ytc / 2), 2 * tCall);

        public static double YtcFromPrice(double couponRate, double callPrice, double tCall, double cleanPrice)
        {
            var solverFn = new Func<double, double>(ytc =>
            {
                return PriceFromYTC(couponRate, callPrice, tCall, ytc) - cleanPrice;
            });

            return Solvers.Brent.BrentsMethodSolve(solverFn, 1e-6, 1, 1e-6);
        }

        public static double MacaulayDuration(double couponRate, double faceValue, double ytm, double periodsPerYear, double tMaturity, double tNext, double cleanPrice)
        {
            var nPeriods = System.Math.Round((tMaturity - tNext) * periodsPerYear);
            var couponFlow = couponRate * faceValue / periodsPerYear;
            var divisor = 1 + ytm / periodsPerYear;
            var sum = 0.0;

            for (var i = 0; i <= nPeriods; i++)
            {
                var df = System.Math.Pow(divisor, i + 1);
                var rowFlow = couponFlow / df * (tNext + i / periodsPerYear) / cleanPrice;
                if (i == nPeriods)
                {
                    rowFlow += faceValue / df * (tNext + i / periodsPerYear) / cleanPrice;
                }
                sum += rowFlow;
            }
            return sum;
        }

        public static double ModifiedDuration(double couponRate, double faceValue, double ytm, double periodsPerYear, double tMaturity, double tNext, double cleanPrice)
        {
            var mcD = MacaulayDuration(couponRate, faceValue, ytm, periodsPerYear, tMaturity, tNext, cleanPrice);
            var modD = mcD / (1 + ytm / periodsPerYear);
            return modD;
        }
    }
}
