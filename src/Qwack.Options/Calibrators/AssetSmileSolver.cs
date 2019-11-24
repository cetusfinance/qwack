using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Math.Solvers;
using static System.Math;
using static Qwack.Options.BlackFunctions;

namespace Qwack.Options.Calibrators
{
    public class AssetSmileSolver
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double JacobianBump = 0.0001;

        private DateTime _buildDate;

        private RRBFConstraint[] _smileConstraints;
        private ATMStraddleConstraint _atmConstraint;

        private SABRParameters _currentSABR;
        private SviRawParameters _currentSVIRaw;
        private SviNaturalParameters _currentSVINatural;

        private int _numberOfConstraints;
        private double _fwd;
        private double _tExp;
        private Interpolator1DType _interpType;
        private IInterpolator1D _interp;
        private double[] _strikes;

        private double[] _currentGuess;
        private double[] _currentErrors;
        private double[][] _jacobian;

        private bool _vegaWeighted;

        public double[] Solve(ATMStraddleConstraint atmConstraint, RRBFConstraint[] smileConstraints, DateTime buildDate, 
            DateTime expiry, double fwd, double[] strikesToFit, Interpolator1DType interpType)
        {
            _atmConstraint = atmConstraint;
            _smileConstraints = smileConstraints;

            _numberOfConstraints = smileConstraints.Length * 2 + 1;

            if (strikesToFit.Length != _numberOfConstraints)
                throw new Exception($"{_numberOfConstraints} constraints provided to fit {strikesToFit.Length} strikes");

            _fwd = fwd;
            _buildDate = buildDate;
            _tExp = (expiry - buildDate).TotalDays / 365.0;
            _interpType = interpType;
            _strikes = strikesToFit;

            if (TrivialSolution(out var vols))
                return vols;

            _currentGuess = Enumerable.Repeat(atmConstraint.MarketVol, _numberOfConstraints).ToArray();

            SetupConstraints();

            _currentErrors = ComputeErrors(_currentGuess);

            ComputeJacobian();

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                NaNCheck();
                _currentErrors = ComputeErrors(_currentGuess);
                if (_currentErrors.Max(x => Abs(x)) < Tollerance)
                {
                    UsedItterations = i + 1;
                    break;
                }
                ComputeJacobian();
            }

