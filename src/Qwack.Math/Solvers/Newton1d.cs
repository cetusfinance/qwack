using System;

namespace Qwack.Math.Solvers
{
    public static class Newton1d
    {
        public static double MethodSolve(Func<double, double> function, double initialGuess, double errorTol, int maxItterations = 1000, double bump = 1e-6)
        {
            double x = initialGuess;
            int itteration = 0;

            while (itteration < maxItterations && System.Math.Abs(function(x)) > errorTol)
            {
                var f = function(x);
                var fBumped = function(x + bump);
                var dfdx = (fBumped - f) / bump;
                x -= f / dfdx;
            }

            return x;
        }
    }
}
