using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Transport.BasicTypes;

namespace Qwack.Math.Interpolation
{
    public class Generic2dInterpolator : IInterpolator2D
    {
        private readonly double[] _x;
        private readonly double[] _y;
        private readonly double[,] _z;
        private readonly Interpolator1DType _xType;
        private readonly Interpolator1DType _yType;
        private IInterpolator1D[] _yInterps;

        public Generic2dInterpolator()
        {

        }

        public Generic2dInterpolator(double[] x, double[] y, double[,] z, Interpolator1DType xType, Interpolator1DType yType)
        {
            _x = x;
            _y = y;
            _z = z;
            _xType = xType;
            _yType = yType;

            _yInterps = new IInterpolator1D[_y.Length];
            for (var i = 0; i < _yInterps.Length; i++)
            {
                _yInterps[i] = InterpolatorFactory.GetInterpolator(_x, _z.GetRow(i), _yType);
            }
        }

        public Generic2dInterpolator(double[][] x, double[] y, double[][] z, Interpolator1DType xType, Interpolator1DType yType)
        {
            _y = y;
            _xType = xType;
            _yType = yType;

            _yInterps = new IInterpolator1D[_y.Length];
            for (var i = 0; i < _yInterps.Length; i++)
            {
                _yInterps[i] = InterpolatorFactory.GetInterpolator(x[i], z[i], _yType);
            }
        }

        public double Interpolate(double x, double y)
        {
            var xVals = _yInterps.Select(t => t.Interpolate(x));
            var finalInterp = InterpolatorFactory.GetInterpolator(_y, xVals.ToArray(), _xType);
            return finalInterp.Interpolate(y);
        }
    }
}
