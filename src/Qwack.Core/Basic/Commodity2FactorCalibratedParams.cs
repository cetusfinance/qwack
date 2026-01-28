namespace Qwack.Core.Basic
{
    public class Commodity2FactorCalibratedParams
    {
        public double Sigma1 { get; set; }   // Long-term (GBM) volatility
        public double Sigma2 { get; set; }   // Short-term (OU) volatility
        public double Kappa { get; set; }    // Mean reversion speed
        public double Rho { get; set; }      // Correlation between factors

        // Initial states (can be set to zero - drift adjustment handles forwards)
        public double X1_0 { get; set; } = 0;
        public double X2_0 { get; set; } = 0;

        // Calibration quality metrics
        public double CalibrationError { get; set; }
        public double[] ModelVols { get; set; }
        public double[] MarketVols { get; set; }
    }
}
