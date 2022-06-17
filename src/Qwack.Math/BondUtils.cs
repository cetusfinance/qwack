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

        public static double PriceFromYTC(double couponRate, double callPrice, double tCall, double ytc) =>
            couponRate / 2 * ((1 - System.Math.Pow(1 + ytc / 2, -2 * tCall)) / (ytc / 2)) + System.Math.Pow(callPrice / (1 + ytc / 2), 2 * tCall);

        public static double YtcFromPrice(double couponRate, double callPrice, double tCall, double cleanPrice)
        {
            var solverFn = new Func<double, double>(ytc =>
            {
                return PriceFromYTC(couponRate, callPrice, tCall, ytc);
            });

            return Solvers.Brent.BrentsMethodSolve(solverFn, 1e-6, 1, 1e-6);
        }
    }
}
