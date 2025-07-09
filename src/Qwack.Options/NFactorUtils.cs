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
    }
}
