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
    public class LinearInterpolatorFlatExtrapNoBinSearch : IInterpolator1D, IIntegrableInterpolator
    {
        public Interpolator1DType Type => Interpolator1DType.LinearFlatExtrapNoBinSearch;

        const double xBump = 1e-10;

        private double[] _x;
        private double[] _y;
        private double[] _slope;
        private readonly double _minX;
        private readonly double _maxX;
        private readonly double _minY;
        private readonly double _maxY;

        public double[] Xs => _x;
        public double[] Ys => _y;

        public LinearInterpolatorFlatExtrapNoBinSearch(double[] x, double[] y)
        {
            _x = x;
            _y = y;
            _minX = _x[0];
            _maxX = _x[x.Length - 1];
            _minY = _y[0];
            _maxY = _y[y.Length - 1];
            CalculateSlope();
        }
                
        public LinearInterpolatorFlatExtrapNoBinSearch()
        { }

        private LinearInterpolatorFlatExtrapNoBinSearch(double[] x, double[] y, double[] slope)
        {
            _x = x;
            _y = y;
            _slope = slope;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindFloorPoint(double t)
        {
            var x = _x;
            for(var i = 1; i < x.Length;i++)
            {
                if(x[i] >= t)
                {
                    return i-1;
                }
            }
            throw new NotImplementedException();
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

        public double FirstDerivative(double x)
        {
            var x1 = Interpolate(x);
            var x2 = Interpolate(x + xBump);
            var d1 = (x2 - x1) / xBump;
            return d1;
        }

        public double Interpolate(double t)
        {
            if (t <= _minX)
            {
                return _y[0];
            }
            else if (t >= _maxX)
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
            var x1 = FirstDerivative(x);
            var x2 = FirstDerivative(x + xBump);
            var d2 = (x2 - x1) / xBump;
            return d2;
        }

        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false)
        {
            if (updateInPlace)
            {
                var y = _y;
                var x = _x;
                y[pillar] = newValue;
                if (pillar < _slope.Length)
                {
                    _slope[pillar] = (y[pillar + 1] - y[pillar]) / (x[pillar + 1] - x[pillar]);
                }
                if (pillar != 0)
                {
                    pillar -= 1;
                    _slope[pillar] = (y[pillar + 1] - y[pillar]) / (x[pillar + 1] - x[pillar]);
                }
                return this;
            }
            else
            {
                var newY = new double[_y.Length];
                Buffer.BlockCopy(_y, 0, newY, 0, _y.Length * 8);
                var newSlope = new double[_slope.Length];
                Buffer.BlockCopy(_slope, 0, newSlope, 0, _slope.Length * 8);
                var returnValue = new LinearInterpolatorFlatExtrapNoBinSearch(_x, newY, newSlope).Bump(pillar, newValue-_y[pillar], true);
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

        public double DefiniteIntegral(double a, double b)
        {
            if (b < a) throw new Exception("b must be strictly greater than a");
            if (b == a) return 0;

            var iSum = 0.0;

            if (a < _minX) //flat extrap on the left
            {
                iSum += (_minX - a) * _minY;
                a = _minX;
            }
            if (b > _maxX) //flat extrap on the right
            {
                iSum += (b - _maxX) * _maxY;
                b = _maxX;
            }

            var ka = FindFloorPoint(a);
            var kb = FindFloorPoint(b);

            double vA, vU;
            while (kb > ka)
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
    }
}
