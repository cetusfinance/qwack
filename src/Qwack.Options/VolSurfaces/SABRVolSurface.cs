using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Options;
using Qwack.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Calibrators;

namespace Qwack.Options.VolSurfaces
{
    /// <summary>
    /// A volatility surface based on the SABR parameterization
    /// Can be built from SABR parameters directly or fitted to strike/vol pairs
    /// Interpolates in SABR parameter space in the time dimension using the method specified
    /// </summary>
    public class SabrVolSurface : IATMVolSurface
    {
        public string Name { get; set; }
        public DateTime OriginDate { get; set; }
        public double[] Alphas { get; set; }
        public double[] Betas { get; set; }
        public double[] Rhos { get; set; }
        public double[] Nus { get; set; }
        public DateTime[] Expiries { get; set; }
        public double[] ExpiriesDouble { get; set; }

        public Currency Currency { get; set; }

        public Interpolator1DType TimeInterpolatorType { get; set; } = Interpolator1DType.LinearFlatExtrap;
        public DayCountBasis TimeBasis { get; set; } = DayCountBasis.Act365F;
        public string AssetId { get; set; }
        public IInterpolator2D LocalVolGrid { get; set; }
        public string[] PillarLabels { get; }

        private IInterpolator1D _alphaInterp;
        private IInterpolator1D _betaInterp;
        private IInterpolator1D _rhoInterp;
        private IInterpolator1D _nuInterp;
        private IInterpolator1D _fwdsInterp;

        public SabrVolSurface() { }

        public SabrVolSurface(DateTime originDate, double[][] strikes, DateTime[] expiries, double[][] vols, Func<double, double> forwardCurve) => Build(originDate, strikes, expiries, vols, forwardCurve);

        public SabrVolSurface(DateTime originDate, double[][] strikes, DateTime[] expiries, double[][] vols, Func<double, double> forwardCurve,
            Interpolator1DType timeInterpType, DayCountBasis timeBasis)
        {
            TimeInterpolatorType = timeInterpType;
            TimeBasis = timeBasis;

            Build(originDate, strikes, expiries, vols, forwardCurve);
        }

