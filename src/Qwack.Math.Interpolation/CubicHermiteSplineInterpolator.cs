using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static System.Math;

namespace Qwack.Math.Interpolation
{
    public class CubicHermiteSplineInterpolator : IInterpolator1D
    {
        const double xBump = 1e-10;

        private double[] _x;
        private double[] _y;

        private double[] _tangents;

        private readonly double _minX;
        private readonly double _maxX;

        public CubicHermiteSplineInterpolator()
        {

        }

        public CubicHermiteSplineInterpolator(double[] x, double[] y)
        {
            _x = x;
            _y = y;
            _minX = _x[0];
            _maxX = _x[x.Length - 1];
            _tangents = new double[_x.Length];

            Setup();
        }

        private CubicHermiteSplineInterpolator(double[] x, double[] y, double[] tangents)
        {
            _x = x;
            _y = y;

            _tangents = tangents;

            Setup();
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

        private void Setup()
        {
            _tangents[0] = (_y[1] - _y[0]) / (_x[1] - _x[0]);
            for(var i=1;i<_tangents.Length-1;i++)
            {
                _tangents[i] = 0.5 * ((_y[i + 1] - _y[i]) / (_x[i + 1] - _x[i]) + (_y[i] - _y[i - 1]) / (_x[i] - _x[i - 1]));
            }
            var l = _tangents.Length - 1;
            _tangents[l] = (_y[l] - _y[l-1]) / (_x[l] - _x[l-1]);
        }

        private double H00(double t) => 2 * t * t * t - 3 * t * t + 1.0;
        private double H10(double t) => t * t * t - 2 * t * t + t;
        private double H01(double t) => -2 * t * t * t + 3 * t * t;
        private double H11(double t) => t * t * t - t * t;

        private double H00d(double t) => 6 * t * t - 6 * t;
        private double H10d(double t) => 3 * t * t - 4 * t + 1.0;
        private double H01d(double t) => -6 * t * t + 6 * t;
        private double H11d(double t) => 3 * t * t - 2 * t;

        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false)
        {
            var newY = _y[pillar] + delta;
            return UpdateY(pillar, newY, updateInPlace);
        }

        public double FirstDerivative(double x)
        {
            if (_x.Length == 1)
            {
                return 0;
            }
            else if (x <= _minX) //linear extrapolation
            {
                return (_y[1] - _y[0]) / (_x[1] - _x[0]);
            }
            else if (x >= _maxX) //linear extrapolation
            {
                var k = _y.Length - 1;
                return (_y[k] - _y[k - 1]) / (_x[k] - _x[k - 1]);
            }
            else
            {
                var k = FindFloorPoint(x);

                var interval = (_x[k + 1] - _x[k]);
                var t = (x - _x[k]) / interval;
                var dVdt = H00d(t) * _y[k]
                    + H10d(t) * interval * _tangents[k]
                    + H01d(t) * _y[k + 1]
                    + H11d(t) * interval * _tangents[k + 1];
                var dVdx = 1 / interval;
                return dVdt * dVdx;
            }
        }

        public double Interpolate(double x)
        {
            if (_x.Length == 1)
            {
                return _y[0];
            }
            else if (x<=_minX) //linear extrapolation
            {
                return _y[0] + (_y[1] - _y[0]) / (_x[1] - _x[0]) * (x - _x[0]);
            }
            else if (x>=_maxX) //linear extrapolation
            {
                var k = _y.Length - 1;
                return _y[k] + (_y[k] - _y[k - 1]) / (_x[k] - _x[k - 1]) * (x - _x[k]);
            }
            else
            {
                var k = FindFloorPoint(x);

                var interval = (_x[k + 1] - _x[k]);
                var t = (x - _x[k]) / interval;
                return H00(t) * _y[k]
                    + H10(t) * interval * _tangents[k]
                    + H01(t) * _y[k + 1]
                    + H11(t) * interval * _tangents[k + 1];
            }
        }


        public double SecondDerivative(double x) => 0;

        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false)
        {
            var newY = new double[_y.Length];
            Buffer.BlockCopy(_y, 0, newY, 0, _y.Length * 8);
            newY[pillar] = newValue;
            var newT = new double[_tangents.Length];
            Buffer.BlockCopy(_tangents, 0, newT, 0, _tangents.Length * 8);
             var returnValue = new CubicHermiteSplineInterpolator(_x, newY, newT);
            return returnValue;
        }

        public double[] Sensitivity(double t) => throw new NotImplementedException();
    }
}
