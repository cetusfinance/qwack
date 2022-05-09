namespace Qwack.Options
{
    /// <summary>
    /// A collection of functions relating to the Stochastic Alpha-Beta-Rho (SABR) parameterization
    /// </summary>
    public static class SABR
    {
        /// <summary>
        /// Compute implied vol from a set of SABR parameters, Berestycki algo
        /// </summary>
        /// <param name="fwd"></param>
        /// <param name="k"></param>
        /// <param name="t"></param>
        /// <param name="alpha"></param>
        /// <param name="beta"></param>
        /// <param name="rho"></param>
        /// <param name="nu"></param>
        /// <returns></returns>
        public static double CalcImpVol_Berestycki(double fwd, double k, double t, double alpha, double beta, double rho, double nu)
        {
            double sigma;
            if (fwd != k)
            {
                var x = System.Math.Log(fwd / k);
                var z = nu / alpha * (System.Math.Pow(fwd, 1 - beta) - System.Math.Pow(k, 1 - beta)) / (1 - beta);
                var q = System.Math.Log((System.Math.Sqrt(1 - 2 * rho * z + z * z) + z - rho) / (1 - rho));
                //double e = nu / alpha * (fwd - k) / System.Math.Pow(fwd * k, beta / 2);

                var I0 = nu * x / q;
                var I1 = System.Math.Pow((beta - 1) * alpha, 2) / (24 * System.Math.Pow(fwd * k, 1 - beta)) + 0.25 * rho * nu * alpha * beta / System.Math.Pow(fwd * k, (1 - beta) / 2) + (2 - 3 * rho * rho) / 24 * nu * nu;
                sigma = I0 * (1 + I1 * t);
            }
            else
            {
                var I0 = alpha * System.Math.Pow(k, beta - 1);
                var I1 = System.Math.Pow((beta - 1) * alpha, 2) / (24 * System.Math.Pow(fwd * k, 1 - beta)) + 0.25 * rho * nu * alpha * beta / System.Math.Pow(fwd * k, (1 - beta) / 2) + (2 - 3 * rho * rho) / 24 * nu * nu;
                sigma = I0 * (1 + I1 * t);
            }

            return sigma;
        }

        /// <summary>
        /// Compute implied vol from a set of SABR parameters, Hagan algo
        /// </summary>
        /// <param name="fwd"></param>
        /// <param name="k"></param>
        /// <param name="t"></param>
        /// <param name="alpha"></param>
        /// <param name="beta"></param>
        /// <param name="rho"></param>
        /// <param name="nu"></param>
        /// <returns></returns>
        public static double CalcImpVol_Hagan(double fwd, double k, double t, double alpha, double beta, double rho, double nu)
        {
            double t1;
            double t2;
            double t5;
            double t6;
            double t9;
            double t10;
            double t16;
            double t17;
            double t18;
            double t20;
            double t25;
            double t26;
            double t29;
            double t33;
            double t41;
            double t45;
            double t54;
            if (k == fwd)
            {
                k *= (1 + 0.00000001);
            }
            if (k == 0.0)
            {
                k = 0.00000001;
            }
            t1 = 1.0 - beta;
            t2 = t1 * t1;
            t5 = System.Math.Log(fwd / k);
            t6 = t5 * t5;
            t9 = t2 * t2;
            t10 = t6 * t6;
            t16 = rho * nu;
            t17 = 1.0 / alpha;
            t18 = k * fwd;
            t20 = System.Math.Pow(t18, (t1 / 2.0));
            t25 = nu * nu;
            t26 = alpha * alpha;
            t29 = t20 * t20;
            t33 = System.Math.Pow((1.0 - 2.0 * t16 * t17 * t20 * t5 + t25 / t26 * t29 * t6), 0.5);
            t41 = System.Math.Log((t33 + nu * t17 * t20 * t5 - rho) / (1.0 - rho));
            t45 = System.Math.Pow(t18, t1);
            t54 = rho * rho;
            return 1.0 / (1.0 + t2 * t6 / 24.0 + t9 * t10 / 1920.0) * nu * t5 / t41 * (1.0 + (t2 * t26 / t45 / 24.0 + alpha * beta * t16 / t20 / 4.0 + (2.0 - 3.0 * t54) * t25 / 24.0) * t);
        }

        /// <summary>
        /// Computes implied vol for a set of modified SABR parameters
        /// Note beta == 1 and the form has been modified to make ATM-forward vol insensitive to rho an nu
        /// </summary>
        /// <param name="fwd"></param>
        /// <param name="k"></param>
        /// <param name="t"></param>
        /// <param name="alpha"></param>
        /// <param name="rho"></param>
        /// <param name="nu"></param>
        /// <returns></returns>
        public static double CalcImpVol_GB(double fwd, double k, double t, double alpha, double rho, double nu)
        {
            double sigma;
            if (fwd != k)
            {
                var lk = System.Math.Log(fwd / k);
                var y = nu / alpha * lk;
                var x = System.Math.Log((System.Math.Sqrt(1 - 2 * rho * y + y * y) + y - rho) / (1 - rho));

                if (x == y) //trap case of 0/0
                {
                    x = 1;
                    y = 1;
                }

                //sigma = alpha * (y / x) * (1 + (0.25 * rho * nu * alpha + (2 - 3 * rho * rho) / 24 * nu * nu) * t *System.Math.Pow(System.Math.Log(fwd / k),2));
                sigma = alpha * (y / x) * (1 + (0.25 * rho * nu * alpha + (2 - 3 * rho * rho) / 24 * nu * nu) * t * System.Math.Pow(lk, 2));
            }
            else
            {
                sigma = alpha;
            }

            return sigma;

        }

        /// <summary>
        /// Computes implied vol for the log-normal-vol (beta==1) case
        /// </summary>
        /// <param name="fwd"></param>
        /// <param name="k"></param>
        /// <param name="t"></param>
        /// <param name="alpha"></param>
        /// <param name="rho"></param>
        /// <param name="nu"></param>
        /// <returns></returns>
        public static double CalcImpVol_Beta1(double fwd, double k, double t, double alpha, double rho, double nu)
        {
            double sigma;
            if (fwd != k)
            {
                var y = nu / alpha * System.Math.Log(fwd / k);
                var x = System.Math.Log((System.Math.Sqrt(1 - 2 * rho * y + y * y) + y - rho) / (1 - rho));

                if (x == y) //trap case of 0/0
                {
                    x = 1;
                    y = 1;
                }

                sigma = alpha * (y / x) * (1 + (0.25 * rho * nu * alpha + (2 - 3 * rho * rho) / 24 * nu * nu) * t);
            }
            else
            {
                sigma = alpha * (1 + (0.25 * rho * nu * alpha + (2 - 3 * rho * rho) / 24 * nu * nu) * t);
            }

            return sigma;
        }
    }
}
