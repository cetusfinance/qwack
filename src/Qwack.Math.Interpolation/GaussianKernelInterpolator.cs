using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static System.Math;
using Qwack.Math;
using Qwack.Transport.BasicTypes;

namespace Qwack.Math.Interpolation
{
    public class GaussianKernelInterpolator : IInterpolator1D
    {
        public Interpolator1DType Type => Interpolator1DType.GaussianKernel;

        private readonly double[] _x;
        private readonly double[] _y;
        private double[] _weights;
        private readonly double _minX;
        private readonly double _maxX;
        private readonly double _bandwidth = 0.25;

        public double[] Xs => _x;
        public double[] Ys => _y;

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

        public double FirstDerivative(double t)
        {
            var du = 0.0;
            var u = 0.0;
            var dv = 0.0;
            var v = 0.0;

            for (var i = 0; i < _x.Length; i++)
            {
                var K = Distributions.Gaussian.GKern(t, _x[i], _bandwidth);
                var Kd = Distributions.Gaussian.GKernDeriv(t, _x[i], _bandwidth);
                v += K;
                u += _weights[i] * K;
                dv += Kd;
                du += _weights[i] * Kd;
            }

            return (du * v - dv * u) / (v * v);
        }

        public double SecondDerivative(double t)
        {
            var d2u = 0.0;
            var du = 0.0;
            var u = 0.0;
            var d2v = 0.0;
            var dv = 0.0;
            var v = 0.0;

            for (var i = 0; i < _x.Length; i++)
            {
                var K = Distributions.Gaussian.GKern(t, _x[i], _bandwidth);
                var Kd = Distributions.Gaussian.GKernDeriv(t, _x[i], _bandwidth);
                var K2d = Distributions.Gaussian.GKernDeriv2(t, _x[i], _bandwidth);
                v += K;
                u += _weights[i] * K;
                dv += Kd;
                du += _weights[i] * Kd;
                d2v += K2d;
                d2u += _weights[i] * K2d;
            }

            return (v * v * (d2u * v - d2v * u - 2 * dv * du) + 2 * u * v * dv * dv) / (v * v * v * v);
        }

        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false) => throw new NotImplementedException();

        public double[] Sensitivity(double t) => throw new NotImplementedException();
    }
}
