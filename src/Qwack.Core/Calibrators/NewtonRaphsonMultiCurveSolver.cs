using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            List<Tuple<int, int>> pillarToCurveMap = new List<Tuple<int, int>>();
            for (int i = 0; i < _numberOfCurves; i++)
            {
                var currentCurve = _curveEngine.Curves[_curveNames[i]];
                int nPillarsOnCurve = currentCurve.NumberOfPillars;
                pillarToCurveMap.AddRange(Enumerable.Range(0, nPillarsOnCurve).Select(x => new Tuple<int, int>(i, x)));
            }
            PillarToCurveMapping = pillarToCurveMap.ToArray();


            ComputeJacobian();

            for (int i = 0; i < MaxItterations; i++)
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

        void ComputeNextGuess()
        {
            // f = f - d/f'




            //for (int i = 0; i < nPillars; i++)
            //{

            //    int pillarIx = i - pillarsSoFar;

            //var JacobianM = MathNet.Numerics.LinearAlgebra.Double.Matrix.Build.DenseOfArray(Jacobian);
            var JacobianMI = Qwack.Math.Matrix.DoubleArrayFunctions.InvertMatrix(_jacobian);
            //JacobianMI = Math.Matrix.MSMathMatrix.MatrixTranspose(JacobianMI);
            //var CurrentPVV = MathNet.Numerics.LinearAlgebra.Double.Vector.Build.Dense(CurrentPVs);
            //var deltaGuess = Math.Matrix.MSMathMatrix.MatrixProduct(JacobianMI, _currentPVs);
            var deltaGuess = Qwack.Math.Matrix.DoubleArrayFunctions.MatrixProduct(_currentPVs, JacobianMI);
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
            _jacobian = Qwack.Math.Matrix.DoubleArrayFunctions.MatrixCreate(_numberOfPillars, _numberOfPillars);

            for (int i = 0; i < _numberOfPillars; i++)
            {
                int curveIx = PillarToCurveMapping[i].Item1;
                int curvePillarIx = PillarToCurveMapping[i].Item2;

                var currentCurve = _curveEngine.Curves[_curveNames[curveIx]];

                _currentGuess[i] = currentCurve.GetRate(curvePillarIx);

                currentCurve.BumpRate(curvePillarIx, _jacobianBump, true);
                double[] bumpedPVs = ComputePVs();
                currentCurve.BumpRate(curvePillarIx, -_jacobianBump, true);

                for (int j = 0; j < bumpedPVs.Length; j++)
                {
                    _jacobian[i][j] = (bumpedPVs[j] - _currentPVs[j]) / _jacobianBump;
                }
            }
        }

        private double[] ComputePVs()
        {
            var O = new double[_numberOfInstruments];
            for (int i = 0; i < O.Length; i++)
            {
                O[i] = _fundingInstruments[i].Pv(_curveEngine, true);
            }

            return O;
        }
    }
}

