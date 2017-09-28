using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;

namespace Qwack.Core.Calibrators
{
    public class NewtonRaphsonMultiCurveSolver
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double JacobianBump = 0.0001;

        private FundingModel _curveEngine;
        private List<IFundingInstrument> _fundingInstruments;
        private int _numberOfInstruments;
        private int _numberOfPillars;
        private int _numberOfCurves;
        private double[] _currentGuess;
        private double[] _currentPVs;
        private double[][] _jacobian;
        private string[] _curveNames;

        private Tuple<int, int>[] _pillarToCurveMapping;

        public void Solve(FundingModel fundingModel, FundingInstrumentCollection instruments)
        {
            _curveEngine = fundingModel;
            _fundingInstruments = instruments;
            _numberOfInstruments = _fundingInstruments.Count;
            _numberOfPillars = _curveEngine.Curves.Select(kv => kv.Value.NumberOfPillars).Sum();
            _numberOfCurves = _curveEngine.Curves.Count;
            _currentGuess = new double[_numberOfPillars];
            _currentPVs = ComputePVs();
            _curveNames = fundingModel.Curves.Keys.ToArray();
            if (_numberOfPillars != _numberOfInstruments)
                throw new ArgumentException();

            var pillarToCurveMap = new List<Tuple<int, int>>();
            for (var i = 0; i < _numberOfCurves; i++)
            {
                var currentCurve = _curveEngine.Curves[_curveNames[i]];
                var nPillarsOnCurve = currentCurve.NumberOfPillars;
                var i1 = i;
                pillarToCurveMap.AddRange(Enumerable.Range(0, nPillarsOnCurve).Select(x => new Tuple<int, int>(i1, x)));
            }
            _pillarToCurveMapping = pillarToCurveMap.ToArray();


            ComputeJacobian();

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                _currentPVs = ComputePVs();
                if (_currentPVs.Max(x => System.Math.Abs(x)) < Tollerance)
                {
                    UsedItterations = i + 1;
                    break;
                }
                ComputeJacobian();
            }
        }

        private void ComputeNextGuess()
        {
            var jacobianMi = Math.Matrix.DoubleArrayFunctions.InvertMatrix(_jacobian);
            var deltaGuess = Math.Matrix.DoubleArrayFunctions.MatrixProduct(_currentPVs, jacobianMi);
            for (var j = 0; j < _numberOfInstruments; j++)
            {
                var curveIx = _pillarToCurveMapping[j].Item1;
                var curvePillarIx = _pillarToCurveMapping[j].Item2;
                var currentEngine = _curveEngine.Curves[_curveNames[curveIx]];
                _currentGuess[j] -= deltaGuess[j];

                currentEngine.SetRate(curvePillarIx, _currentGuess[j], true);
            }
        }

        private void ComputeJacobian()
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_numberOfPillars, _numberOfPillars);

            for (var i = 0; i < _numberOfPillars; i++)
            {
                var curveIx = _pillarToCurveMapping[i].Item1;
                var curvePillarIx = _pillarToCurveMapping[i].Item2;
                var currentCurve = _curveEngine.Curves[_curveNames[curveIx]];
                _currentGuess[i] = currentCurve.GetRate(curvePillarIx);

                currentCurve.BumpRate(curvePillarIx, JacobianBump, true);
                var bumpedPVs = ComputePVs();
                currentCurve.BumpRate(curvePillarIx, -JacobianBump, true);

                for (var j = 0; j < bumpedPVs.Length; j++)
                {
                    _jacobian[i][j] = (bumpedPVs[j] - _currentPVs[j]) / JacobianBump;
                }
            }
        }

        private double[] ComputePVs()
        {
            var o = new double[_numberOfInstruments];
            for (var i = 0; i < o.Length; i++)
            {
                o[i] = _fundingInstruments[i].Pv(_curveEngine, true);
            }
            return o;
        }
    }
}

