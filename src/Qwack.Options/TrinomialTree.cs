using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Qwack.Math.Extensions;

namespace Qwack.Options
{
    /// <summary>
    /// American (and European) option pricing performed using a trinomial tree
    /// Includes case of option-on-future where the forward of the underlying future is treated as flat
    /// </summary>
    public class TrinomialTree
    {
        public static double AmericanPV(double T, double S, double K, double r, double sigma, OptionType CP, double q, int n) => VanillaPV(T, S, K, r, sigma, CP, q, n, true);

        public static double EuropeanPV(double T, double S, double K, double r, double sigma, OptionType CP, double q, int n) => VanillaPV(T, S, K, r, sigma, CP, q, n, false);

        public static double AmericanFuturePV(double T, double S, double K, double r, double sigma, OptionType CP, int n) => VanillaPV(T, S, K, r, sigma, CP, r, n, true);

        public static double EuropeanFuturePV(double T, double S, double K, double r, double sigma, OptionType CP, int n) => VanillaPV(T, S, K, r, sigma, CP, r, n, false);

        public static double VanillaPV(double T, double S, double K, double r, double sigma, OptionType CP, double q, int n, bool isAmerican)
        {
            var deltaT = T / n;

            var df1p = System.Math.Exp(-r * deltaT);


            var u = System.Math.Exp(sigma * System.Math.Sqrt(2.0 * deltaT));
            var d = System.Math.Exp(-sigma * System.Math.Sqrt(2.0 * deltaT));
            //double m = 1;

            var Z = sigma * System.Math.Sqrt(deltaT / 2.0);

            //r==q for the case of a flat fwd
            var pu = (System.Math.Exp((r - q) * deltaT / 2.0) - System.Math.Exp(-Z)) / (System.Math.Exp(Z) - System.Math.Exp(-Z));
            var pd = (System.Math.Exp(Z) - System.Math.Exp((r - q) * deltaT / 2.0)) / (System.Math.Exp(Z) - System.Math.Exp(-Z));
            pu *= pu;
            pd *= pd;
            var pm = 1 - (pu + pd);

            var p = new double[n * 2 + 1];
            double exercise, spot;

            double cp = CP == OptionType.Call ? 1 : -1;

            //initial values at time T
            for (var i = 0; i <= 2 * n; i++)
            {
                spot = S * System.Math.Pow(d, n - i);
                p[i] = System.Math.Max(0, cp * (S * System.Math.Pow(u, System.Math.Max(i - n, 0)) * System.Math.Pow(d, System.Math.Max(n - i, 0)) - K));
            }

            //move back to earlier times
            for (var j = n - 1; j >= 0; j--)
            {
                for (var i = 0; i <= j * 2; i++)
                {
                    spot = S * System.Math.Pow(u, i - j);
                    p[i] = df1p * (pd * p[i] + pm * p[i + 1] + pu * p[i + 2]);

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

        public static double VanillaPV(double T, double S, double K, IInterpolator1D r_interp, double sigma, OptionType CP, IInterpolator1D fwd_interp, int n, bool isAmerican)
        {
            var deltaT = T / (double)n;
            var u = System.Math.Exp(sigma * System.Math.Sqrt(2.0 * deltaT));
            var d = System.Math.Exp(-sigma * System.Math.Sqrt(2.0 * deltaT));

            var Z = sigma * System.Math.Sqrt(deltaT / 2.0);

            var p = new double[n * 2 + 1];
            double exercise, spot;

            double cp = CP == OptionType.Call ? 1 : -1;

            //initial values at time T
            for (var i = 0; i <= 2 * n; i++)
            {
                spot = S * System.Math.Pow(d, n - i);
                p[i] = System.Math.Max(0, cp * (S * System.Math.Pow(u, System.Math.Max(i - n, 0)) * System.Math.Pow(d, System.Math.Max(n - i, 0)) - K));
            }

            //move back to earlier times
            for (var j = n - 1; j >= 0; j--)
            {
                var t_step = T * (double)j / (double)n;
                var t_stepPrev = T * (double)(j + 1) / (double)n;
                var r = System.Math.Log(System.Math.Exp(r_interp.Interpolate(t_stepPrev) * t_stepPrev) / System.Math.Exp(r_interp.Interpolate(t_step) * t_step)) / deltaT;
                var q = r - System.Math.Log(fwd_interp.Interpolate(t_stepPrev) / fwd_interp.Interpolate(t_step)) / deltaT;
                var df1p = System.Math.Exp(-r * deltaT);

                var pu = (System.Math.Exp((r - q) * deltaT / 2.0) - System.Math.Exp(-Z)) / (System.Math.Exp(Z) - System.Math.Exp(-Z));
                var pd = (System.Math.Exp(Z) - System.Math.Exp((r - q) * deltaT / 2.0)) / (System.Math.Exp(Z) - System.Math.Exp(-Z));
                pu *= pu;
                pd *= pd;
                var pm = 1 - (pu + pd);


                for (var i = 0; i <= j * 2; i++)
                {
                    spot = S * System.Math.Pow(u, i - j);
                    p[i] = df1p * (pd * p[i] + pm * p[i + 1] + pu * p[i + 2]);

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

   
        public static double AmericanFutureOptionPV(double forward, double strike, double riskFree, double expTime, double volatility, OptionType CP)
        {
            var blackPV = BlackFunctions.BlackPV(forward, strike, riskFree, expTime, volatility, CP);
            if (riskFree == 0)
            {
                return blackPV;
            }

            var n = System.Math.Max(32, (int)System.Math.Round(365.0 / 2.0 * expTime));

            var PV = AmericanPV(expTime, forward, strike, riskFree, volatility, CP, riskFree, n);

            return PV.SafeMax(blackPV);
        }

        public static double AmericanAssetOptionPV(double forward, double strike, double riskFree, double spot, double expTime, double volatility, OptionType CP)
        {
            var blackPV = BlackFunctions.BlackPV(forward, strike, riskFree, expTime, volatility, CP);
            if (riskFree == 0)
            {
                return blackPV;
            }

            var n = System.Math.Max(32, (int)System.Math.Round(365.0 / 2.0 * expTime));

            var assetYield = riskFree - System.Math.Log(forward / spot) / expTime;

            var PV = AmericanPV(expTime, forward, strike, riskFree, volatility, CP, assetYield, n);

            return PV.SafeMax(blackPV);
        }


        public static double EuropeanFutureOptionPV(double forward, double strike, double riskFree, double expTime, double volatility, OptionType CP)
        {
            var n = System.Math.Max(32, (int)System.Math.Round(365.0 / 2.0 * expTime));
            var PV = EuropeanPV(expTime, forward, strike, riskFree, volatility, CP, riskFree, n);

            return PV;
        }


        public static object[,] AmericanFutureOption(double forward, double strike, double riskFree, double expTime, double volatility, OptionType CP)
        {
            var objArray = new object[5, 2];

            var deltaBump = 0.0001;
            var vegaBump = 0.0001;



            double PV, PVbumped, PVbumped2;
            double delta, deltaBumped, gamma, vega, theta;

            PV = AmericanFutureOptionPV(forward, strike, riskFree, expTime, volatility, CP);
            objArray[0, 0] = PV;
            objArray[0, 1] = "PV";

            PVbumped = AmericanFutureOptionPV(forward * (1 + deltaBump), strike, riskFree, expTime, volatility, CP);
            delta = (PVbumped - PV) / (forward * deltaBump);

            objArray[1, 0] = delta;
            objArray[1, 1] = "Delta%";

            PVbumped2 = AmericanFutureOptionPV(forward * (1 + 2 * deltaBump), strike, riskFree, expTime, volatility, CP);
            deltaBumped = (PVbumped2 - PVbumped) / (forward * deltaBump);
            gamma = (deltaBumped - delta) / (deltaBump / 0.01);
            objArray[2, 0] = gamma;
            objArray[2, 1] = "Gamma %/%";

            PVbumped = AmericanFutureOptionPV(forward, strike, riskFree, expTime, volatility + vegaBump, CP);
            vega = (PVbumped - PV) / (vegaBump / 0.01);
            objArray[3, 0] = vega;
            objArray[3, 1] = "Vega";

            PVbumped = AmericanFutureOptionPV(forward, strike, riskFree, System.Math.Max(0, expTime - 1 / 365.0), volatility + vegaBump, CP);
            theta = PVbumped - PV;
            objArray[4, 0] = theta;
            objArray[4, 1] = "Theta";

            return objArray;
        }

        public static double AmericanFuturesOptionImpliedVol(double forward, double strike, double riskFreeRate, double expTime, double premium, OptionType CP)
        {
            Func<double, double> testTrinomial = (vol =>
            {
                return AmericanFutureOptionPV(forward, strike, riskFreeRate, expTime, vol, CP) - premium;
            });

            var impliedVol = Math.Solvers.Brent.BrentsMethodSolve(testTrinomial, 0.000000001, 5.0000000, 1e-10);
            return impliedVol;
        }
    }
}
