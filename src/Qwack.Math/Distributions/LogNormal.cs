using Qwack.Math.Extensions;
using static System.Math;

namespace Qwack.Math.Distributions
{
    public static class LogNormal
    {
        public static double M(double mu, double sigma) => Exp(mu + sigma * sigma / 2.0);
        public static double V(double mu, double sigma) => (Exp(sigma * sigma) - 1.0) * Exp(2.0 * mu + sigma * sigma);
        public static double Mu(double m, double v) => Log(m / Sqrt(1 + v / m / m));
        public static double Sigma(double m, double v) => Log(m / Sqrt(1 + v / m / m));

        public static double PDF(double x, double mu, double sigma)
            => 1.0 / (x * sigma * DoubleExtensions.Sqrt2Pi) * Exp(-(Log(x) - mu).IntPow(2) / (2.0 * sigma * sigma));

        public static double CDF(double x, double mu, double sigma)
         => 0.5 * (1.0 + Statistics.Erf((Log(x) - mu) / sigma / Sqrt(2.0)));

    }
}
