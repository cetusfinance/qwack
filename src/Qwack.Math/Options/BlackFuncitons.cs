using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Math.Options
{
    public class BlackFuncitons
    {
        public static double BlackPV(double forward, double strike, double riskFreeRate, double expTime, double volatility, string CP)
        {
            double cpf = (CP == "P") ? -1.0 : 1.0;

            double d1 = (System.Math.Log(forward / strike) + (expTime / 2 * (System.Math.Pow(volatility, 2)))) / (volatility * System.Math.Sqrt(expTime));
            double d2 = d1 - volatility * System.Math.Sqrt(expTime);

            double num2 = (System.Math.Log(forward / strike) + ((expTime / 2.0) * System.Math.Pow(volatility, 2.0))) / (volatility * System.Math.Sqrt(expTime));
            double num3 = num2 - (volatility * System.Math.Sqrt(expTime));
            return (System.Math.Exp(-riskFreeRate * expTime) * (((cpf * forward) * Utils.Statistics.NormSDist(num2 * cpf)) - ((cpf * strike) * Utils.Statistics.NormSDist(num3 * cpf))));
        }

        public static double BlackVega(double forward, double strike, double riskFreeRate, double expTime, double volatility)
        {
            double d = (System.Math.Log(forward / strike) + ((expTime / 2.0) * System.Math.Pow(volatility, 2.0))) / (volatility * System.Math.Sqrt(expTime));
            double num5 = System.Math.Exp(-riskFreeRate * expTime);
            return (((forward * num5) * Utils.Statistics.Phi(d)) * System.Math.Sqrt(expTime)) / 100.0;
        }


        public static double BlackGamma(double forward, double strike, double riskFreeRate, double expTime, double volatility)
        {
            double d1, d2, DF;

            DF = System.Math.Exp(-riskFreeRate * expTime);

            d1 = (System.Math.Log(forward / strike) + (expTime / 2 * (System.Math.Pow(volatility, 2)))) / (volatility * System.Math.Sqrt(expTime));
            d2 = d1 - volatility * System.Math.Sqrt(expTime);

            return DF * Utils.Statistics.Phi(d1) / (forward * volatility * System.Math.Sqrt(expTime)) * (0.01 * forward);
        }


        public static double BlackDelta(double forward, double strike, double riskFreeRate, double expTime, double volatility, string CP)
        {
            double d1, d2, DF;

            DF = System.Math.Exp(-riskFreeRate * expTime);

            d1 = (System.Math.Log(forward / strike) + (expTime / 2 * (System.Math.Pow(volatility, 2)))) / (volatility * System.Math.Sqrt(expTime));
            d2 = d1 - volatility * System.Math.Sqrt(expTime);

            //Delta
            if (CP == "P")
            {
                return DF * (Utils.Statistics.NormSDist(d1) - 1);
            }
            else
            {
                return DF * Utils.Statistics.NormSDist(d1);
            }
        }

        public static double[] BlackDerivs(double forward, double strike, double riskFreeRate, double expTime, double volatility, string CP)
        {
            double[] output = new double[3];
            double d1, d2, DF;

            DF = System.Math.Exp(-riskFreeRate * expTime);

            d1 = (System.Math.Log(forward / strike) + (expTime / 2 * (System.Math.Pow(volatility, 2)))) / (volatility * System.Math.Sqrt(expTime));
            d2 = d1 - volatility * System.Math.Sqrt(expTime);

            //delta
            if (CP == "P")
            {
                output[0] = DF * (Utils.Statistics.NormSDist(d1) - 1);
            }
            else
            {
                output[0] = DF * Utils.Statistics.NormSDist(d1);
            }
            //gamma
            output[1] = DF * Utils.Statistics.Phi(d1) / (forward * volatility * System.Math.Sqrt(expTime));
            //speed
            output[2] = -output[1] / forward * (1 + d1 / (volatility * System.Math.Sqrt(expTime)));

            return output;
        }

        public static double AbsoluteStrikefromDeltaKAnalytic(double forward, double delta, double riskFreeRate, double expTime, double volatility)
        {
            double psi = System.Math.Sign(delta);
            double sqrtT = System.Math.Sqrt(expTime);
            double Q = Utils.Statistics.NormInv(psi * delta);

            return forward * System.Math.Exp(-psi * volatility * sqrtT * Q + 0.5 * System.Math.Pow(volatility, 2) * expTime);
        }
    }
}
