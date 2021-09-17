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
    public class LinearInterpolatorFlatExtrap : IInterpolator1D, IIntegrableInterpolator
    {
        public Interpolator1DType Type => Interpolator1DType.LinearFlatExtrap;

        private readonly double[] _x;
        private readonly double[] _y;
        private double[] _slope;
        private double _minX;
        private double _maxX;
        public double MinX => _minX;
        public double MaxX => _maxX;
        public double[] Xs => _x;
        public double[] Ys => _y;

        public LinearInterpolatorFlatExtrap(double[] x, double[] y)
        {
            _x = x;
            _y = y;
            _minX = _x[0];
            _maxX = _x[x.Length - 1];
            CalculateSlope();
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
            var index = Array.BinarySearch(_x, t);
            if (index < 0)
            {
                index = ~index - 1;
            }

            return Min(Max(index, 0), _x.Length - 2);
        }

        private void CalculateSlope()
        {
            _slope = new double[_x.Length - 1];
            for (var i = 0; i < _slope.Length; i++)
            {
                _slope[i] = (_y[i + 1] - _y[i]) / (_x[i + 1] - _x[i]);
            }
        }

        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false)
        {
            var newY = _y[pillar] + delta;
            return UpdateY(pillar, newY, updateInPlace);
        }

        public double FirstDerivative(double t)
        {
            if (t < _minX || t > _maxX)
            {
                return 0;
            }
            else
            {
                var k = FindFloorPoint(t);
                return _slope[k];
            }
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
                var k = FindFloorPoint(t);
                return _y[k] + (t - _x[k]) * _slope[k];
            }
        }

        public double SecondDerivative(double x)
        {
            if (!_x.Contains(x))
                return 0;
            var k = FindFloorPoint(x);
            if (k == 0)
                return 0.5 * _slope[0];
            if(k==_x.Length)
                return 0.5 * _slope[_slope.Length];
            
            return (_slope[k] + _slope[k - 1]) / 2.0;
        }

        public double DefiniteIntegral(double a, double b)
        {
            if (b < a) throw new Exception("b must be strictly greater than a");
            if (b == a) return 0;

            var iSum = 0.0;

            if(a<_minX) //flat extrap on the left
            {
                iSum += (_minX - a) * _minX;
                a = _minX;
            }
            if (b > _maxX) //flat extrap on the right
            {
                iSum += (b - _maxX) * _maxX;
                b = _maxX;
            }

            var ka = FindFloorPoint(a);
            var kb = FindFloorPoint(b);

            double vA, vU;
            while(kb>ka)
            {
                var u = _x[ka + 1]; //upper bound of segment
                vA = Interpolate(a);
                vU = Interpolate(u);
                iSum += 0.5 * (vU + vA) * (u - a);

                a = u;
                ka++;
            }

            vA = Interpolate(a);
            vU = Interpolate(b);
            iSum += 0.5 * (vU + vA) * (b - a);
            return iSum;
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
                var returnValue = new LinearInterpolatorFlatExtrap(_x, newY, newSlope).Bump(pillar, newValue - _y[pillar], true);
                return returnValue;
            }
        }

        public double[] Sensitivity(double t)
        {
            var o = new double[_y.Length];
            if (t <= _minX)
            {
                o[0] = 1;
            }
            else if (t >= _maxX)
            {
                o[o.Length - 1] = 1;
            }
            else
            {
                var k = FindFloorPoint(t);
                var prop = (t - _x[k]) / (_x[k + 1] - _x[k]);
                o[k + 1] = prop;
                o[k] = (1.0 - prop);
            }
            return o;
        }
    }
}