        public SabrVolSurface(DateTime originDate, double[] ATMVols, DateTime[] expiries, double[] wingDeltas,
          double[][] riskies, double[][] flies, double[] fwds, WingQuoteType wingQuoteType, AtmVolType atmVolType,
          Interpolator1DType timeInterpType, string[] pillarLabels = null)
        {
            if (pillarLabels == null)
                PillarLabels = expiries.Select(x => x.ToString("yyyy-MM-dd")).ToArray();
            else
                PillarLabels = pillarLabels;

            if (ATMVols.Length != expiries.Length || expiries.Length != riskies.Length || riskies.Length != flies.Length)
                throw new Exception("Inputs do not have consistent time dimensions");

            if (wingDeltas.Length != riskies[0].Length || riskies[0].Length != flies[0].Length)
                throw new Exception("Inputs do not have consistent strike dimensions");

            var atmConstraints = ATMVols.Select(a => new ATMStraddleConstraint
            {
                ATMVolType = atmVolType,
                MarketVol = a
            }).ToArray();

            var needsFlip = wingDeltas.First() > wingDeltas.Last();
            var strikes = new double[2 * wingDeltas.Length + 1];
            if (needsFlip)
            {
                for (var s = 0; s < wingDeltas.Length; s++)
                {
                    strikes[s] = wingDeltas[wingDeltas.Length - 1 - s];
                    strikes[strikes.Length - 1 - s] = 1.0 - wingDeltas[wingDeltas.Length - 1 - s];
                }
            }
            else
            {
                for (var s = 0; s < wingDeltas.Length; s++)
                {
                    strikes[s] = wingDeltas[s];
                    strikes[strikes.Length - 1 - s] = 1.0 - wingDeltas[s];
                }
            }
            strikes[wingDeltas.Length] = 0.5;

            var wingConstraints = new RRBFConstraint[expiries.Length][];
            var parameters = new SABRParameters[expiries.Length];
            var f = new NewtonRaphsonAssetSmileSolver();

            if (needsFlip)
            {
                for (var i = 0; i < wingConstraints.Length; i++)
                {
                    var offset = wingDeltas.Length - 1;
                    wingConstraints[i] = new RRBFConstraint[wingDeltas.Length];
                    for (var j = 0; j < wingConstraints[i].Length; j++)
                    {
                        wingConstraints[i][j] = new RRBFConstraint
                        {
                            Delta = wingDeltas[offset - j],
                            FlyVol = flies[i][offset - j],
                            RisykVol = riskies[i][offset - j],
                            WingQuoteType = wingQuoteType,
                        };
                    }
                    parameters[i] = f.SolveSABR(atmConstraints[i], wingConstraints[i], originDate, expiries[i], fwds[i], 1.0);
                }
            }
            else
            {
                for (var i = 0; i < wingConstraints.Length; i++)
                {
                    wingConstraints[i] = new RRBFConstraint[wingDeltas.Length];
                    for (var j = 0; j < wingConstraints[i].Length; j++)
                    {
                        wingConstraints[i][j] = new RRBFConstraint
                        {
                            Delta = wingDeltas[j],
                            FlyVol = flies[i][j],
                            RisykVol = riskies[i][j],
                            WingQuoteType = wingQuoteType,
                        };
                    }
                    parameters[i] = f.SolveSABR(atmConstraints[i], wingConstraints[i], originDate, expiries[i], fwds[i], 1.0);
                }
            }

            OriginDate = originDate;
            TimeInterpolatorType = timeInterpType;
            Expiries = expiries;
            ExpiriesDouble = expiries.Select(x => originDate.CalculateYearFraction(x,TimeBasis)).ToArray();

            Alphas = parameters.Select(x => x.Alpha).ToArray();
            Betas = parameters.Select(x => x.Beta).ToArray();
            Nus = parameters.Select(x => x.Nu).ToArray();
            Rhos = parameters.Select(x => x.Rho).ToArray();

            _alphaInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble,  Alphas, TimeInterpolatorType);
            _betaInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Betas, TimeInterpolatorType);
            _rhoInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Rhos, TimeInterpolatorType);
            _nuInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Nus, TimeInterpolatorType);
            _fwdsInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, fwds, TimeInterpolatorType);
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

            var fwds = ExpiriesDouble.Select(x => forwardCurve(x)).ToArray();

            _alphaInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Alphas, TimeInterpolatorType);
            _betaInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Betas, TimeInterpolatorType);
            _rhoInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Rhos, TimeInterpolatorType);
            _nuInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, Nus, TimeInterpolatorType);
            _fwdsInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, fwds, TimeInterpolatorType);
        }

        public static double GetVolForAbsoluteStrike(double strike, double maturity, double forward, SABRParameters sabrParams) => sabrParams.Beta >= 1.0
                ? SABR.CalcImpVol_Beta1(forward, strike, maturity, sabrParams.Alpha, sabrParams.Rho, sabrParams.Nu)
                : SABR.CalcImpVol_Hagan(forward, strike, maturity, sabrParams.Alpha, sabrParams.Beta, sabrParams.Rho, sabrParams.Nu);

        public double GetVolForAbsoluteStrike(double strike, double maturity, double forward) => GetVolForAbsoluteStrike(strike, maturity, forward, new SABRParameters
        {
            Alpha = _alphaInterp.Interpolate(maturity),
            Beta = _betaInterp.Interpolate(maturity),
            Nu = _nuInterp.Interpolate(maturity),
            Rho = _rhoInterp.Interpolate(maturity)
        });

        public double GetVolForAbsoluteStrike(double strike, DateTime expiry, double forward) => GetVolForAbsoluteStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry), forward);

        public double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward)
        {
            var fwd = forward;
            var cp = OptionType.Put;

            Func<double, double> testFunc = (absK =>
            {
                var vol = GetVolForAbsoluteStrike(absK, maturity, forward);
                var deltaK = System.Math.Abs(BlackFunctions.BlackDelta(fwd, absK, 0, maturity, vol, cp));
                return deltaK - System.Math.Abs(deltaStrike);
            });

            var solvedStrike = Math.Solvers.Brent.BrentsMethodSolve(testFunc, fwd/10, 10 * fwd, 1e-8);

            return GetVolForAbsoluteStrike(solvedStrike, maturity, forward);
        }

        public double GetVolForDeltaStrike(double strike, DateTime expiry, double forward) => GetVolForDeltaStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry), forward);

        public Dictionary<string, IVolSurface> GetATMVegaScenarios(double bumpSize, DateTime? LastSensitivityDate) => throw new NotImplementedException();

        public DateTime PillarDatesForLabel(string label) => throw new NotImplementedException();

        public double GetForwardATMVol(DateTime startDate, DateTime endDate) => GetForwardATMVol(TimeBasis.CalculateYearFraction(OriginDate, startDate), TimeBasis.CalculateYearFraction(OriginDate, endDate));

        public double GetForwardATMVol(double start, double end)
        {
            if (start > end)
                throw new Exception("Start must be strictly less than end");

            var fwdStart = _fwdsInterp.Interpolate(start);
            var fwdEnd = _fwdsInterp.Interpolate(end);
            if (start == end)
                return start == 0 ? 0.0 : GetVolForAbsoluteStrike(fwdStart, start, fwdStart);

            var vStart = start == 0 ? 0.0 : GetVolForDeltaStrike(fwdStart, start, fwdStart);
            vStart *= vStart * start;

            var vEnd = GetVolForDeltaStrike(fwdEnd, end, fwdEnd);
            vEnd *= vEnd * end;

            var vDiff = vEnd - vStart;
            if (vDiff < 0)
                throw new Exception("Negative forward variance detected");

            return System.Math.Sqrt(vDiff / (end - start));
        }
    }
}
