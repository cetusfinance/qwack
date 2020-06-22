using System;
using Qwack.Transport.BasicTypes;

namespace Qwack.Math.Interpolation
{
    public class ParametricLinearInterpolator : IInterpolator1D
    {
        public Interpolator1DType Type => Interpolator1DType.Other;

        private readonly double _alpha;
        private readonly double _beta;

        public double MinX => double.MinValue;
        public double MaxX => double.MaxValue;

        public ParametricLinearInterpolator() {}

        public ParametricLinearInterpolator(double alpha, double beta)
        {
            _alpha = alpha;
            _beta = beta;
        }
    
        public double FirstDerivative(double x) => _beta;
        public double Interpolate(double t) => _alpha + t * _beta;
        public double SecondDerivative(double x) => 0.0;

        public double[] Sensitivity(double x) => throw new NotImplementedException();
        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false) => throw new NotImplementedException();
        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false) => throw new NotImplementedException();
    }
}
