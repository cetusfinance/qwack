using System;
using Qwack.Transport.BasicTypes;
using static System.Math;

namespace Qwack.Math.Interpolation
{
    public class CubicHermiteSplineInterpolator : IIntegrableInterpolator
    {
        public Interpolator1DType Type => Interpolator1DType.CubicSpline;

        private const double xBump = 1e-10;

        public double MinX => _minX;
        public double MaxX => _maxX;

        private readonly double[] _x;
        private readonly double[] _y;
        public double[] Xs => _x;
        public double[] Ys => _y;

        private readonly double[] _tangents;

        private readonly double _minX;
        private readonly double _maxX;
        private readonly bool _monotone = false;

        public CubicHermiteSplineInterpolator()
        {

        }

        public CubicHermiteSplineInterpolator(double[] x, double[] y, bool monotone = false)
        {
            _x = x;
            _y = y;
            _minX = _x[0];
            _maxX = _x[x.Length - 1];
            _tangents = new double[_x.Length];
            _monotone = monotone;

            Setup();
        }

        private CubicHermiteSplineInterpolator(double[] x, double[] y, double[] tangents, bool monotone=false)
        {
            _x = x;
            _y = y;

            _tangents = tangents;
            _monotone = monotone;

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
            if (_x.Length == 1)
                return;

            var dk = new double[_tangents.Length-1];
            _tangents[0] = (_y[1] - _y[0]) / (_x[1] - _x[0]);
            dk[0] = _tangents[0];
            for(var i=1;i<_tangents.Length-1;i++)
            {
                dk[i] = (_y[i+1] - _y[i]) / (_x[i+1] - _x[i]);
                _tangents[i] = 0.5 * (dk[i] + dk[i-1]);
            }
            var l = _tangents.Length - 1;
            _tangents[l] = (_y[l] - _y[l-1]) / (_x[l] - _x[l-1]);

            if(_monotone)
            {
                for (var i = 0; i < dk.Length; i++)
                {
                    if (dk[i] == 0)
                    {
                        _tangents[i] = 0;
                        _tangents[i + 1] = 0;
                    }
                    else
                    {
                        var a = _tangents[i] / dk[i];
                        var b = _tangents[i+1] / dk[i];
                        if (a > 3)
                            _tangents[i] = dk[i] * 3.0;
                        if (b > 3)
                            _tangents[i+1] = dk[i] * 3.0;
                    }
                }
            }
        }

        private double H00(double t) => 2 * t * t * t - 3 * t * t + 1.0;
        private double H10(double t) => t * t * t - 2 * t * t + t;
        private double H01(double t) => -2 * t * t * t + 3 * t * t;
        private double H11(double t) => t * t * t - t * t;

        private double H00d(double t) => 6 * t * t - 6 * t;
        private double H10d(double t) => 3 * t * t - 4 * t + 1.0;
        private double H01d(double t) => -6 * t * t + 6 * t;
        private double H11d(double t) => 3 * t * t - 2 * t;

        private double H00dd(double t) => 12 * t - 6;
        private double H10dd(double t) => 6 * t - 4;
        private double H01dd(double t) => -12 * t + 6;
        private double H11dd(double t) => 6 * t - 2;

