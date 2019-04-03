using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math;
using Qwack.Core.Basic;
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

        public static double BlackDigitalPV(double forward, double strike, double riskFreeRate, double expTime, double volatility, OptionType CP)
        {
            var cpf = (CP == OptionType.Put) ? -1.0 : 1.0;

            var d1 = (Log(forward / strike) + (expTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expTime));
            var d2 = d1 - volatility * Sqrt(expTime);
   
            return Exp(-riskFreeRate * expTime) * Statistics.NormSDist(d2 * cpf);
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

        public static double BlackTheta(double forward, double strike, double riskFreeRate, double expTime, double volatility, OptionType CP)
        {
            double d1, d2, DF;
            DF = Exp(-riskFreeRate * expTime);
            d1 = (Log(forward / strike) + (expTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expTime));
            d2 = d1 - volatility * Sqrt(expTime);

            //Delta
            if (CP == OptionType.Put)
            {
                return -forward * DF * Statistics.Phi(d1) * volatility / (2.0 * Sqrt(expTime)) + riskFreeRate * forward * DF * Statistics.NormSDist(d1) - riskFreeRate * strike * DF * Statistics.NormSDist(d2);
            }
            else
            {
                return -forward * DF * Statistics.Phi(d1) * volatility / (2.0 * Sqrt(expTime)) - riskFreeRate * forward * DF * Statistics.NormSDist(-d1) + riskFreeRate * strike * DF * Statistics.NormSDist(-d2);
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

        public static double BlackDigitalImpliedVol(double forward, double strike, double riskFreeRate, double expTime, double premium, OptionType CP)
        {
            Func<double, double> testBlack = (vol =>
            {
                return BlackDigitalPV(forward, strike, riskFreeRate, expTime, vol, CP) - premium;
            });

            var impliedVol = Math.Solvers.Brent.BrentsMethodSolve(testBlack, 0.000000001, 5.0000000, 1e-10);
            return impliedVol;
        }

        public static double BarrierProbability(double startFwd, double endFwd, double barrier, double sigma, double t, BarrierSide barrierSide) => barrierSide == BarrierSide.Down
                ? (Min(startFwd, endFwd) < barrier ? 1.0 : Exp(-2.0 * Log(startFwd / barrier) * Log(endFwd / barrier) / (sigma * sigma * t)))
                : (Max(startFwd, endFwd) > barrier ? 1.0 : Exp(-2.0 * Log(startFwd / barrier) * Log(endFwd / barrier) / (sigma * sigma * t)));

        public static double BarrierOptionPV(double forward, double strike, double riskFreeRate, double expTime, double volatility, OptionType CP, double barrier, BarrierType barrierType, BarrierSide barrierSide)
        {
            var blackPV = BlackPV(forward, strike, riskFreeRate, expTime, volatility, CP);
            var barrierHitProb = BarrierProbability(forward, forward, barrier, volatility, expTime, barrierSide);

            return barrierType == BarrierType.KI ? barrierHitProb * blackPV : (1.0 - barrierHitProb) * blackPV;
        }

        public static double BarrierAdjust(double barrier, double vol, double deltaT, BarrierSide side) => 
            side==BarrierSide.Down 
            ? barrier * Exp(0.5826 * vol * deltaT) 
            : barrier / Exp(0.5826 * vol * deltaT);
    }
}
