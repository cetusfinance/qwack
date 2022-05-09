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
            var f1 = 1.0 / (2 * PI * sx * sy * Sqrt(1.0 - rho * rho));
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
        public static double PDF(double x, double y, double rho)
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

        /// <summary>
        /// Cumulative distribution of unit bi-variate normal
        /// http://www.codeplanet.eu/files/download/accuratecumnorm.pdf
        /// </summary>
        /// <param name="a">Z-value A</param>
        /// <param name="b">Z-value B</param>
        /// <param name="r">Correlation</param>
        /// <returns></returns>
        public static double CDF(double a, double b, double r)
        {
            var output = 0.0;

            var x = new[] { 0.04691008, 0.23076534, 0.5, 0.76923466, 0.95308992 };
            var W = new[] { 0.018854042, 0.038088059, 0.0452707394, 0.038088059, 0.018854042 };
            var h1 = a;
            var h2 = b;
            var h12 = (h1 * h1 + h2 * h2) / 2.0;
            if (Abs(r) >= 0.7)
            {
                var r2 = 1.0 - r * r;
                var r3 = Sqrt(r2);
                if (r < 0)
                    h2 = -h2;
                var h3 = h1 * h2;
                var h7 = Exp(-h3 / 2);
                var LH = 0.0;
                if (Abs(r) < 1)
                {
                    var h6 = Abs(h1 - h2);
                    var h5 = h6 * h6 / 2.0;
                    h6 /= r3;
                    var AA = 0.5 - h3 / 8.0;
                    var ab = 3 - 2 * AA * h5;
                    LH = 0.13298076 * h6 * ab * (1 - Statistics.CumulativeNormalDistribution(h6)) - Exp(-h5 / r2) * (ab + AA * r2) * 0.053051647;
                    for (var i = 0; i < 5; i++)
                    {
                        var r1 = r3 * x[i];
                        var rr = r1 * r1;
                        r2 = Sqrt(1.0 - rr);
                        var h8 = h7 == 0 ? 0.0 : Exp(-h3 / (1.0 + r2)) / r2 / h7;
                        LH -= W[i] * Exp(-h5 / rr) * (h8 - 1.0 - AA * rr);
                    }
                }
                output = LH * r3 * h7 + Statistics.CumulativeNormalDistribution(Min(h1, h2));
                if (r < 0)
                    output = Statistics.CumulativeNormalDistribution(h1) - output;

                return output;
            }
            else
            {
                var h3 = h1 * h2;
                var LH = 0.0;
                if (r != 0)
                {
                    for (var i = 0; i < 5; i++)
                    {
                        var r1 = r * x[i];
                        var r2 = 1 - r1 * r1;
                        LH += W[i] * Exp((r1 * h3 - h12) / r2) / Sqrt(r2);
                    }
                }
                output = Statistics.CumulativeNormalDistribution(h1) * Statistics.CumulativeNormalDistribution(h2) + r * LH;

                return output;
            }
        }
    }
}
