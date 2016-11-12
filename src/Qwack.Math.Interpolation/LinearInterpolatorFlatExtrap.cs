using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static System.Math;

namespace Qwack.Math.Interpolation
{
    public class LinearInterpolatorFlatExtrap : IInterpolator1D
    {
        const double xBump = 1e-10;

        private double[] _x;
        private double[] _y;
        private double[] _slope;
        private double _minX;
        private double _maxX;

        public LinearInterpolatorFlatExtrap(double[] x, double[] y, bool noCopy, bool isSorted)
        {
            if (noCopy)
            {
                _x = x;
                _y = y;
            }
            else
            {
                _x = new double[x.Length];
                _y = new double[y.Length];
                Buffer.BlockCopy(x, 0, _x, 0, x.Length * 8);
                Buffer.BlockCopy(y, 0, _y, 0, y.Length * 8);
            }
            if (!isSorted)
            {
                Array.Sort(_x, _y);
            }
            _minX = _x[0];
            _maxX = _x[x.Length - 1];
            CalculateSlope();
        }

        public LinearInterpolatorFlatExtrap(double[] x, double[] y)
            : this(x, y, false, false)
        {
        }

        public LinearInterpolatorFlatExtrap()
        { }

        private LinearInterpolatorFlatExtrap(double[] x, double[] y, double[] slope)
        {
            _x = x;
            _y = y;
            _slope = slope;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindFloorPoint(double t)
        {
            int index = Array.BinarySearch(_x, t);
            if (index < 0)
            {
                index = ~index - 1;
            }

            return Min(Max(index, 0), _x.Length - 2);
        }

        private void CalculateSlope()
        {
            _slope = new double[_x.Length - 1];
            for (int i = 0; i < _slope.Length; i++)
            {
                _slope[i] = (_y[i + 1] - _y[i]) / (_x[i + 1] - _x[i]);
            }
        }

        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false)
        {
            var newY = _y[pillar] + delta;
            return UpdateY(pillar, newY, updateInPlace);
        }

        public double FirstDerivative(double x)
        {
            double x1 = Interpolate(x);
            double x2 = Interpolate(x + xBump);
            double d1 = (x2 - x1) / xBump;
            return d1;
        }

        public double Interpolate(double t)
        {
            if (t < _minX)
            {
                return _y[0];
            }
            else if (t > _maxX)
            {
                return _y[_y.Length - 1];
            }
            else
            {
                int k = FindFloorPoint(t);
                return _y[k] + (t - _x[k]) * _slope[k];
            }
        }

        public double SecondDerivative(double x)
        {
            double x1 = FirstDerivative(x);
            double x2 = FirstDerivative(x + xBump);
            double d2 = (x2 - x1) / xBump;
            return d2;
        }

        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false)
        {
            if (updateInPlace)
            {
                _y[pillar] = newValue;
                if (pillar < _slope.Length)
                {
                    _slope[pillar] = (_y[pillar + 1] - _y[pillar]) / (_x[pillar + 1] - _x[pillar]);
                }
                if (pillar > 0)
                {
                    pillar -= 1;
                    _slope[pillar] = (_y[pillar + 1] - _y[pillar]) / (_x[pillar + 1] - _x[pillar]);
                }
                _minX = _x[0];
                _maxX = _x[_x.Length - 1];
                return this;
            }
            else
            {
                var newY = new double[_y.Length];
                Buffer.BlockCopy(_y, 0, newY, 0, _y.Length * 8);
                var newSlope = new double[_slope.Length];
                Buffer.BlockCopy(_slope, 0, newSlope, 0, _slope.Length * 8);
                var returnValue = new LinearInterpolatorFlatExtrap(_x, newY, newSlope).Bump(pillar, newValue, true);
                return returnValue;
            }
        }
    }
}
