 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Math.Interpolation
{
    public class LinearInterpolator:IInterpolator1D
    {
        

        public LinearInterpolator()
        {

        }

        public IInterpolator1D Bump(int pillar, double delta, bool updateInPlace = false)
        {
            throw new NotImplementedException();
        }

        public double FirstDerivative(double x)
        {
            throw new NotImplementedException();
        }

        public double Interpolate(double x)
        {
           return 10.0;
        }

        public double SecondDerivative(double x)
        {
            throw new NotImplementedException();
        }

        public IInterpolator1D UpdateY(int pillar, double newValue, bool updateInPlace = false)
        {
            throw new NotImplementedException();
        }
    }
}
