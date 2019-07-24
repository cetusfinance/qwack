using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Math.Extensions;
using static System.Math;

namespace Qwack.Options
{
    /// <summary>
    /// A collection of functions relating to the Stochastic Vol Inspired (SVI) parameterizations
    /// https://arxiv.org/pdf/1204.0646.pdf
    /// </summary>
    public static class SVI
    {
        /// <summary>
        /// "Raw" form of SVI 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="rho"></param>
        /// <param name="k">log moniness, log (F/K)</param>
        /// <param name="m"></param>
        /// <param name="sigma"></param>
        /// <returns>Total implied variance, vol * vol * t</returns>
        public static double SVI_Raw_ImpliedVariance(double a, double b, double rho, double k, double m, double sigma) 
            => a + b * (rho * (k - m) + Sqrt((k - m) * (k - m) + sigma * sigma));

        public static double SVI_Raw_ImpliedVol(double a, double b, double rho, double K, double F, double t, double m, double sigma)
            => Sqrt(SVI_Raw_ImpliedVariance(a, b, rho, Log(K / F), m, sigma) / t);

        /// <summary>
        /// "Natural" fform of SVI
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="omega"></param>
        /// <param name="eta"></param>
        /// <param name="rho"></param>
        /// <param name="k">log moniness, log (F/K)</param>
        /// <param name="mu"></param>
        /// <returns>Total implied variance, vol * vol * t</returns>
        public static double SVI_Natural_ImpliedVariance(double delta, double omega, double eta, double rho, double k, double mu)
            => delta + omega / 2 * (1.0 + eta * rho * (k - mu) + Sqrt((eta * (k - mu) + rho).IntPow(2) + (1 - rho) * (1 - rho)));

        public static double SVI_Natural_ImpliedVol(double delta, double omega, double eta, double rho, double K, double F, double t, double mu)
            => Sqrt(SVI_Natural_ImpliedVariance(delta, omega, eta, rho, Log(K / F), mu) / t);
    }
}
