using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static System.Math;

namespace Qwack.Math.Interpolation
{
    public class DummyPointInterpolator:IInterpolator1D
    {
        private readonly double _point;
     
        public DummyPointInterpolator()
        {

        }

        public DummyPointInterpolator(double point)
        {
            _point = point;
        }

        public DummyPointInterpolator(double[] x, double[] y)
        {
            _point = y.First();
        }

        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false)
        {
            return new DummyPointInterpolator(_point + delta);
        }

        public double FirstDerivative(double t)
        {
            return 0;
        }

        public double Interpolate(double t)
        {
            return _point;
        }

        public double SecondDerivative(double x)
        {
            return 0;
        }

        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false)
        {
            return new DummyPointInterpolator(newValue);
        }

        public double[] Sensitivity(double t)
        {
            return new double[] { 1.0 };
        }
    }
}
