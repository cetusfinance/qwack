using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Qwack.Transport.BasicTypes;
using static System.Math;

namespace Qwack.Math.Interpolation
{
    public class LinearHazzardInterpolator : IInterpolator1D
    {
        private readonly double[] _hs;
        private readonly double[] _xs;
        private readonly IInterpolator1D _linearInterp;
        public Interpolator1DType Type => Interpolator1DType.LinearHazzard;

        public double MinX => _xs.Min();
        public double MaxX => _xs.Max();

        public LinearHazzardInterpolator() {}

        public LinearHazzardInterpolator(double[] xs, double[] hs)
        {
            _xs = xs;
            _hs = hs;
            _linearInterp = InterpolatorFactory.GetInterpolator(xs, hs, Interpolator1DType.LinearFlatExtrap);
        }

        public double FirstDerivative(double x) => -_linearInterp.Interpolate(x) * Interpolate(x);
        public double Interpolate(double t) => Exp(-_linearInterp.Interpolate(t) * t);
        public double SecondDerivative(double x) => -_linearInterp.Interpolate(x) * FirstDerivative(x);

        public double[] Sensitivity(double x) => throw new NotImplementedException();
        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false) => throw new NotImplementedException();
        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false) => throw new NotImplementedException();
    }
}
