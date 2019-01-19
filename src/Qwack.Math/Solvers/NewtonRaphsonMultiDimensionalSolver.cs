using System;
using System.Collections.Generic;
using System.Linq;

namespace Qwack.Math.Solvers
{
    public class NewtonRaphsonMultiDimensionalSolver
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        public double JacobianBump { get; set; } = 0.0001;
        public Func<double[],double[]> ObjectiveFunction { get; set; }
        public double[] InitialGuess { get; set; }

        private double[] _currentGuess;
        private double[] _currentOutput;
        private double[][] _jacobian;
        private int _n;

        public double[] Solve()
        {
            _n = InitialGuess.Length;
            _currentGuess = new double[_n];
            Array.Copy(InitialGuess, _currentGuess, _n);
            _currentOutput = ObjectiveFunction(_currentGuess);

            if (_currentOutput.Length!= _n)
                throw new ArgumentException();
     
            for (var i = 0; i < MaxItterations; i++)
            {
                if (_currentOutput.Max(x => System.Math.Abs(x)) < Tollerance)
                {
                    UsedItterations = i;
                    break;
                }
                ComputeJacobian();
                ComputeNextGuess();
                NaNCheck();
                _currentOutput = ObjectiveFunction(_currentGuess);
            }

            return _currentGuess;
        }

        private void NaNCheck()
        {
            if (_currentGuess.Any(x => double.IsNaN(x)))
                throw new Exception("NaNs detected in solution");
        }

        private void ComputeNextGuess()
        {
            var jacobianMi = Matrix.DoubleArrayFunctions.InvertMatrix(_jacobian);
            var deltaGuess = Matrix.DoubleArrayFunctions.MatrixProduct(_currentOutput, jacobianMi);
            for (var j = 0; j < _n; j++)
            {
                _currentGuess[j] -= deltaGuess[j];
            }
        }

        private void ComputeJacobian()
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_n, _n);

            for (var i = 0; i < _n; i++)
            {
                var bumpedInputs = new double[_n];
                Array.Copy(_currentGuess, bumpedInputs, _n);
                bumpedInputs[i] += JacobianBump;
                var bumpedOutputs = ObjectiveFunction(bumpedInputs);

                for (var j = 0; j < _n; j++)
                {
                    _jacobian[i][j] = (bumpedOutputs[j] - _currentOutput[j]) / JacobianBump;
                }
            }
        }
    }
}

