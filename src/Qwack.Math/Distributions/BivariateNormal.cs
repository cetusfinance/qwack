using System;
using System.Collections.Generic;
using System.Text;
using static System.Math;

namespace Qwack.Math.Distributions
{
    public static class BivariateNormal
    {
        /// <summary>
        /// Returns probability density function for the bivariate normal distribution
        /// </summary>
        /// <param name="x">x value</param>
        /// <param name="mx">x mean</param>
        /// <param name="sx">x std deviation</param>
        /// <param name="y">y value</param>
        /// <param name="my">y mean</param>
        /// <param name="sy">y std deviation</param>
        /// <param name="rho">correlation</param>
        /// <returns></returns>
        public static double PDF(double x, double mx, double sx, double y, double my, double sy, double rho)
        {
            var f1 = 1.0 / (2*PI*sx*sy*Sqrt(1.0-rho*rho));
            var f2 = (x - mx) * (x - mx) / (sx * sx);
            f2 += (y - my) * (y - my) / (sy * sy);
            f2 -= (2.0 * rho * (x - mx) * (y - my)) / (sx * sy);
            var f3 = -1.0 / (2.0 * (1.0 - rho * rho));

            var pdf = f1 * Exp(f2 * f3);
            return pdf;
        }

        /// <summary>
        /// Returns probability density function for the unit bivariate normal distribution
        /// Assumes zero means and unit standard deviation for both variables
        /// </summary>
        /// <param name="x">x value</param>
        /// <param name="y">y value</param>
        /// <param name="rho">correlation</param>
        /// <returns></returns>
        public static double PDF(double x,  double y, double rho)
        {
            var f1 = 1.0 / (2 * PI * Sqrt(1.0 - rho * rho));
            var f2 = x * x + y * y - 2.0 * rho * x * y;
            var f3 = -1.0 / (2.0 * (1.0 - rho * rho));

            var pdf = f1 * Exp(f2 * f3);
            return pdf;
        }

        public static double Characteristic(double x, double y, double rho)
        {
            var f = x * x + y * y + 2.0 * rho * x * y;
            var psi = Exp(-0.5 * f);
            return psi;
        }
    }
}
