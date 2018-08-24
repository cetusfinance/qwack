using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Options;
using Qwack.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Options.VolSurfaces
{
    /// <summary>
    /// A volatility surface based on the SABR parameterization
    /// Can be built from SABR parameters directly or fitted to strike/vol pairs
    /// Interpolates in SABR parameter space in the time dimension using the method specified
    /// </summary>
    public class SabrVolSurface : IVolSurface
    {
        public string Name { get; set; }
        public DateTime OriginDate { get; set; }
        public double[] Alphas { get; set; }
        public double[] Betas { get; set; }
        public double[] Rhos { get; set; }
        public double[] Nus { get; set; }
        public DateTime[] Expiries { get; set; }
        public double[] ExpiriesDouble { get; set; }

        public Interpolator1DType TimeInterpolatorType { get; set; } = Interpolator1DType.LinearFlatExtrap;
        public DayCountBasis TimeBasis { get; set; } = DayCountBasis.Act365F;

        private IInterpolator1D _alphaInterp;
        private IInterpolator1D _betaInterp;
        private IInterpolator1D _rhoInterp;
        private IInterpolator1D _nuInterp;

        public SabrVolSurface() { }

        public SabrVolSurface(DateTime originDate, double[][] strikes, DateTime[] expiries, double[][] vols, Func<double, double> forwardCurve) => Build(originDate, strikes, expiries, vols, forwardCurve);

        public SabrVolSurface(DateTime originDate, double[][] strikes, DateTime[] expiries, double[][] vols, Func<double, double> forwardCurve,
            Interpolator1DType timeInterpType, DayCountBasis timeBasis)
        {
            TimeInterpolatorType = timeInterpType;
            TimeBasis = timeBasis;

            Build(originDate, strikes, expiries, vols, forwardCurve);
        }

        public void Build(DateTime originDate, double[][] strikes, DateTime[] expiries, double[][] vols, Func<double, double> forwardCurve)
        {
            OriginDate = originDate;
            Expiries = expiries;
            ExpiriesDouble = Expiries.Select(t => TimeBasis.CalculateYearFraction(originDate, t)).ToArray();

            if (Expiries.Length != strikes.Length)
            {
                throw new InvalidOperationException("Expiries and first dimension of Strikes must of same length");
            }
            if (Expiries.Length != vols.Length)
            {
                throw new InvalidOperationException("Expiries and first dimension of Vols must of same length");
            }

            Alphas = new double[Expiries.Length];
            Betas = new double[Expiries.Length];
            Nus = new double[Expiries.Length];
            Rhos = new double[Expiries.Length];


            for (var i = 0; i < expiries.Length; i++)
            {
                var vs = vols[i];
                var ks = strikes[i];
                var t = ExpiriesDouble[i];
                var fwd = forwardCurve(t);
                Betas[i] = 1.0;
                Func<double[], double[]> errorFunc = (x =>
                    {
                        var err = ks.Select((k, ix) => vs[ix] - SABR.CalcImpVol_Beta1(fwd, k, t, x[0], x[1], x[2]));
                        return err.ToArray();
                    });

                var n2Sol = new Math.Solvers.GaussNewton
                {
                    ObjectiveFunction = errorFunc,
                    InitialGuess = new double[] { vs.Average(), 0.1, 0.1 },
                    Tollerance = 1e-8,
                    JacobianBump = 0.0000001
                };

                var paramArr = n2Sol.Solve();

                Alphas[i] = paramArr[0];
                Rhos[i] = paramArr[1];
                Nus[i] = paramArr[2];
            }

            _alphaInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Alphas, TimeInterpolatorType);
            _betaInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Betas, TimeInterpolatorType);
            _rhoInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Rhos, TimeInterpolatorType);
            _nuInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Nus, TimeInterpolatorType);
        }

        public double GetVolForAbsoluteStrike(double strike, double maturity, double forward)
        {
            var alpha = _alphaInterp.Interpolate(maturity);
            var beta = _betaInterp.Interpolate(maturity);
            var nu = _nuInterp.Interpolate(maturity);
            var rho = _rhoInterp.Interpolate(maturity);
            var fwd = forward;
            if (beta >= 1.0)
                return SABR.CalcImpVol_Beta1(fwd, strike, maturity, alpha, rho, nu);
            else
                return SABR.CalcImpVol_Hagan(fwd, strike, maturity, alpha, beta, rho, nu);
        }

        public double GetVolForAbsoluteStrike(double strike, DateTime expiry, double forward) => GetVolForAbsoluteStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry), forward);

        public double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward)
        {
            var fwd = forward;
            var cp = deltaStrike < 0 ? OptionType.Put : OptionType.Call;

            Func<double, double> testFunc = (absK =>
            {
                var vol = GetVolForAbsoluteStrike(absK, maturity, forward);
                var deltaK = BlackFunctions.BlackDelta(fwd, absK, 0, maturity, vol, cp);
                return deltaK - System.Math.Abs(deltaStrike);
            });

            var solvedStrike = Math.Solvers.Brent.BrentsMethodSolve(testFunc, 0.000000001, 10 * fwd, 1e-8);

            return GetVolForAbsoluteStrike(solvedStrike, maturity, forward);
        }

        public double GetVolForDeltaStrike(double strike, DateTime expiry, double forward) => GetVolForDeltaStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry), forward);
    }
}
