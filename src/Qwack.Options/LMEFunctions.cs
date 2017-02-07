using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math;
using static System.Math;

namespace Qwack.Options
{
    //functions for pricing and risking listed options on the London Metal Exchange
    public class LMEFunctions
    {
        public static double LMEBlackPV(double forward, double strike, double discountingRate, double expiryTime, double deliveryTime, double volatility, OptionType CP)
        {
            double cpf = (CP == OptionType.Put) ? -1.0 : 1.0;

            double d1 = (Log(forward / strike) + (expiryTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expiryTime));
            double d2 = d1 - volatility * Sqrt(expiryTime);

            double num2 = (Log(forward / strike) + ((expiryTime / 2.0) * Pow(volatility, 2.0))) / (volatility * Sqrt(expiryTime));
            double num3 = num2 - (volatility * Sqrt(expiryTime));
            return (Exp(-discountingRate * deliveryTime) * (((cpf * forward) * Statistics.NormSDist(num2 * cpf)) - ((cpf * strike) * Statistics.NormSDist(num3 * cpf))));
        }

        public static double LMEBlackVega(double forward, double strike, double discountingRate, double expiryTime, double deliveryTime, double volatility)
        {
            double d = (Log(forward / strike) + ((expiryTime / 2.0) * Pow(volatility, 2.0))) / (volatility * Sqrt(expiryTime));
            double num5 = Exp(-discountingRate * deliveryTime);
            return (((forward * num5) * Statistics.Phi(d)) * Sqrt(expiryTime)) / 100.0;
        }
        
        public static double LMEBlackGamma(double forward, double strike, double expiryTime, double volatility)
        {
            double d1, d2;
            d1 = (Log(forward / strike) + (expiryTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expiryTime));
            d2 = d1 - volatility * Sqrt(expiryTime);
            return  Statistics.Phi(d1) / (forward * volatility * Sqrt(expiryTime)) * (0.01 * forward);
        }
        
        public static double LMEBlackDelta(double forward, double strike, double expiryTime, double volatility, OptionType CP)
        {
            double d1, d2;
            d1 = (Log(forward / strike) + (expiryTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expiryTime));
            d2 = d1 - volatility * Sqrt(expiryTime);

            //Delta
            if (CP == OptionType.Put)
            {
                return Statistics.NormSDist(d1) - 1.0;
            }
            else
            {
                return Statistics.NormSDist(d1);
            }
        }

        public static double AbsoluteStrikefromDeltaKAnalytic(double forward, double delta, double expiryTime, double volatility)
        {
            double psi = Sign(delta);
            double sqrtT = Sqrt(expiryTime);
            double q = Statistics.NormInv(psi * delta);
            return forward * Exp(-psi * volatility * sqrtT * q + 0.5 * Pow(volatility, 2) * expiryTime);
        }

        public static double LMEBlackImpliedVol(double forward, double strike, double discountingRate, double expiryTime, double deliveryTime, double premium, OptionType CP)
        {
            Func<double, double> testLMEBlack = (vol =>
            {
                return LMEBlackPV(forward, strike, discountingRate, expiryTime, deliveryTime, vol, CP) - premium;
            });

            var impliedVol = Math.Solvers.Brent.BrentsMethodSolve(testLMEBlack, 0.000000001, 5.0000000, 1e-10);
            return impliedVol;
        }
    }
}
