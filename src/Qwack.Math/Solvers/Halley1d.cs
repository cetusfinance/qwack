using System;

namespace Qwack.Math.Solvers
{
    public static class Halley1d
    {
        //https://en.wikipedia.org/wiki/Halley%27s_method
        public static double MethodSolve(Func<double, double> function, double initialGuess, double errorTol, int maxItterations = 1000, double bump = 1e-6)
        {
            var x = initialGuess;
            var itteration = 0;

            while (itteration < maxItterations && System.Math.Abs(function(x)) > errorTol)
            {
                var f = function(x);
                var fPlus = function(x + bump);
                var fMinus = function(x - bump);
                var dfdxPlus = (fPlus - f) / bump;
                var dfdxMinus = (f - fMinus) / bump;
                var dfdx = (dfdxPlus + dfdxMinus) / 2.0;
                var d2fdx2 = (dfdxPlus - dfdxMinus) / bump;
                x -= 2.0 * f * dfdx / (2 * dfdx * dfdx - f * d2fdx2);
            }

            return x;
        }

        public static double MethodSolve(Func<double, double> function, Func<double, double> derivativeFunction, Func<double, double> secondDerivativeFunction, double initialGuess, double errorTol, int maxItterations = 1000)
        {
            var x = initialGuess;
            var itteration = 0;

            while (itteration < maxItterations && System.Math.Abs(function(x)) > errorTol)
            {
                var f = function(x);
                var dfdx = derivativeFunction(x);
                var d2fdx2 = secondDerivativeFunction(x);
                x -= 2.0 * f * dfdx / (2 * dfdx * dfdx - f * d2fdx2);
            }

            return x;
        }
    }
}
