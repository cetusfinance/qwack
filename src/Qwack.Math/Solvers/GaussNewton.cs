using System;
using System.Collections.Generic;
using System.Linq;

namespace Qwack.Math.Solvers
{
    public class GaussNewton
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        public double JacobianBump { get; set; } = 0.000001;
        public Func<double[],double[]> ObjectiveFunction { get; set; }
        public double[] InitialGuess { get; set; }

        private double[] _currentGuess;
        private double[] _currentOutput;
        private double[][] _jacobian;
        private int _nInputs;
        private int _nConstraints;

        public double[] Solve()
        {
            _nInputs = InitialGuess.Length;
            _currentGuess = new double[_nInputs];
            Array.Copy(InitialGuess, _currentGuess, _nInputs);
            _currentOutput = ObjectiveFunction(_currentGuess);
            _nConstraints = _currentOutput.Length;
            double previousRMS = RMS;

            for (int i = 0; i < MaxItterations; i++)
            {
                if (_currentOutput.Max(x => System.Math.Abs(x)) < Tollerance || (i > 0 && previousRMS - RMS < Tollerance))
                {
                    UsedItterations = i;
                    break;
                }
                ComputeJacobian();
                ComputeNextGuess();
                previousRMS = RMS;
                _currentOutput = ObjectiveFunction(_currentGuess);

            }

            return _currentGuess;
        }

        private double RMS => _currentOutput.Select(x => x * x).Sum();

        private void ComputeNextGuess()
        {
            var jacobianTranspose = Math.Matrix.DoubleArrayFunctions.Transpose(_jacobian);
            var term1 = Math.Matrix.DoubleArrayFunctions.MatrixProduct(jacobianTranspose, _jacobian);
            var term1Inverse = Math.Matrix.DoubleArrayFunctions.InvertMatrix(term1);

            if (term1Inverse.Any(x => x.Contains(double.NaN)))
                throw new Exception("Failed to invert matrix");

            var term2 = Math.Matrix.DoubleArrayFunctions.MatrixProduct(term1Inverse, jacobianTranspose);
            var deltaGuess = Math.Matrix.DoubleArrayFunctions.MatrixProduct(term2, _currentOutput);

            double[] trialSoltion = _currentGuess.Select((x, ix) => x - deltaGuess[ix]).ToArray();
            while (ObjectiveFunction(trialSoltion).Select(x => x * x).Sum()>RMS && deltaGuess.Any(x=>x!=0))
            {
                deltaGuess = deltaGuess.Select(x => x / 2.0).ToArray();
                trialSoltion = _currentGuess.Select((x, ix) => x - deltaGuess[ix]).ToArray();
            }

            for (var j = 0; j < _nInputs; j++)
            {
                _currentGuess[j] -= deltaGuess[j];
            }
        }

        private void ComputeJacobian()
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_nConstraints, _nInputs);
            for (var i = 0; i < _nInputs; i++)
            {
                var bumpedInputs = new double[_nInputs];
                Array.Copy(_currentGuess, bumpedInputs, _nInputs);
                bumpedInputs[i] += JacobianBump;
                var bumpedOutputs = ObjectiveFunction(bumpedInputs);

                for (var j = 0; j < _nConstraints; j++)
                {
                    _jacobian[j][i] = (bumpedOutputs[j] - _currentOutput[j]) / JacobianBump;
                }
            }
        }
    }
}

