using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Qwack.Transport.BasicTypes;
using static System.Math;

namespace Qwack.Math.Interpolation
{
    public class NextInterpolator:IInterpolator1D
    {
        public Interpolator1DType Type => Interpolator1DType.NextValue;

        private double[] _x;
        private double[] _y;
        private double _minX;
        private double _maxX;

        public double[] Xs => _x;
        public double[] Ys => _y;

        public NextInterpolator()
        {

        }

        public NextInterpolator(double[] x, double[] y)
        {
            _x = x;
            _y = y;
            _minX = _x[0];
            _maxX = _x[x.Length - 1];
        }

        private int FindCeilingPoint(double t)
        {
            var index = Array.BinarySearch(_x, t);
            if (index < 0)
            {
                index = ~index;
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
            var k = FindCeilingPoint(t);
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
                var returnValue = new NextInterpolator(_x, newY).Bump(pillar, newValue - _y[pillar], true);
                return returnValue;
            }
        }

        public double[] Sensitivity(double t) => throw new NotImplementedException();
    }
}
