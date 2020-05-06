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
    public class LinearInVarianceInterpolator : IInterpolator1D
    {
        public Interpolator1DType Type => Interpolator1DType.LinearInVariance;

        private double[] _x;
        private double[] _y;
        private double[] _rawY;
        private double[] _slope;
        private double _minX;
        private double _maxX;

        public double[] Xs => _x;
        public double[] Ys => _y;

        public LinearInVarianceInterpolator(double[] x, double[] y)
        {
            _x = x;
            _rawY = y;
            _y = x.Select((v, ix) => v * y[ix] * y[ix]).ToArray();
            _minX = _x[0];
            _maxX = _x[x.Length - 1];
            CalculateSlope();
        }

        public LinearInVarianceInterpolator()
        { }

        private LinearInVarianceInterpolator(double[] x, double[] y, double[] slope)
        {
            _x = x;
            _rawY = y;
            _y = x.Select((v, ix) => v * y[ix] * y[ix]).ToArray();
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
            var newY = _rawY[pillar] + delta;
            return UpdateY(pillar, newY, updateInPlace);
        }

        public double FirstDerivative(double t)
        {
            if (t <= _minX || t >= _maxX)
            {
                return 0;
            }
            else
            {
                var k = FindFloorPoint(t);
                return 0.5 / Interpolate(t) * (_x[k] * _slope[k] - _y[k]) / (t * t);
            }
        }

        public double Interpolate(double t)
        {
            if(t==0)
            {
                return 0.0;
            }
            else if (t <= _minX)
            {
                return Sqrt(_y[0] / _x[0]); //extrapolate flat in vol
            }
            else if (t >= _maxX)
            {
                return Sqrt(_y[_y.Length - 1] / _x[_y.Length - 1]);
            }
            else
            {
                var k = FindFloorPoint(t);
                var variance = _y[k] + (t - _x[k]) * _slope[k];
                var vol = Sqrt(variance / t);
                return vol;
            }
        }

        public double SecondDerivative(double x)
        {
            if (x <= _minX || x >= _maxX)
            {
                return 0;
            }
            else
            {
                var k = FindFloorPoint(x);
                var y = Interpolate(x);
                var f = 0.5 / y;
                var u = y * y;
                var g = (_x[k] * _slope[k] - _y[k]) / (x * x);
                var df = -Pow(u, -1.5) / 4.0 * g;
                var dg = -2.0 * g / x;

                var d2ydx = df * g + dg * f;
                return d2ydx;
            }
        }

        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false)
        {
            if (updateInPlace)
            {
                _rawY[pillar] = newValue;
                _y[pillar] = newValue * newValue * _x[pillar];
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
                var newY = new double[_rawY.Length];
                Buffer.BlockCopy(_rawY, 0, newY, 0, _rawY.Length * 8);
                var newSlope = new double[_slope.Length];
                Buffer.BlockCopy(_slope, 0, newSlope, 0, _slope.Length * 8);
                var returnValue = new LinearInVarianceInterpolator(_x, newY, newSlope).Bump(pillar, newValue - _rawY[pillar], true);
                return returnValue;
            }
        }

        public double[] Sensitivity(double t) => throw new NotImplementedException();
    }
}
