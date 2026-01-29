namespace Qwack.Core.Basic
{
    /// <summary>
    /// Parameters for a Hull-White style commodity model.
    /// 
    /// The model simulates the instantaneous forward rate f(t,T) as:
    ///   df(t,T) = σ(T) * e^{-α(T-t)} * dW(t)
    /// 
    /// This gives log-normal forwards with volatility that depends on the forward's delivery date T,
    /// not the option expiry. An option on Dec26 expiring in March has the same vol as one expiring in Nov.
    /// 
    /// Key properties:
    /// - Exactly calibrates to the forward curve (forwards are martingales)
    /// - Options expiring into immediate delivery recover Black pricing
    /// - The vol for any option on forward F(T) uses σ(T) from the vol surface at maturity T
    /// - Mean reversion α controls correlation between forwards of different maturities
    /// </summary>
    public class HullWhiteCommodityModelParameters
    {
        /// <summary>
        /// Mean reversion speed (alpha). Higher values = faster decorrelation of forwards.
        /// Typical values: 0.01 to 2.0
        /// - α = 0: All forwards perfectly correlated (parallel shifts only)
        /// - α = 1: Half-life of correlation ~ 0.7 years
        /// - α = 2: Half-life of correlation ~ 0.35 years
        /// </summary>
        public double MeanReversion { get; set; } = 0.1;

        /// <summary>
        /// Whether to calibrate volatility from the vol surface.
        /// If true, uses the ATM vol at the forward's delivery date.
        /// </summary>
        public bool CalibrateToVolSurface { get; set; } = true;

        /// <summary>
        /// Base volatility level (used when CalibrateToVolSurface = false)
        /// </summary>
        public double Sigma { get; set; } = 0.20;

        /// <summary>
        /// Creates default parameters suitable for commodity forward curves
        /// </summary>
        /// <param name="meanReversion">Mean reversion speed (default 0.1)</param>
        public static HullWhiteCommodityModelParameters CreateDefault(double meanReversion = 0.1)
        {
            return new HullWhiteCommodityModelParameters
            {
                MeanReversion = meanReversion,
                CalibrateToVolSurface = true,
                Sigma = 0.20
            };
        }

        /// <summary>
        /// Creates parameters with no mean reversion (equivalent to Black model)
        /// </summary>
        public static HullWhiteCommodityModelParameters CreateBlackEquivalent()
        {
            return new HullWhiteCommodityModelParameters
            {
                MeanReversion = 0.0,
                CalibrateToVolSurface = true,
                Sigma = 0.20
            };
        }

        /// <summary>
        /// Computes the B(t,T) function: integral of e^{-α(T-s)} from t to T
        /// B(t,T) = (1 - e^{-α(T-t)}) / α
        /// </summary>
        public double B(double t, double T)
        {
            var tau = T - t;
            if (tau <= 0) return 0;
            if (MeanReversion < 1e-10)
                return tau;
            return (1 - global::System.Math.Exp(-MeanReversion * tau)) / MeanReversion;
        }

        /// <summary>
        /// Computes the variance of the integrated short rate from t to T
        /// Used for bond option pricing in the HW model
        /// </summary>
        public double IntegratedVariance(double t, double T, double sigma)
        {
            if (MeanReversion < 1e-10)
                return sigma * sigma * t;

            var alpha = MeanReversion;
            var B_tT = B(t, T);
            return sigma * sigma * B_tT * B_tT * (1 - global::System.Math.Exp(-2 * alpha * t)) / (2 * alpha);
        }

        /// <summary>
        /// Computes the correlation between two forwards with different delivery dates
        /// under the Hull-White model
        /// </summary>
        public double ForwardCorrelation(double t, double T1, double T2)
        {
            if (MeanReversion < 1e-10)
                return 1.0;  // Perfect correlation when no mean reversion

            // Correlation decays with the difference in delivery dates
            var alpha = MeanReversion;
            return global::System.Math.Exp(-alpha * global::System.Math.Abs(T1 - T2));
        }

        /// <summary>
        /// Computes the volatility scaling factor needed to match Black variance at delivery.
        /// 
        /// Black variance: σ_Black² * T
        /// HW variance: σ² * (1 - e^{-2αT}) / (2α)
        /// 
        /// To match: σ = σ_Black * √(2αT / (1 - e^{-2αT}))
        /// 
        /// This factor → 1 as α → 0 (recovers Black exactly).
        /// </summary>
        /// <param name="T">Time to delivery</param>
        /// <returns>Scaling factor to apply to Black vol</returns>
        public double BlackVolScalingFactor(double T)
        {
            if (MeanReversion < 1e-10 || T < 1e-10)
                return 1.0;

            var alpha = MeanReversion;
            var hwVarianceFactor = (1 - global::System.Math.Exp(-2 * alpha * T)) / (2 * alpha);
            var blackVarianceFactor = T;
            return global::System.Math.Sqrt(blackVarianceFactor / hwVarianceFactor);
        }

        /// <summary>
        /// Computes the integrated variance from 0 to τ for an option on a forward
        /// with delivery at T, using scaled volatility that matches Black at delivery.
        /// </summary>
        /// <param name="tau">Option expiry time</param>
        /// <param name="T">Forward delivery time (must be >= tau)</param>
        /// <param name="blackVol">Black vol at delivery</param>
        /// <returns>Integrated variance for the option</returns>
        public double OptionVariance(double tau, double T, double blackVol)
        {
            if (tau > T) tau = T;  // Can't expire after delivery

            if (MeanReversion < 1e-10)
                return blackVol * blackVol * tau;  // Black case

            var alpha = MeanReversion;
            var scaleFactor = BlackVolScalingFactor(T);
            var scaledVol = blackVol * scaleFactor;

            // Integrate σ² * e^{-2α(T-t)} from 0 to τ
            // = σ² * (e^{-2α(T-τ)} - e^{-2αT}) / (2α)
            var hwIntegral = (global::System.Math.Exp(-2 * alpha * (T - tau)) - global::System.Math.Exp(-2 * alpha * T)) / (2 * alpha);
            return scaledVol * scaledVol * hwIntegral;
        }

        /// <summary>
        /// Computes the effective (implied) volatility for an option expiring at τ
        /// on a forward with delivery at T.
        /// </summary>
        /// <param name="tau">Option expiry time</param>
        /// <param name="T">Forward delivery time</param>
        /// <param name="blackVol">Black vol at delivery</param>
        /// <returns>Effective implied volatility</returns>
        public double EffectiveVol(double tau, double T, double blackVol)
        {
            if (tau < 1e-10) return blackVol;
            var variance = OptionVariance(tau, T, blackVol);
            return global::System.Math.Sqrt(variance / tau);
        }
    }
}
