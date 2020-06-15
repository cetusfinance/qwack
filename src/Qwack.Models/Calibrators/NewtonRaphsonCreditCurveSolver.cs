using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Credit;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;

namespace Qwack.Models.Calibrators
{
    public class NewtonRaphsonCreditCurveSolver
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        public Interpolator1DType InterpolatorType { get; set; } = Interpolator1DType.LinearFlatExtrap;

        private const double JacobianBump = 0.0001;

        private DateTime _buildDate;
        private List<CDS> _curveInstruments;
        private DateTime[] _pillars;
        private double[] _pillarsT;
        private IIrCurve _discountCurve;
        private IInterpolator1D _currentCurve;
        private DayCountBasis _basis;
        private double _recoveryRate;

        private int _numberOfInstruments;
        private int _numberOfPillars;
        private double[] _currentGuess;
        private double[] _currentPVs;
        private double[][] _jacobian;

        public HazzardCurve Solve(List<CDS> instruments, double recoveryRate, IIrCurve discountCurve, DateTime buildDate, DayCountBasis basis = DayCountBasis.ACT365F)
        {
            _curveInstruments = instruments;
            _pillars = instruments.OrderBy(x => x.FinalSensitivityDate).Select(c => c.FinalSensitivityDate).ToArray();
            _pillarsT = _pillars.Select(p => buildDate.CalculateYearFraction(p, basis)).ToArray();
            _numberOfInstruments = _curveInstruments.Count;
            _numberOfPillars = _pillars.Length;
            _discountCurve = discountCurve;
            _buildDate = buildDate;
            _basis = basis;
            _recoveryRate = recoveryRate;

            _currentGuess = instruments.OrderBy(x => x.FinalSensitivityDate).Select((x,ix) => x.Spread / (1.0 - recoveryRate)).ToArray();
            _currentCurve = new LinearHazzardInterpolator(_pillarsT, _currentGuess);
            _currentPVs = ComputePVs();

            ComputeJacobian();

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                _currentCurve = new LinearHazzardInterpolator(_pillarsT, _currentGuess);

                _currentPVs = ComputePVs();
                if (_currentPVs.Max(x => System.Math.Abs(x)) < Tollerance)
                {
                    UsedItterations = i + 1;
                    break;
                }
                ComputeJacobian();
            }

            return new HazzardCurve(_buildDate, _basis, _currentCurve);
        }

        private void ComputeNextGuess()
        {
            var jacobianMi = Math.Matrix.DoubleArrayFunctions.InvertMatrix(_jacobian);
            var deltaGuess = Math.Matrix.DoubleArrayFunctions.MatrixProduct(_currentPVs, jacobianMi);
            for (var j = 0; j < _numberOfInstruments; j++)
            {
                _currentGuess[j] -= deltaGuess[j];
            }
        }

        private void ComputeJacobian()
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_numberOfPillars, _numberOfPillars);

            for (var i = 0; i < _numberOfPillars; i++)
            {
                
                _currentCurve = new LinearHazzardInterpolator(_pillarsT, _currentGuess.Select((g, ix) => ix == i ? g + JacobianBump : g).ToArray());
                var bumpedPVs = ComputePVs();

                for (var j = 0; j < bumpedPVs.Length; j++)
                {
                    _jacobian[i][j] = (bumpedPVs[j] - _currentPVs[j]) / JacobianBump;
                }
            }

            _currentCurve = new LinearHazzardInterpolator(_pillarsT, _currentGuess);
        }

        private double[] ComputePVs()
        {
            var o = new double[_numberOfInstruments];
            var hzCurve = new HazzardCurve(_buildDate, _basis, _currentCurve);
            for (var i = 0; i < o.Length; i++)
            {
                o[i] = _curveInstruments[i].PV_PiecewiseFlat(hzCurve, _discountCurve, _recoveryRate, false);
            }
            return o;
        }

      
    }
}

