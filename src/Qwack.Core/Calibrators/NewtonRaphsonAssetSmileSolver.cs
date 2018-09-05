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
using static System.Math;

namespace Qwack.Core.Calibrators
{
    public class NewtonRaphsonAssetSmileSolver
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double JacobianBump = 0.0001;

        private DateTime _buildDate;

        private RRBFConstraint[] _smileConstraints;
        private ATMStraddleConstraint _atmConstraint;

        private int _numberOfConstraints;
        private double _fwd;
        private double _tExp;
        private Interpolator1DType _interpType;
        private IInterpolator1D _interp;
        private double[] _strikes;

        private int _numberOfPillars;
        private double[] _currentGuess;
        private double[] _currentErrors;
        private double[][] _jacobian;

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
            _currentGuess = Enumerable.Repeat(atmConstraint.MarketVol, _numberOfConstraints).ToArray();

            SetupConstraints();

            _currentErrors = ComputeErrors(_currentGuess);

            ComputeJacobian();

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();

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
            double callVol, putVol;

            var o = new double[_numberOfConstraints];
            for (var i = 0; i < _smileConstraints.Length; i++)
            {
                callVol = GetVolForAbsoluteStrike(_smileConstraints[i].RRCallStrike, interp);
                putVol = GetVolForAbsoluteStrike(_smileConstraints[i].RRPutStrike, interp);
                var callFV = BlackPV(_fwd, _smileConstraints[i].RRCallStrike, 0, _tExp, callVol, OptionType.C);
                var putFV = BlackPV(_fwd, _smileConstraints[i].RRPutStrike, 0, _tExp, putVol, OptionType.P);
                var onSmileRRFV = callFV - putFV;
                var constraintRRFV = _smileConstraints[i].RRCallFV - _smileConstraints[i].RRPutFV;
                o[i] = onSmileRRFV - constraintRRFV;
            }

            var a = _smileConstraints.Length;
            callVol = GetVolForAbsoluteStrike(_atmConstraint.CallStrike, interp);
            putVol = GetVolForAbsoluteStrike(_atmConstraint.PutStrike, interp);
            o[a] = BlackPV(_fwd, _atmConstraint.CallStrike, 0, _tExp, callVol, OptionType.C)
                + BlackPV(_fwd, _atmConstraint.PutStrike, 0, _tExp, putVol, OptionType.P);
            o[a] -= (_atmConstraint.CallFV + _atmConstraint.PutFV);
            a++;

            for (var i = 0; i < _smileConstraints.Length; i++)
            {
                callVol = GetVolForAbsoluteStrike(_smileConstraints[i].BFCallStrike, interp);
                putVol = GetVolForAbsoluteStrike(_smileConstraints[i].BFPutStrike, interp);
                var callFV = BlackPV(_fwd, _smileConstraints[i].BFCallStrike, 0, _tExp, callVol, OptionType.C);
                var putFV = BlackPV(_fwd, _smileConstraints[i].BFPutStrike, 0, _tExp, putVol, OptionType.P);
                var onSmileBFFV = callFV + putFV;
                var constraintBFFV = _smileConstraints[i].BFCallFV + _smileConstraints[i].BFPutFV;
                o[i+a] = onSmileBFFV - constraintBFFV;
            }

            return o;
        }

        private void UpdateInterpolator()
        {
            _interp = InterpolatorFactory.GetInterpolator(_strikes, _currentGuess, _interpType);
        }

        private static double BlackPV(double forward, double strike, double riskFreeRate, double expTime, double volatility, OptionType CP)
        {
            var cpf = (CP == OptionType.Put) ? -1.0 : 1.0;

            var d1 = (Log(forward / strike) + (expTime / 2 * (Pow(volatility, 2)))) / (volatility * Sqrt(expTime));
            var d2 = d1 - volatility * Sqrt(expTime);

            var num2 = (Log(forward / strike) + ((expTime / 2.0) * Pow(volatility, 2.0))) / (volatility * Sqrt(expTime));
            var num3 = num2 - (volatility * Sqrt(expTime));
            return (Exp(-riskFreeRate * expTime) * (((cpf * forward) * Statistics.NormSDist(num2 * cpf)) - ((cpf * strike) * Statistics.NormSDist(num3 * cpf))));
        }

        private static double AbsoluteStrikefromDeltaKAnalytic(double forward, double delta, double riskFreeRate, double expTime, double volatility)
        {
            double psi = Sign(delta);
            var sqrtT = Sqrt(expTime);
            var q = Statistics.NormInv(psi * delta);
            return forward * Exp(-psi * volatility * sqrtT * q + 0.5 * Pow(volatility, 2) * expTime);
        }

        private double GetVolForAbsoluteStrike(double strike, IInterpolator1D interp)
        {
            var cp = strike < 0 ? OptionType.Put : OptionType.Call;
            Func<double, double> testFunc = (deltaK =>
            {
                var vol = interp.Interpolate(-deltaK);
                var absK = AbsoluteStrikefromDeltaKAnalytic(_fwd, deltaK, 0, _tExp, vol);
                return absK - strike;
            });

            var solvedStrike = -Math.Solvers.Brent.BrentsMethodSolve(testFunc, -0.999999999, -0.000000001, 1e-8);
            return interp.Interpolate(solvedStrike);
        }
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