            return _currentGuess;
        }

        public SABRParameters SolveSABR(ATMStraddleConstraint atmConstraint, RRBFConstraint[] smileConstraints, DateTime buildDate,
        DateTime expiry, double fwd, double beta = 1.0, bool vegaWeightedFit=true)
        {
            _atmConstraint = atmConstraint;
            _smileConstraints = smileConstraints;

            _numberOfConstraints = smileConstraints.Length * 2 + 1;
            _vegaWeighted = vegaWeightedFit;

            _fwd = fwd;
            _buildDate = buildDate;
            _tExp = (expiry - buildDate).TotalDays / 365.0;

            var startingPoint = new[] { atmConstraint.MarketVol, Sqrt(atmConstraint.MarketVol), smileConstraints.Average(x => x.RisykVol) >= 0 ? 0.1 : -0.1 };
            var initialStep = new[] { 0.1, 0.25, 0.25 };

            var currentError = new Func<double[], double>(x =>
            {
                var currentSABR = new SABRParameters
                {
                    Alpha = x[0],
                    Beta = beta,
                    Nu = x[1],
                    Rho = x[2],
                };

                var e = ComputeErrorsSABR(currentSABR);
                return e.Sum(ee => ee * ee);
            });

            SetupConstraints();

            var optimal = NelderMead.MethodSolve(currentError, startingPoint, initialStep, 1e-8, 10000);

            return new SABRParameters
            {
                Alpha = optimal[0],
                Beta = beta,
                Nu = optimal[1],
                Rho = optimal[2],
            };
        }

        public SviRawParameters SolveSviRaw(ATMStraddleConstraint atmConstraint, RRBFConstraint[] smileConstraints, DateTime buildDate,
            DateTime expiry, double fwd, bool vegaWeightedFit = true)
        {
            _atmConstraint = atmConstraint;
            _smileConstraints = smileConstraints;

            _numberOfConstraints = smileConstraints.Length * 2 + 1;
            _vegaWeighted = vegaWeightedFit;

            _fwd = fwd;
            _buildDate = buildDate;
            _tExp = (expiry - buildDate).TotalDays / 365.0;

            var startingPoint = new[] { atmConstraint.MarketVol* atmConstraint.MarketVol *_tExp-Sqrt(atmConstraint.MarketVol), 1.0, smileConstraints.Average(x => x.RisykVol) >= 0 ? 0.1 : -0.1, 0, Sqrt(atmConstraint.MarketVol) };
            //var startingPoint = new[] { atmConstraint.MarketVol* atmConstraint.MarketVol *_tExp-Sqrt(atmConstraint.MarketVol), 0.5, smileConstraints.Average(x => x.RisykVol) >= 0 ? 0.25 : -0.25, 0, Sqrt(atmConstraint.MarketVol) };
            var initialStep = new[] { atmConstraint.MarketVol * atmConstraint.MarketVol, 0.5, 0.5, 0.002, Sqrt(atmConstraint.MarketVol) /2};

            //var startingPoint = new[] { atmConstraint.MarketVol, 1.0, 0.1, 0, 0.1 };
            //var initialStep = new[] { 0.1, 0.25, 0.25, 0.01, 0.1 };


            var currentError = new Func<double[], double>(x =>
            {
                var currentSVI = new SviRawParameters
                {
                    A = x[0],
                    B = x[1],
                    Rho = x[2],
                    M = x[3],
                    Sigma = x[4],
                };

                var e = ComputeErrorsSviRaw(currentSVI);
                return Sqrt(e.Sum());
            });

            SetupConstraints();

            var optimal = NelderMead.MethodSolve(currentError, startingPoint, initialStep, 1e-10, 50000);

            return new SviRawParameters
            {
                A = optimal[0],
                B = optimal[1],
                Rho = optimal[2],
                M = optimal[3],
                Sigma = optimal[4],
            };
        }

        private void NaNCheck()
        {
            if (_currentGuess.Any(x => double.IsNaN(x)))
                throw new Exception("NaNs detected in solution");
        }

        private bool TrivialSolution(out double[] vols)
        {
            vols = new double[_strikes.Length];
            var sc = _smileConstraints.OrderBy(x => x.Delta).ToArray();
            var atm = _atmConstraint.MarketVol;
            if (sc.Length * 2 + 1 == _strikes.Length && sc[0].WingQuoteType==WingQuoteType.Simple)
            {
                var deltas = sc.Select(x => x.Delta).ToArray();
                for(var i=0;i<deltas.Length;i++)
                {
                    if (_strikes[i] != deltas[i] || _strikes[_strikes.Length-1-i] != 1 - deltas[i])
                        return false;

                    vols[i] = atm + sc[i].FlyVol - 0.5 * sc[i].RisykVol;
                    vols[vols.Length-1-i] = atm + sc[i].FlyVol + 0.5 * sc[i].RisykVol;
                }
                vols[deltas.Length] = atm;
                return true;
            }
            return false;
        }

        private void SetupConstraints()
        {
            var atm = _atmConstraint.MarketVol;
            if (_atmConstraint.ATMVolType == AtmVolType.ZeroDeltaStraddle)
            {
                _atmConstraint.CallStrike = AbsoluteStrikefromDeltaKAnalytic(_fwd, 0.5, 0, _tExp, atm);
                _atmConstraint.PutStrike = _atmConstraint.CallStrike;
            }
            else //assume ATMF
            {
                _atmConstraint.CallStrike = _fwd;
                _atmConstraint.PutStrike = _fwd;
            }

            _atmConstraint.CallFV = BlackPV(_fwd, _atmConstraint.CallStrike, 0, _tExp, atm, OptionType.C);
            _atmConstraint.PutFV = BlackPV(_fwd, _atmConstraint.PutStrike, 0, _tExp, atm, OptionType.P);

            for (var i = 0; i < _smileConstraints.Length; i++)
            {
                var ss = _smileConstraints[i];
                var rr = ss.RisykVol;
                var bf = ss.FlyVol;
                switch (ss.WingQuoteType)
                {
                    case WingQuoteType.Arithmatic:
                        {
                            ss.RRCallVol = atm + bf + rr / 2;
                            ss.RRPutVol = atm + bf - rr / 2;
                            ss.BFCallVol = ss.RRCallVol;
                            ss.BFPutVol = ss.RRPutVol;
                            break;
                        }
                    case WingQuoteType.Market:
                        {
                            ss.RRCallVol = atm + bf + rr / 2;
                            ss.RRPutVol = atm + bf - rr / 2;
                            ss.BFCallVol = atm + bf;
                            ss.BFPutVol = atm + bf;
                            break;
                        }
                }

                ss.RRCallStrike = AbsoluteStrikefromDeltaKAnalytic(_fwd, ss.Delta, 0, _tExp, ss.RRCallVol);
                ss.RRPutStrike = AbsoluteStrikefromDeltaKAnalytic(_fwd, -ss.Delta, 0, _tExp, ss.RRPutVol);
                ss.BFCallStrike = AbsoluteStrikefromDeltaKAnalytic(_fwd, ss.Delta, 0, _tExp, ss.BFCallVol);
                ss.BFPutStrike = AbsoluteStrikefromDeltaKAnalytic(_fwd, -ss.Delta, 0, _tExp, ss.BFPutVol);

                ss.RRCallFV = BlackPV(_fwd, ss.RRCallStrike, 0, _tExp, ss.RRCallVol, OptionType.C);
                ss.RRPutFV = BlackPV(_fwd, ss.RRPutStrike, 0, _tExp, ss.RRPutVol, OptionType.P);
                ss.BFCallFV = BlackPV(_fwd, ss.BFCallStrike, 0, _tExp, ss.BFCallVol, OptionType.C);
                ss.BFPutFV = BlackPV(_fwd, ss.BFPutStrike, 0, _tExp, ss.BFPutVol, OptionType.P);
            }
        }

        private void ComputeNextGuess()
        {
            var jacobianMi = Math.Matrix.DoubleArrayFunctions.InvertMatrix(_jacobian);
            var deltaGuess = Math.Matrix.DoubleArrayFunctions.MatrixProduct(_currentErrors, jacobianMi);
            for (var j = 0; j < _numberOfConstraints; j++)
            {
                _currentGuess[j] -= deltaGuess[j];
            }
        }

        private void ComputeJacobian()
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_numberOfConstraints, _numberOfConstraints);

            for (var i = 0; i < _numberOfConstraints; i++)
            {
                var bumpedErrors = ComputeErrors(_currentGuess.Select((x, ix) => ix == i ? x + JacobianBump : x).ToArray());

                for (var j = 0; j < bumpedErrors.Length; j++)
                {
                    _jacobian[i][j] = (bumpedErrors[j] - _currentErrors[j]) / JacobianBump;
                }
            }
        }

        private double[] ComputeErrors(double[] currentGuess)
        {
            var interp = InterpolatorFactory.GetInterpolator(_strikes, currentGuess, _interpType);
            var volFunc = new Func<double, double>(k => GetVolForAbsoluteStrike(k, interp));
            var o = ComputeErrorsGeneric(volFunc, false);
            return o;
        }

        private double[] ComputeErrorsSABR(SABRParameters currentSABR)
        {
            var volFunc = new Func<double, double>(k => CalcImpVol_Beta1(k, currentSABR));
            var o = ComputeErrorsGeneric(volFunc, true);
            if (currentSABR.Rho > 1.0 || currentSABR.Rho < -1.0 || currentSABR.Alpha < 0.0 || currentSABR.Nu < 0.0)
                o = o.Select(x => x * 1e10).ToArray();
            return o;
        }

        private double[] ComputeErrorsSviRaw(SviRawParameters currentSviRaw)
        {
            var volFunc = new Func<double, double>(k => CalcVolSviRaw(k, currentSviRaw));
            var o = ComputeErrorsGeneric(volFunc, true);
            if (currentSviRaw.B < 0.0 || currentSviRaw.B > 4.0/(_tExp*(1+Abs(currentSviRaw.Rho))) || currentSviRaw.Sigma <= 0.0 || currentSviRaw.Sigma > 10.0 || currentSviRaw.Rho < -1.0 || currentSviRaw.Rho > 1.0)
                o = o.Select(x => x * 1e100).ToArray();
            return o;
        }

        private double[] ComputeErrorsGeneric(Func<double,double> volForStrike, bool squareErrors)
        {
            double callVol, putVol;

            var o = new double[_numberOfConstraints];
            for (var i = 0; i < _smileConstraints.Length; i++)
            {
                callVol = volForStrike(_smileConstraints[i].RRCallStrike);
                putVol = volForStrike(_smileConstraints[i].RRPutStrike);
                var callFV = BlackPV(_fwd, _smileConstraints[i].RRCallStrike, 0, _tExp, callVol, OptionType.C);
                var putFV = BlackPV(_fwd, _smileConstraints[i].RRPutStrike, 0, _tExp, putVol, OptionType.P);
                var onSmileRRFV = callFV - putFV;
                var constraintRRFV = _smileConstraints[i].RRCallFV - _smileConstraints[i].RRPutFV;
                o[i] = onSmileRRFV - constraintRRFV;

                if (_vegaWeighted)
                {
                    var vega = ((BlackVega(_fwd, _smileConstraints[i].RRCallStrike, 0, _tExp, callVol)
                        + BlackVega(_fwd, _smileConstraints[i].RRPutStrike, 0, _tExp, putVol)) / 2.0);

                    o[i] *= vega;
                }
            }

            var a = _smileConstraints.Length;
            callVol = volForStrike(_atmConstraint.CallStrike);
            putVol = volForStrike(_atmConstraint.PutStrike);
            o[a] = BlackPV(_fwd, _atmConstraint.CallStrike, 0, _tExp, callVol, OptionType.C)
                + BlackPV(_fwd, _atmConstraint.PutStrike, 0, _tExp, putVol, OptionType.P);
            o[a] -= (_atmConstraint.CallFV + _atmConstraint.PutFV);

            if (_vegaWeighted)
            {
                var vega = (BlackVega(_fwd, _atmConstraint.CallStrike, 0, _tExp, callVol));
                o[a] *= vega;
            }

            a++;

            for (var i = 0; i < _smileConstraints.Length; i++)
            {
                callVol = volForStrike(_smileConstraints[i].BFCallStrike);
                putVol = volForStrike(_smileConstraints[i].BFPutStrike);
                var callFV = BlackPV(_fwd, _smileConstraints[i].BFCallStrike, 0, _tExp, callVol, OptionType.C);
                var putFV = BlackPV(_fwd, _smileConstraints[i].BFPutStrike, 0, _tExp, putVol, OptionType.P);
                var onSmileBFFV = callFV + putFV;
                var constraintBFFV = _smileConstraints[i].BFCallFV + _smileConstraints[i].BFPutFV;
                o[i + a] = onSmileBFFV - constraintBFFV;

                if (_vegaWeighted)
                {
                    var vega = ((BlackVega(_fwd, _smileConstraints[i].BFCallStrike, 0, _tExp, callVol)
                        + BlackVega(_fwd, _smileConstraints[i].BFPutStrike, 0, _tExp, putVol)) / 2.0);

                    o[i + a] *= vega;
                }
            }

            if (squareErrors)
            {
                o = o.Select(x => x * x).ToArray();
            }

            return o;
        }

        private void UpdateInterpolator() => _interp = InterpolatorFactory.GetInterpolator(_strikes, _currentGuess, _interpType);

   

        private double GetVolForAbsoluteStrike(double strike, IInterpolator1D interp)
        {
            var cp = strike < 0 ? OptionType.Put : OptionType.Call;
            Func<double, double> testFunc = (deltaK =>
            {
                var vol = interp.Interpolate(-deltaK);
                var absK = AbsoluteStrikefromDeltaKAnalytic(_fwd, deltaK, 0, _tExp, vol);
                return absK - strike;
            });

            var solvedStrike = -Brent.BrentsMethodSolve(testFunc, -0.999999999, -0.000000001, 1e-8);
            return interp.Interpolate(solvedStrike);
        }

        public double CalcImpVol_Beta1(double k, SABRParameters currentSABR) => SABR.CalcImpVol_Beta1(_fwd, k, _tExp, currentSABR.Alpha, currentSABR.Rho, currentSABR.Nu);

        public double CalcVolSviRaw(double k, SviRawParameters currentSVI) => SVI.SVI_Raw_ImpliedVol(currentSVI.A, currentSVI.B, currentSVI.Rho, k, _fwd, _tExp, currentSVI.M, currentSVI.Sigma);
    }

    public class RRBFConstraint
    {
        public double Delta;
        public double RisykVol;
        public double FlyVol;
        public WingQuoteType WingQuoteType;
        public double RRCallStrike;
        public double RRCallVol;
        public double RRCallFV;
        public double RRPutStrike;
        public double RRPutVol;
        public double RRPutFV;
        public double BFCallStrike;
        public double BFCallVol;
        public double BFCallFV;
        public double BFPutStrike;
        public double BFPutVol;
        public double BFPutFV;
    }


    public class ATMStraddleConstraint
    {
        public double MarketVol;
        public AtmVolType ATMVolType;
        public double CallStrike;
        public double CallFV;
        public double PutStrike;
        public double PutFV;
    }
}

