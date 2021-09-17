using System;
using Qwack.Transport.BasicTypes;
using static System.Math;

namespace Qwack.Math.Interpolation
{
    public class PreviousInterpolator:IInterpolator1D
    {
        public Interpolator1DType Type => Interpolator1DType.PreviousValue; 

        const double xBump = 1e-10;

        private readonly double[] _x;
        private readonly double[] _y;
        private double _minX;
        private double _maxX;

        public double[] Xs => _x;
        public double[] Ys => _y;

        public double MinX => _minX;
        public double MaxX => _maxX;

        public PreviousInterpolator()
        {

        }

        public PreviousInterpolator(double[] x, double[] y)
        {
            _x = x;
            _y = y;
            _minX = _x[0];
            _maxX = _x[x.Length - 1];
        }

        private int FindFloorPoint(double t)
        {
            var index = Array.BinarySearch(_x, t);
            if (index < 0)
            {
                index = ~index - 1;
            }

            return Min(Max(index, 0), _x.Length - 1);
        }

        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false)
        {
            var newY = _y[pillar] + delta;
            return UpdateY(pillar, newY, updateInPlace);
        }

        public double FirstDerivative(double t) => 0;

        public double Interpolate(double t)
        {
            var k = FindFloorPoint(t);
            return _y[k];
        }

        public double SecondDerivative(double x) => 0;

        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false)
        {
            if (updateInPlace)
            {
                _y[pillar] = newValue;
                _minX = _x[0];
                _maxX = _x[_x.Length - 1];
                return this;
            }
            else
            {
                var newY = new double[_y.Length];
                Buffer.BlockCopy(_y, 0, newY, 0, _y.Length * 8);
                var returnValue = new PreviousInterpolator(_x, newY).Bump(pillar, newValue - _y[pillar], true);
                return returnValue;
            }
        }

        public double[] Sensitivity(double t) => throw new NotImplementedException();
    }
}
