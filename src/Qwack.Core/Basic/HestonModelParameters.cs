namespace Qwack.Core.Basic
{
    /// <summary>
    /// Parameters for the Heston stochastic volatility model.
    /// 
    /// The model dynamics are:
    ///   dS(t) = μ(t) S(t) dt + √v(t) S(t) dW_S(t)
    ///   dv(t) = κ(θ - v(t)) dt + σ_v √v(t) dW_v(t)
    ///   Corr(dW_S, dW_v) = ρ
    /// 
    /// Where:
    ///   S = spot/forward price
    ///   v = instantaneous variance
    ///   κ = mean reversion speed of variance
    ///   θ = long-term variance level
    ///   σ_v = volatility of variance (vol of vol)
    ///   ρ = correlation between spot and variance (typically negative for equity/commodities)
    /// 
    /// The model can auto-calibrate:
    ///   - v_0 from short-term ATM vol
    ///   - θ from long-term ATM vol
    ///   - Drift from forward curve
    /// </summary>
    public class HestonModelParameters
    {
        /// <summary>
        /// Mean reversion speed of variance (κ).
        /// Higher values = variance reverts faster to θ.
        /// Typical range: 0.5 to 5.0
        /// </summary>
        public double Kappa { get; set; } = 2.0;

        /// <summary>
        /// Long-term variance level (θ).
        /// The variance mean-reverts to this level.
        /// If CalibrateToVolSurface is true, this is set from long-term ATM vol.
        /// </summary>
        public double Theta { get; set; } = 0.04;  // Corresponds to 20% vol

        /// <summary>
        /// Initial instantaneous variance (v_0).
        /// If CalibrateToVolSurface is true, this is set from short-term ATM vol.
        /// </summary>
        public double V0 { get; set; } = 0.04;  // Corresponds to 20% vol

        /// <summary>
        /// Volatility of variance (σ_v), also called "vol of vol".
        /// Controls how much the variance fluctuates.
        /// Typical range: 0.2 to 1.0
        /// </summary>
        public double VolOfVol { get; set; } = 0.5;

        /// <summary>
        /// Correlation between spot returns and variance changes (ρ).
        /// Negative values create the "leverage effect" (falling prices = rising vol).
        /// Typical range: -0.9 to 0.0 for equities/commodities
        /// </summary>
        public double Rho { get; set; } = -0.5;

        /// <summary>
        /// Whether to calibrate v_0 and θ from the vol surface.
        /// If true:
        ///   - v_0 = (short-term ATM vol)²
        ///   - θ = (long-term ATM vol)²
        /// </summary>
        public bool CalibrateToVolSurface { get; set; } = true;

        /// <summary>
        /// Whether to calibrate drift to match the forward curve exactly.
        /// </summary>
        public bool CalibrateToForwardCurve { get; set; } = true;

        /// <summary>
        /// Time horizon (in years) for determining "long-term" vol for θ calibration.
        /// </summary>
        public double LongTermHorizon { get; set; } = 2.0;

        /// <summary>
        /// Time horizon (in years) for determining "short-term" vol for v_0 calibration.
        /// </summary>
        public double ShortTermHorizon { get; set; } = 0.25;

        /// <summary>
        /// Feller condition: 2κθ > σ_v² ensures variance stays positive.
        /// Returns true if the Feller condition is satisfied.
        /// </summary>
        public bool FellerConditionSatisfied => 2 * Kappa * Theta > VolOfVol * VolOfVol;

        /// <summary>
        /// Creates default Heston parameters suitable for equity/commodity markets.
        /// </summary>
        public static HestonModelParameters CreateDefault(
            double kappa = 2.0,
            double volOfVol = 0.5,
            double rho = -0.5)
        {
            return new HestonModelParameters
            {
                Kappa = kappa,
                VolOfVol = volOfVol,
                Rho = rho,
                CalibrateToVolSurface = true,
                CalibrateToForwardCurve = true
            };
        }

        /// <summary>
        /// Creates parameters with explicit variance levels (no auto-calibration).
        /// </summary>
        public static HestonModelParameters CreateExplicit(
            double v0,
            double theta,
            double kappa,
            double volOfVol,
            double rho)
        {
            return new HestonModelParameters
            {
                V0 = v0,
                Theta = theta,
                Kappa = kappa,
                VolOfVol = volOfVol,
                Rho = rho,
                CalibrateToVolSurface = false,
                CalibrateToForwardCurve = true
            };
        }

        /// <summary>
        /// Computes the expected variance at time t: E[v(t)] = θ + (v_0 - θ)e^{-κt}
        /// </summary>
        public double ExpectedVariance(double t)
        {
            return Theta + (V0 - Theta) * global::System.Math.Exp(-Kappa * t);
        }

        /// <summary>
        /// Computes the expected volatility at time t: √E[v(t)]
        /// </summary>
        public double ExpectedVol(double t)
        {
            return global::System.Math.Sqrt(ExpectedVariance(t));
        }

        /// <summary>
        /// Computes the approximate ATM implied vol for maturity T.
        /// This is an approximation - actual implied vol requires solving the Heston formula.
        /// </summary>
        public double ApproximateImpliedVol(double T)
        {
            if (T < 1e-10) return global::System.Math.Sqrt(V0);

            // Average expected variance over [0, T]
            // ∫₀^T E[v(s)] ds / T = θ + (v₀ - θ)(1 - e^{-κT})/(κT)
            double avgVar;
            if (Kappa < 1e-10)
            {
                avgVar = V0;  // No mean reversion
            }
            else
            {
                avgVar = Theta + (V0 - Theta) * (1 - global::System.Math.Exp(-Kappa * T)) / (Kappa * T);
            }

            return global::System.Math.Sqrt(avgVar);
        }
    }
}
