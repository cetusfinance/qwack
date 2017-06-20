using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math;
using static System.Math;

namespace Qwack.Options
{
    /// <summary>
    /// Functions for pricing and risking vanilla European options using the Black '76 formula
    /// </summary>
    public class BlackFunctions
    {
        public static double BlackPV(double forward, double strike, double riskFreeRate, double expTime, double volatility, OptionType CP)
        {
            var cpf = (CP == OptionType.Put) ? -1.0 : 1.0;

            var d1 = (Log(forward / strike) + (expTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expTime));
            var d2 = d1 - volatility * Sqrt(expTime);

            var num2 = (Log(forward / strike) + ((expTime / 2.0) * Pow(volatility, 2.0))) / (volatility * Sqrt(expTime));
            var num3 = num2 - (volatility * Sqrt(expTime));
            return (Exp(-riskFreeRate * expTime) * (((cpf * forward) * Statistics.NormSDist(num2 * cpf)) - ((cpf * strike) * Statistics.NormSDist(num3 * cpf))));
        }

        public static double BlackVega(double forward, double strike, double riskFreeRate, double expTime, double volatility)
        {
            var d = (Log(forward / strike) + ((expTime / 2.0) * Pow(volatility, 2.0))) / (volatility * Sqrt(expTime));
            var num5 = Exp(-riskFreeRate * expTime);
            return (((forward * num5) * Statistics.Phi(d)) * Sqrt(expTime)) / 100.0;
        }
        
        public static double BlackGamma(double forward, double strike, double riskFreeRate, double expTime, double volatility)
        {
            double d1, d2, dF;
            dF = Exp(-riskFreeRate * expTime);
            d1 = (Log(forward / strike) + (expTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expTime));
            d2 = d1 - volatility * Sqrt(expTime);
            return dF * Statistics.Phi(d1) / (forward * volatility * Sqrt(expTime)) * (0.01 * forward);
        }
        
        public static double BlackDelta(double forward, double strike, double riskFreeRate, double expTime, double volatility, OptionType CP)
        {
            double d1, d2, DF;
            DF = Exp(-riskFreeRate * expTime);
            d1 = (Log(forward / strike) + (expTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expTime));
            d2 = d1 - volatility * Sqrt(expTime);

            //Delta
            if (CP == OptionType.Put)
            {
                return DF * (Statistics.NormSDist(d1) - 1);
            }
            else
            {
                return DF * Statistics.NormSDist(d1);
            }
        }

        public static double[] BlackDerivs(double forward, double strike, double riskFreeRate, double expTime, double volatility, OptionType CP)
        {
            var output = new double[3];
            double d1, d2, DF;

            DF = Exp(-riskFreeRate * expTime);
            d1 = (Log(forward / strike) + (expTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expTime));
            d2 = d1 - volatility * Sqrt(expTime);

            //delta
            if (CP == OptionType.Put)
            {
                output[0] = DF * (Statistics.NormSDist(d1) - 1);
            }
            else
            {
                output[0] = DF * Statistics.NormSDist(d1);
            }
            //gamma
            output[1] = DF * Statistics.Phi(d1) / (forward * volatility * Sqrt(expTime));
            //speed
            output[2] = -output[1] / forward * (1 + d1 / (volatility * Sqrt(expTime)));
            return output;
        }

        public static double AbsoluteStrikefromDeltaKAnalytic(double forward, double delta, double riskFreeRate, double expTime, double volatility)
        {
            double psi = Sign(delta);
            var sqrtT = Sqrt(expTime);
            var q = Statistics.NormInv(psi * delta);
            return forward * Exp(-psi * volatility * sqrtT * q + 0.5 * Pow(volatility, 2) * expTime);
        }

        public static double BlackImpliedVol(double forward, double strike, double riskFreeRate, double expTime, double premium, OptionType CP)
        {
            Func<double, double> testBlack = (vol =>
            {
                return BlackPV(forward, strike, riskFreeRate, expTime, vol, CP) - premium;
            });

            var impliedVol = Math.Solvers.Brent.BrentsMethodSolve(testBlack, 0.000000001, 5.0000000, 1e-10);
            return impliedVol;
        }
    }
}
