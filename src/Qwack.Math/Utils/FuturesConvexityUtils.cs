using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Dates;

namespace Qwack.Math.Utils
{
    public static class FuturesConvexityUtils
    {
        public static double CalculateConvexityAdjustment(DateTime valDate, DateTime futureExpiry, DateTime underlyingDepoExpiry, double volatility, DayCountBasis basis = DayCountBasis.Act_365F)
        {
            var t1 = basis.CalculateYearFraction(valDate, futureExpiry);
            var t2 = basis.CalculateYearFraction(valDate, underlyingDepoExpiry);
            return volatility * volatility * t1 * t2 / 2.0;
        }

        //https://www.glynholton.com/notes/convexity_bias/

        /*For short-dated Eurodollar futures—those out to a year or eighteen months, the effect is hardly noticeable, perhaps a basis point or less.
         * For longer-dated futures, convexity bias can be more pronounced, causing Eurodollar futures rates to exceed corresponding forward rates by ten basis points or more at the longest maturities.
         * The actual magnitude depends on the level and volatility of interest rates.Hull[1] provides the following approximation

            forward rate = futures rate – σ2t1t2/2	[1]
            where

                σ is the standard deviation of the underlying interest rate;
                t1 is the time(in years) until maturity of the futures contract; and
                t2 is the time(in years) until maturity of the underlying loan.

            For example, if 3-month Libor has a standard deviation of 0.012 (120 basis points), the three-year Eurodollar futures rate will be higher than the corresponding FRA rate by

                0.0122(3)(3.25)/2 = 0.0007	[2]

            or 7 basis points.The approximation assumes both the futures and FRA rates are continuously compounded.Hull cites no source and offers no justification for the formula other than to say it is based on the Ho-Lee interest rate model.

            [1] Hull, John (2002). Futures, Options and Other Derivatives, Fifth Edition, Upper Saddle River: Prentice-Hall, p. 111.
        */
    }
}
