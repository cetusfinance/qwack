using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Math;
using static System.Math;

namespace Qwack.Options
{
    public static class NFactorUtils
    {
        private static double[][] Cov_Func2F(this SchwartzSmithTwoFactorModelParameters p, double dt)
        {
            var o = new double[2][];
            o[0] = new double[2];
            o[1] = new double[2];

            return o;

        }

        public static double[,] CovFunc(this NFactorModelParameters p, double dt)
        {
            // Check if parameters dictionary has keys
            if (p == null || !(p.X0s.Length > 0))
            {
                throw new ArgumentException("Argument parameters must be a non-empty object");
            }

            var N_factors = p.X0s.Length;
            var output = new double[N_factors, N_factors];

            for (var i = 0; i < N_factors; i++)
            {
                output[i, i] = 1;
            }


            var isGBM = p.Kappas[0] == 0;

            // Calculate covariance matrix
            for (var i = 0; i < N_factors; i++)
            {
                for (var j = 0; j < N_factors; j++)
                {
                    var kappaSum = p.Kappas[i] + p.Kappas[j];
                    var rho = p.Rhos[i][j];

                    output[i, j] = p.Sigmas[i] * p.Sigmas[j] *
                                    (i == j ? 1 : rho) * (1 - Exp(-kappaSum * dt)) / kappaSum;
                }
            }

            // Adjust output for GBM case
            if (isGBM)
            {
                output[0, 0] = Pow(p.Sigmas[0], 2) * dt;
            }

            return output;
        }

        public static double[,] A_Matrix(this NFactorModelParameters p, double Tt)
        {
            // Check if parameters dictionary has keys
            if (p == null || !(p.Sigmas?.Length > 0))
            {
                throw new ArgumentException("Argument parameters must be a non-empty object");
            }

            var N_factors = p.Sigmas.Length;
            var output = new double[N_factors, N_factors];

            // Initialize diagonal of output matrix to 0
            for (var i = 0; i < N_factors; i++)
            {
                output[i, i] = 0;
            }

            var GBM = p.Kappas[0] == 0;

            if (GBM)
            {
                output[0, 0] = (p.Mu_Rn + 0.5 * Pow(p.Sigmas[0], 2)) * Tt;
            }

            if (!(GBM && N_factors == 1))
            {
                for (var i = GBM ? 1 : 0; i < N_factors; i++)
                {
                    var kappa = p.Kappas[i];
                    var lambda = p.Lambdas[i];
                    output[i,i] -= (1 - Exp(-kappa * Tt)) * (lambda / kappa);
                }

                for (var i = 0; i < N_factors; i++)
                {
                    for (var j = 0; j < N_factors; j++)
                    {
                        if (!(i == 0 && j == 0))
                        {
                            var kappaSum = p.Kappas[i] + p.Kappas[j];
                            var rho = p.Rhos[i][j];
                            output[i, j] += 0.5 * p.Sigmas[i] * p.Sigmas[j] *
                                    (i == j ? 1 : rho) * (1 - Exp(-kappaSum * Tt)) / kappaSum;
                        }
                    }
                }
            }

            return output;
        }

        public static double A(this NFactorModelParameters p, double Tt)
        {
            // Check if parameters dictionary has keys
            if (p == null || !(p.Sigmas?.Length > 0))
            {
                throw new ArgumentException("Argument parameters must be a non-empty object");
            }

            var N_factors = p.Sigmas.Length;
            var output = 0.0;

            var GBM = p.Kappas[0] == 0;

            if (GBM)
            {
                output = (p.Mu_Rn + 0.5 * Pow(p.Sigmas[0], 2)) * Tt;
            }

            if (!(GBM && N_factors == 1))
            {
                for (var i = GBM ? 1 : 0; i < N_factors; i++)
                {
                    var kappa = p.Kappas[i];
                    var lambda = p.Lambdas[i];
                    output -= (1 - Exp(-kappa * Tt)) * (lambda / kappa);
                }

                for (var i = 0; i < N_factors; i++)
                {
                    for (var j = 0; j < N_factors; j++)
                    {
                        if (!(i == 0 && j == 0))
                        {
                            var kappaSum = p.Kappas[i] + p.Kappas[j];
                            var rho = p.Rhos[i][j];
                            output += 0.5 * p.Sigmas[i] * p.Sigmas[j] *
                                    (i == j ? 1 : rho) * (1 - Exp(-kappaSum * Tt)) / kappaSum;
                        }
                    }
                }
            }

            return output;
        }

