using System.Linq;
using Qwack.Transport.BasicTypes;

namespace Qwack.Math.Interpolation
{
    public class DummyPointInterpolator : IInterpolator1D, IInterpolator2D
    {
        public Interpolator1DType Type => Interpolator1DType.DummyPoint;

        private readonly double _point;

        public double Point => _point;

        public double MinX => double.MinValue;
        public double MaxX => double.MaxValue;

        public DummyPointInterpolator()
        {

        }

        public DummyPointInterpolator(double point) => _point = point;

        public DummyPointInterpolator(double[] x, double[] y) => _point = y.First();

        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false) => new DummyPointInterpolator(_point + delta);

        public double FirstDerivative(double t) => 0;

        public double Interpolate(double t) => _point;

        public double SecondDerivative(double x) => 0;

        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false) => new DummyPointInterpolator(newValue);

        public double[] Sensitivity(double t) => new double[] { 1.0 };

        public double Interpolate(double x, double y) => _point;

    }
}
