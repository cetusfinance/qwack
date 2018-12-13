using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static System.Math;
using Qwack.Math;

namespace Qwack.Math.Interpolation
{
    public class GaussianKernelInterpolator : IInterpolator1D
    {
        const double xBump = 1e-10;

        private readonly double[] _x;
        private readonly double[] _y;
        private double[] _weights;
        private readonly double _minX;
        private readonly double _maxX;
        private readonly double _bandwidth = 0.25;

        public GaussianKernelInterpolator()
        {

        }

        public GaussianKernelInterpolator(double[] x, double[] y)
        {
            _x = x;
            _y = y;
            _minX = _x[0];
            _maxX = _x[x.Length - 1];
            FitWeights();
        }

        private GaussianKernelInterpolator(double[] x, double[] y, double[] weights)
        {
            _x = x;
            _y = y;
            _weights = weights;
        }

      
        private void FitWeights()
        {
            Func<double[], double[]> errFunc = (weights =>
             {
                 return _x.Select((x, ix) => GKernInterpolate(x, _x, weights, _bandwidth) - _y[ix]).ToArray();
             });
            var n2Sol = new Solvers.NewtonRaphsonMultiDimensionalSolver
            {
                InitialGuess = Enumerable.Repeat(1.0 / _x.Length, _x.Length).ToArray(),
                ObjectiveFunction = errFunc
            };
            _weights = n2Sol.Solve();
        }

        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false) => throw new NotImplementedException();

        public double FirstDerivative(double t) => throw new NotImplementedException();

        public double Interpolate(double t) => GKernInterpolate(t, _x, _weights, _bandwidth);

        private static double GKernInterpolate(double X, double[] Xmeans, double[] weights, double Bandwidth)
        {
            var output = 0.0;
            var Q = 0.0;
            
            for (var i = 0; i < Xmeans.Length; i++)
            {
                var K = Distributions.Gaussian.GKern(X, Xmeans[i], Bandwidth);
                Q += K;
                output += weights[i] * K;
            }

            return output / Q;
        }

        public double SecondDerivative(double x) => throw new NotImplementedException();

        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false) => throw new NotImplementedException();

        public double[] Sensitivity(double t) => throw new NotImplementedException();
    }
}
