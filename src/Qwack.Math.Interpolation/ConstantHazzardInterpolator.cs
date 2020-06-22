using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Qwack.Transport.BasicTypes;
using static System.Math;

namespace Qwack.Math.Interpolation
{
    public class ConstantHazzardInterpolator : IInterpolator1D
    {
        private readonly double _h;



        public double H => _h;

        public Interpolator1DType Type => Interpolator1DType.ConstantHazzard;

        public double MinX => double.MinValue;
        public double MaxX => double.MaxValue;

        public ConstantHazzardInterpolator() {}

        public ConstantHazzardInterpolator(double hazzardRate) => _h = hazzardRate;

        public double FirstDerivative(double x) => -_h * Interpolate(x);
        public double Interpolate(double t) => Exp(-_h * t);
        public double SecondDerivative(double x) => -_h * FirstDerivative(x);

        public double[] Sensitivity(double x) => throw new NotImplementedException();
        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false) => throw new NotImplementedException();
        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false) => throw new NotImplementedException();
    }
}