        private double H00i(double t) => 0.5 * t * t * t * t - t * t * t + t;
        private double H10i(double t) => 0.25 * t * t * t * t - 2.0/3.0 * t * t * t + 0.5 * t * t;
        private double H01i(double t) => -0.5 * t * t * t * t + t * t * t;
        private double H11i(double t) => 0.25 * t * t * t * t - 1.0/3.0 * t * t * t;

        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false)
        {
            var newY = _y[pillar] + delta;
            return UpdateY(pillar, newY, updateInPlace);
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

        public double SecondDerivative(double x)
        {
            if (_x.Length == 1 || x < _minX || x > _maxX) //linear extrapolation
            {
                return 0;
            }
            else if (x == _minX || x==_maxX) //numerical approx at linear bounds
            {
                var d = 0.00000001;
                return (FirstDerivative(x + d / 2) - FirstDerivative(x - d / 2)) / d;
            }
            else
            {
                var k = FindFloorPoint(x);

                var interval = (_x[k + 1] - _x[k]);
                var t = (x - _x[k]) / interval;
                var d2Vdt2 = H00dd(t) * _y[k]
                    + H10dd(t) * interval * _tangents[k]
                    + H01dd(t) * _y[k + 1]
                    + H11dd(t) * interval * _tangents[k + 1];;
                var d2Vdx2 = (1 / interval) * (1 / interval);
                return d2Vdt2 * d2Vdx2;
            }
        }

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

        public double DefiniteIntegral(double a, double b)
        {
            if (_x.Length == 1)
            {
                return _y[0] * (b - a);
            }

            var integral = 0.0;
            var x = a;
            while (x < b)
            {
                if (x < _minX) //linear extrapolation
                {
                    var y1 = Interpolate(_minX);
                    var y0 = Interpolate(x);
                    integral += (y1 - (y1 - y0) / 2.0) * (_minX - x);
                    x = _minX;
                }
                else if (x >= _maxX) //linear extrapolation
                {
                    var y1 = Interpolate(b);
                    var y0 = Interpolate(_maxX);
                    integral += (y1 - (y1 - y0) / 2.0) * (b - _maxX);
                    x = b;
                }
                else
                {
                    var k = FindFloorPoint(x);

                    var fullSegmentR = (k < _x.Length - 2 && _x[k + 1] < b) || (k == _x.Length - 1 && _maxX < b);
                    var fullSegmentL = x <= _x[k];
                    var fullSegment = fullSegmentL && fullSegmentR;
                    var interval = (_x[k + 1] - _x[k]);

                    if (fullSegment)
                    {
                        
                        
                        var i1 = H00i(1.0) * _y[k]
                            + H10i(1.0) * interval * _tangents[k]
                            + H01i(1.0) * _y[k + 1]
                            + H11i(1.0) * interval * _tangents[k + 1];
                        var i0 = H00i(0.0) * _y[k]
                            + H10i(0.0) * interval * _tangents[k]
                            + H01i(0.0) * _y[k + 1]
                            + H11i(0.0) * interval * _tangents[k + 1];
                        integral += (i1 - i0) * interval;
                        x = k < _x.Length - 2 ? _x[k + 1] : _maxX;
                    }
                    else if (fullSegmentR)
                    {
                        var t = (x - _x[k]) / interval;
                        var i1 = H00i(1.0) * _y[k]
                            + H10i(1.0) * interval * _tangents[k]
                            + H01i(1.0) * _y[k + 1]
                            + H11i(1.0) * interval * _tangents[k + 1];
                        var i0 = H00i(t) * _y[k]
                            + H10i(t) * interval * _tangents[k]
                            + H01i(t) * _y[k + 1]
                            + H11i(t) * interval * _tangents[k + 1];
                        integral += (i1 - i0) * interval;
                        x = k < _x.Length - 2 ? _x[k + 1] : _maxX;
                    }
                    else
                    {
                        var t1 = (b - _x[k]) / interval;
                        var t0 = (x - _x[k]) / interval;
                        var i1 = H00i(t1) * _y[k]
                            + H10i(t1) * interval * _tangents[k]
                            + H01i(t1) * _y[k + 1]
                            + H11i(t1) * interval * _tangents[k + 1];
                        var i0 = H00i(t0) * _y[k]
                            + H10i(t0) * interval * _tangents[k]
                            + H01i(t0) * _y[k + 1]
                            + H11i(t0) * interval * _tangents[k + 1];
                        integral += (i1 - i0) * interval;
                        x = b;
                    }

                   
                }
            }
            return integral;
        }
    }
}
