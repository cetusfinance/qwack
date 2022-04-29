using System.Collections.Generic;
using System.Linq;
using static System.Math;

namespace Qwack.Math
{
    public static class HistoricVolatility
    {
        public static double CloseToCloseVolatility(this IEnumerable<double> prices, VolatilitySamplingPeriod period = VolatilitySamplingPeriod.BusinessDaily, int nPeriodsPerSample = 1)
            => prices.Returns(true).StdDev() * GetScalingFactor(period, nPeriodsPerSample);


        /// <summary>
        /// The first advanced volatility estimator was created by Parkinson in 1980, and instead of using
        /// closing prices it uses the high and low price.One drawback of this estimator is that it assumes
        /// continuous trading, hence it underestimates the volatility as potential movements when the
        /// market is shut are ignored.
        /// </summary>
        /// <param name="highs"></param>
        /// <param name="lows"></param>
        /// <param name="period"></param>
        /// <param name="nPeriodsPerSample"></param>
        /// <returns></returns>
        public static double ParkinsonVolatility(IList<double> highs, IList<double> lows, VolatilitySamplingPeriod period = VolatilitySamplingPeriod.BusinessDaily, int nPeriodsPerSample = 1)
        {
            double n = highs.Count();
            if (n != lows.Count())
                return -1;

            var sum = 0.0;
            for (var i = 0; i < n; i++)
            {
                sum += Pow(Log(highs[i] / lows[i]), 2);
            }

            sum /= (4.0 * Log(2.0));

            return Sqrt(sum) * GetScalingFactor(period, nPeriodsPerSample);
        }

        /// <summary>
        /// Later in 1980 the Garman-Klass volatility estimator was created. It is an extension of Parkinson
        /// which includes opening and closing prices(if opening prices are not available the close from
        /// the previous day can be used instead). As overnight jumps are ignored the measure
        /// underestimates the volatility
        /// </summary>
        /// <param name="opens"></param>
        /// <param name="highs"></param>
        /// <param name="lows"></param>
        /// <param name="closes"></param>
        /// <param name="period"></param>
        /// <param name="nPeriodsPerSample"></param>
        /// <returns></returns>
        public static double GarmanKlass(IList<double> opens, IList<double> highs, IList<double> lows, IList<double> closes, VolatilitySamplingPeriod period = VolatilitySamplingPeriod.BusinessDaily, int nPeriodsPerSample = 1)
        {
            double n = highs.Count();
            if (n != lows.Count() || n != closes.Count() || n != opens.Count())
                return -1;

            var sum = 0.0;
            for (var i = 0; i < n; i++)
            {
                sum += 0.5 * Pow(Log(highs[i] / lows[i]), 2) - (2.0 * Log(2.0) - 1.0) * Pow(Log(closes[i] / opens[i]), 2);
            }

            return Sqrt(sum) * GetScalingFactor(period, nPeriodsPerSample);
        }

        /// <summary>
        /// All of the previous advanced volatility measures assume the average return (or drift) is zero.
        /// Securities which have a drift, or non-zero mean, require a more sophisticated measure of
        /// volatility.The Rogers-Satchell volatility created in the early 1990s is able to properly measure
        /// the volatility for securities with non-zero mean. It does not, however, handle jumps, hence it
        /// underestimates the volatility.
        /// </summary>
        /// <param name="opens"></param>
        /// <param name="highs"></param>
        /// <param name="lows"></param>
        /// <param name="closes"></param>
        /// <param name="period"></param>
        /// <param name="nPeriodsPerSample"></param>
        /// <returns></returns>
        public static double RogersSatchell(IList<double> opens, IList<double> highs, IList<double> lows, IList<double> closes, VolatilitySamplingPeriod period = VolatilitySamplingPeriod.BusinessDaily, int nPeriodsPerSample = 1)
        {
            double n = highs.Count();
            if (n != lows.Count() || n != closes.Count() || n != opens.Count())
                return -1;

            var sum = 0.0;
            for (var i = 0; i < n; i++)
            {
                sum += Log(highs[i] / closes[i]) * Log(highs[i] / opens[i]) + Log(lows[i] / closes[i]) * Log(lows[i] / opens[i]);
            }

            return Sqrt(sum) * GetScalingFactor(period, nPeriodsPerSample);
        }


        private static double GetScalingFactor(VolatilitySamplingPeriod period, int nPeriodsPerSample)
        {
            var freq = 1.0;
            switch (period)
            {
                case VolatilitySamplingPeriod.Annually:
                    freq = 1.0;
                    break;
                case VolatilitySamplingPeriod.HalfYearly:
                    freq = 2.0;
                    break;
                case VolatilitySamplingPeriod.Quarterly:
                    freq = 4.0;
                    break;
                case VolatilitySamplingPeriod.Monthly:
                    freq = 12.0;
                    break;
                case VolatilitySamplingPeriod.CalendarDaily:
                    freq = 365.0;
                    break;
                case VolatilitySamplingPeriod.BusinessDaily:
                    freq = 252.0;
                    break;
                case VolatilitySamplingPeriod.Hourly:
                    freq = 24.0 * 365.0;
                    break;
                case VolatilitySamplingPeriod.Minutely:
                    freq = 24.0 * 365.0 * 60;
                    break;
            }

            return Sqrt(nPeriodsPerSample / freq);
        }
    }

    public enum VolatilitySamplingPeriod
    {
        Annually,
        HalfYearly,
        Quarterly,
        Monthly,
        CalendarDaily,
        BusinessDaily,
        Hourly,
        Minutely
    }


}