        public static double FuturesPriceForecast(this SchwartzSmithTwoFactorModelParameters p, double T, double t)
        {
            var pPrime = new NFactorModelParameters
            {
                Kappas = [p.Kappa_1, p.Kappa_2],
                Lambdas = [0, p.Lambda_2],
                Sigmas = [p.Sigma_1, p.Sigma_2],
                Rhos = [[1, p.Rho_1_2], [p.Rho_1_2, 1]],
                Mu = p.Mu,
                Mu_Rn = p.Mu_Rn,
                X0s = [p.X1_0, p.X2_0],
            };

            var A = pPrime.A(T - t);

            var logExp = p.X1_0 * Exp(-p.Kappa_1 * T) + p.X2_0 * Exp(-p.Kappa_2 * T);
            logExp += p.Mu_Rn * t;
            logExp += A;

            return Exp(logExp);
        }

        public static double FuturesPriceForecast(this NFactorModelParameters p, double T, double t)
        {
            var A = p.A(T - t);
            var logExp = p.X0s[0] * Exp(-p.Kappas[0] * T) + p.X0s[1] * Exp(-p.Kappas[1] * T);
            logExp += p.Mu_Rn * t;
            logExp += A;

            return Exp(logExp);
        }

        public static double SpotPriceForecast(this SchwartzSmithTwoFactorModelParameters p, double t)
        {
            var logExp = p.Mu * t; 
            logExp += p.X1_0 * Exp(-p.Kappa_1 * t) + p.X2_0 * Exp(-p.Kappa_2 * t);


            var pPrime = new NFactorModelParameters
            {
                Kappas = [p.Kappa_1, p.Kappa_2],
                Lambdas = [0,p.Lambda_2],
                Sigmas = [p.Sigma_1,p.Sigma_2],
                Rhos = [[1, p.Rho_1_2], [p.Rho_1_2,1]],
                Mu = p.Mu,
                Mu_Rn = p.Mu_Rn,
                X0s = [p.X1_0, p.X2_0],
            };
            var covMatrix = pPrime.CovFunc(t);
            var varSum = 0.0;
            for( var i = 0;i<covMatrix.GetLength(0); i++)
                for( var j = 0;j<covMatrix.GetLength(1);j++)
                    varSum += covMatrix[i,j];

            logExp += 0.5 * varSum;
            return Exp(logExp);
        }

        public static double EuropeanOptionPv(this NFactorModelParameters p, double K, double r, double tOption, double tFuture, bool isCall)
        {
            var N_factors = p.Sigmas.Length;
            var isGBM = p.Kappas[0] ==0 ;

            // Current expected futures price:
            var F_Tt = FuturesPriceForecast(p, tFuture, 0);

            // Underlying Volatility:
            var covariance = CovFunc(p, tOption);
            double sigmaTt = 0;
            for (var i = 0; i < N_factors; i++)
            {
                for (var j = 0; j < N_factors; j++)
                {
                    sigmaTt += covariance[i, j] * Exp(-(p.Kappas[i] + p.Kappas[j]) * (tFuture - tOption));
                }
            }

            var sd = Sqrt(sigmaTt);

            var d1 = (Log(F_Tt / K) + 0.5 * Pow(sd, 2)) / sd;
            var d2 = d1 - sd;

            double value;
            if (isCall)
            {
                value = Exp(-r * tOption) * (F_Tt * Statistics.NormSDist(d1) - K * Statistics.NormSDist(d2));
            }
            else
            {
                value = Exp(-r * tOption) * (K * Statistics.NormSDist(-d2) - F_Tt * Statistics.NormSDist(-d1));
            }

            return value;
        }

        /// <summary>
        /// Compute ATM implied vol for futures option in 2-factor model (Schwartz-Smith style).
        /// Factor 1 is GBM (kappa=0), Factor 2 is mean-reverting OU.
        /// </summary>
        /// <param name="sigma1">Long-term (GBM) volatility</param>
        /// <param name="sigma2">Short-term (OU) volatility</param>
        /// <param name="kappa">Mean reversion speed for factor 2</param>
        /// <param name="rho">Correlation between factors</param>
        /// <param name="tOption">Time to option expiry in years</param>
        /// <param name="tFuture">Time to futures expiry in years (>= tOption, default = tOption for spot)</param>
        /// <returns>ATM implied volatility</returns>
        public static double ImpliedVolForFuturesOption2F(
            double sigma1, double sigma2, double kappa, double rho,
            double tOption, double tFuture = 0)
        {
            // If tFuture not specified or less than tOption, assume spot option (tFuture = tOption)
            if (tFuture < tOption) tFuture = tOption;

            var tau = tFuture - tOption;  // Time from option expiry to futures expiry

            var expNegKappaTau = Exp(-kappa * tau);
            var expNeg2KappaTau = expNegKappaTau * expNegKappaTau;

            // Variance components for ln(F(tOption, tFuture))
            // Var[ln(F)] = sigma1^2 * tOption
            //            + sigma2^2 * e^{-2*kappa*tau} * (1 - e^{-2*kappa*tOption}) / (2*kappa)
            //            + 2 * rho * sigma1 * sigma2 * e^{-kappa*tau} * (1 - e^{-kappa*tOption}) / kappa

            var var1 = sigma1 * sigma1 * tOption;  // GBM factor contribution

            double var2, cov;
            if (Abs(kappa) < 1e-10)
            {
                // Limiting case when kappa -> 0: OU becomes another GBM
                var2 = sigma2 * sigma2 * tOption;
                cov = 2 * rho * sigma1 * sigma2 * tOption;
            }
            else
            {
                var expNeg2KappaTOption = Exp(-2 * kappa * tOption);
                var expNegKappaTOption = Exp(-kappa * tOption);

                var2 = sigma2 * sigma2 * expNeg2KappaTau * (1 - expNeg2KappaTOption) / (2 * kappa);
                cov = 2 * rho * sigma1 * sigma2 * expNegKappaTau * (1 - expNegKappaTOption) / kappa;
            }

            var totalVariance = var1 + var2 + cov;
            if (totalVariance <= 0) return 0;

            return Sqrt(totalVariance / tOption);
        }

