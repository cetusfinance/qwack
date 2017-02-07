using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Extensions;

namespace Qwack.Options
{
    /// <summary>
    /// American (and European) option pricing performed using a binomial tree
    /// Includes case of option-on-future where the forward of the underlying future is treated as flat
    /// </summary>
    public class BinomialTree
    {
        private static double u(double deltaT, double vol)
        {
            return System.Math.Exp(vol * System.Math.Sqrt(deltaT));
        }

        private static double d(double deltaT, double vol)
        {
            return 1 / u(deltaT, vol);
        }

        public static double AmericanPV(double T, double S, double K, double r, double sigma, OptionType CP, double q, int n)
        {
            return VanillaPV(T, S, K, r, sigma, CP, q, n, true);
        }

        public static double EuropeanPV(double T, double S, double K, double r, double sigma, OptionType CP, double q, int n)
        {
            return VanillaPV(T, S, K, r, sigma, CP, q, n, false);
        }

        public static double AmericanFuturePV(double T, double S, double K, double r, double sigma, OptionType CP, int n)
        {
            return VanillaPV(T, S, K, r, sigma, CP, r, n, true);
        }

        public static double EuropeanFuturePV(double T, double S, double K, double r, double sigma, OptionType CP, int n)
        {
            return VanillaPV(T, S, K, r, sigma, CP, r, n, false);
        }

        public static double VanillaPV(double T, double S, double K, double r, double sigma, OptionType CP, double q, int n, bool isAmerican)
        {
            double deltaT = T / (double)n;
            double up = System.Math.Exp(sigma * System.Math.Sqrt(deltaT));
            double p0 = (up * System.Math.Exp(-r * deltaT) - System.Math.Exp(-q * deltaT)) * up / (System.Math.Pow(up, 2) - 1);
            double p1 = System.Math.Exp(-r * deltaT) - p0;

            double[] p = new double[n + 1];
            double exercise, spot;

            //initial values at time T
            for (int i = 0; i <= n; i++)
            {
                spot = S * System.Math.Pow(up, 2 * i - n);

                if (CP == OptionType.Put)
                    p[i] = K - spot;
                else
                    p[i] = spot - K;

                if (p[i] < 0)
                    p[i] = 0;
            }

            //move back to earlier times
            for (int j = n - 1; j >= 0; j--)
            {
                for (int i = 0; i <= j; i++)
                {
                    spot = S * System.Math.Pow(up, 2 * i - j);
                    p[i] = p0 * p[i] + p1 * p[i + 1]; //binomial value

                    if (isAmerican)
                    {
                        if (CP == OptionType.Put)
                            exercise = K - spot; //exercise value
                        else
                            exercise = spot - K; //exercise value

                        if (p[i] < exercise)
                            p[i] = exercise;
                    }
                }
            }


            return p[0];
        }

        public static double AmericanFutureOptionPV(double forward, double strike, double RiskFree, double expTime, double Volatility, OptionType CP)
        {
            int n = (int)System.Math.Round(365 * expTime);
            double blackPV = BlackFunctions.BlackPV(forward, strike, RiskFree, expTime, Volatility, CP);

            if (RiskFree == 0) //american option under zero rates has no benefit to ever exercise early
                return blackPV;

            double PV = AmericanPV(expTime, forward, strike, RiskFree, Volatility, CP, RiskFree, n);


            return PV.SafeMax(blackPV);
        }

        public static object[,] AmericanFutureOption(double forward, double strike, double RiskFree, double expTime, double Volatility, OptionType CP)
        {
            object[,] objArray = new object[5, 2];

            double deltaBump = 0.0001;
            double vegaBump = 0.0001;

            double PV, PVbumped, PVbumped2;
            double delta, deltaBumped, gamma, vega, theta;

            PV = AmericanFutureOptionPV(forward, strike, RiskFree, expTime, Volatility, CP);
            objArray[0, 0] = PV;
            objArray[0, 1] = "PV";

            PVbumped = AmericanFutureOptionPV(forward * (1 + deltaBump), strike, RiskFree, expTime, Volatility, CP);
            delta = (PVbumped - PV) / (forward * deltaBump);

            objArray[1, 0] = delta;
            objArray[1, 1] = "Delta%";

            PVbumped2 = AmericanFutureOptionPV(forward * (1 + 2 * deltaBump), strike, RiskFree, expTime, Volatility, CP);
            deltaBumped = (PVbumped2 - PVbumped) / (forward * deltaBump);
            gamma = (deltaBumped - delta) / (deltaBump / 0.01);
            objArray[2, 0] = gamma;
            objArray[2, 1] = "Gamma %/%";

            PVbumped = AmericanFutureOptionPV(forward, strike, RiskFree, expTime, Volatility + vegaBump, CP);
            vega = (PVbumped - PV) / (vegaBump / 0.01);
            objArray[3, 0] = vega;
            objArray[3, 1] = "Vega";

            PVbumped = AmericanFutureOptionPV(forward, strike, RiskFree, System.Math.Max(0, expTime - 1 / 365.0), Volatility + vegaBump, CP);
            theta = PVbumped - PV;
            objArray[4, 0] = theta;
            objArray[4, 1] = "Theta";

            return objArray;
        }

        public static double AmericanFuturesOptionImpliedVol(double forward, double strike, double riskFreeRate, double expTime, double premium, OptionType CP)
        {
            Func<double, double> testBinomial = (vol =>
            {
                return AmericanFutureOptionPV(forward, strike, riskFreeRate, expTime, vol, CP) - premium;
            });

            var impliedVol = Math.Solvers.Brent.BrentsMethodSolve(testBinomial, 0.000000001, 5.0000000, 1e-10);
            return impliedVol;
        }
    }
}
