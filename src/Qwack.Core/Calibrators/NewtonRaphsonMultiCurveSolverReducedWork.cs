using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;

namespace Qwack.Core.Calibrators
{
    public class NewtonRaphsonMultiCurveSolverReducedWork
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double _jacobianBump = 0.0001;

        private FundingModel _curveEngine;
        private List<IFundingInstrument> _fundingInstruments;
        private int _numberOfInstruments;
        private int _numberOfPillars;
        private int _numberOfCurves;
        private double[] _currentGuess;
        private double[] _currentPVs;
        private double[][] _jacobian;
        private string[] _curveNames;

        private Tuple<int, int>[] PillarToCurveMapping;

        private List<IrCurve> _curvesForStage;
        private List<string> _curvesNamesForStage;

        private List<IFundingInstrument> _instrumentsForStage;
        private int _stage;
        private int _maxStage;

        public void Solve(FundingModel fundingModel, FundingInstrumentCollection instruments)
        {
            _curveEngine = fundingModel;
            _maxStage = fundingModel.Curves.Max(x => x.Value.SolveStage);

            for (_stage = 0; _stage <= _maxStage; _stage++)
            {
                _curvesNamesForStage = fundingModel.Curves.Where(x => x.Value.SolveStage == _stage).Select(x => x.Key).ToList();
                _curvesForStage = fundingModel.Curves.Where(x => x.Value.SolveStage == _stage).Select(x => x.Value).ToList();

                _fundingInstruments = instruments.Where(x => _curvesNamesForStage.Contains(x.SolveCurve)).ToList();
                _numberOfInstruments = _fundingInstruments.Count;
                _numberOfPillars = _curvesForStage.Select(kv => kv.NumberOfPillars).Sum();
                _numberOfCurves = _curvesForStage.Count;
                _currentGuess = new double[_numberOfPillars];
                _curveNames = _curvesNamesForStage.ToArray();

                if (_numberOfPillars != _numberOfInstruments)
                    throw new ArgumentException();

                List<Tuple<int, int>> pillarToCurveMap = new List<Tuple<int, int>>();
                for (int i = 0; i < _numberOfCurves; i++)
                {
                    var currentCurve = _curveEngine.Curves[_curveNames[i]];
                    int nPillarsOnCurve = currentCurve.NumberOfPillars;
                    pillarToCurveMap.AddRange(Enumerable.Range(0, nPillarsOnCurve).Select(x => new Tuple<int, int>(i, x)));
                }
                PillarToCurveMapping = pillarToCurveMap.ToArray();

                for (int i = 0; i < MaxItterations; i++)
                {
                    _currentPVs = ComputePVs(true);
                    if (_currentPVs.Max(x => System.Math.Abs(x)) < Tollerance)
                    {
                        UsedItterations = i + 1;
                        break;
                    }
                    ComputeJacobian();
                    ComputeNextGuess();
                }
            }
        }

        void ComputeNextGuess()
        {
            // f = f - d/f'
            var JacobianMI = Math.Matrix.DoubleArrayFunctions.InvertMatrix(_jacobian);
            var deltaGuess = Math.Matrix.DoubleArrayFunctions.MatrixProduct(_currentPVs, JacobianMI);
            for (int j = 0; j < _numberOfInstruments; j++)
            {
                int curveIx = PillarToCurveMapping[j].Item1;
                int curvePillarIx = PillarToCurveMapping[j].Item2;
                var currentEngine = _curveEngine.Curves[_curveNames[curveIx]];
                _currentGuess[j] -= deltaGuess[j];

                currentEngine.SetRate(curvePillarIx, _currentGuess[j], true);

            }

        }

        private void ComputeJacobian()
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_numberOfPillars, _numberOfPillars);

            for (int i = 0; i < _numberOfPillars; i++)
            {
                int curveIx = PillarToCurveMapping[i].Item1;
                int curvePillarIx = PillarToCurveMapping[i].Item2;

                var currentCurve = _curveEngine.Curves[_curveNames[curveIx]];
                _curveEngine.CurrentSolveCurve = _curveNames[curveIx];
                _currentGuess[i] = currentCurve.GetRate(curvePillarIx);

                currentCurve.BumpRate(curvePillarIx, _jacobianBump, true);
                double[] bumpedPVs = ComputePVs(false);
                currentCurve.BumpRate(curvePillarIx, -_jacobianBump, true);

                for (int j = 0; j < bumpedPVs.Length; j++)
                {
                    _jacobian[i][j] = (bumpedPVs[j] - _currentPVs[j]) / _jacobianBump;
                }
            }
            _curveEngine.CurrentSolveCurve = null;
        }

        private double[] ComputePVs(bool updateState)
        {
            var O = new double[_numberOfInstruments];
            for (int i = 0; i < O.Length; i++)
            {
                O[i] = _fundingInstruments[i].Pv(_curveEngine, updateState);
            }

            return O;
        }
    }
}