        /// <summary>
        /// Compute implied vol for swaption (option on commodity swap/average) in 2-factor model.
        /// The swap rate is the average of spot prices over the averaging period.
        /// </summary>
        /// <param name="sigma1">Long-term (GBM) volatility</param>
        /// <param name="sigma2">Short-term (OU) volatility</param>
        /// <param name="kappa">Mean reversion speed for factor 2</param>
        /// <param name="rho">Correlation between factors</param>
        /// <param name="tOption">Time to option expiry in years</param>
        /// <param name="averagingDates">Dates over which the swap averages</param>
        /// <param name="originDate">Valuation date</param>
        /// <returns>Implied volatility for the swaption</returns>
        public static double ImpliedVolForSwaption2F(
            double sigma1, double sigma2, double kappa, double rho,
            double tOption, DateTime[] averagingDates, DateTime originDate)
        {
            if (averagingDates == null || averagingDates.Length == 0)
                return ImpliedVolForFuturesOption2F(sigma1, sigma2, kappa, rho, tOption, tOption);

            var n = averagingDates.Length;

            // Convert averaging dates to year fractions
            var tValues = new double[n];
            for (var i = 0; i < n; i++)
            {
                tValues[i] = (averagingDates[i] - originDate).TotalDays / 365.0;
            }

            // Variance of average price in 2-factor model:
            // Var[SwapRate] = (1/n^2) * sum_{i,j} Cov[ln(S(ti)), ln(S(tj))]
            // where the covariance involves integrals of the 2-factor covariance structure

            var totalCovariance = 0.0;

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    var ti = tValues[i];
                    var tj = tValues[j];
                    var tMin = Min(ti, tj);
                    var tMax = Max(ti, tj);

                    // Cov[ln(S(ti)), ln(S(tj))] has contributions from both factors
                    // For GBM factor (factor 1): Cov = sigma1^2 * min(ti, tj)
                    var cov1 = sigma1 * sigma1 * tMin;

                    double cov2, cov12;
                    if (Abs(kappa) < 1e-10)
                    {
                        // Limiting case: OU becomes GBM
                        cov2 = sigma2 * sigma2 * tMin;
                        cov12 = 2 * rho * sigma1 * sigma2 * tMin;
                    }
                    else
                    {
                        // For OU factor (factor 2):
                        // Cov[X2(ti), X2(tj)] = (sigma2^2 / (2*kappa)) * e^{-kappa*(ti+tj)} * (e^{2*kappa*min(ti,tj)} - 1)
                        var expNegKappaTi = Exp(-kappa * ti);
                        var expNegKappaTj = Exp(-kappa * tj);
                        var exp2KappaMin = Exp(2 * kappa * tMin);

                        cov2 = (sigma2 * sigma2 / (2 * kappa)) * expNegKappaTi * expNegKappaTj * (exp2KappaMin - 1);

                        // Cross-covariance between GBM and OU factors:
                        // Cov[X1(ti), X2(tj)] = rho * sigma1 * sigma2 * e^{-kappa*tj} * (1 - e^{-kappa*min(ti,tj)}) / kappa
                        // Cov[X1(tj), X2(ti)] = rho * sigma1 * sigma2 * e^{-kappa*ti} * (1 - e^{-kappa*min(ti,tj)}) / kappa
                        var expNegKappaMin = Exp(-kappa * tMin);
                        cov12 = rho * sigma1 * sigma2 * (1 - expNegKappaMin) / kappa *
                                (expNegKappaTi + expNegKappaTj);
                    }

                    totalCovariance += cov1 + cov2 + cov12;
                }
            }

            var variance = totalCovariance / (n * n);
            if (variance <= 0 || tOption <= 0) return 0;

            return Sqrt(variance / tOption);
        }
    }
}
