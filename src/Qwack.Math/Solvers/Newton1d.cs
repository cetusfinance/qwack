using System;
using static System.Math;

namespace Qwack.Math.Solvers
{
    /// <summary>
    /// Implementation(s) of Newton's method for root finding in a single dimension
    /// </summary>
    public static class Newton1D
    {
        /// <summary>
        /// Implementation of Newton's method for root finding which uses a numerical estimate of the first derivative
        /// </summary>
        /// <param name="function">The function called to evaluate f(x). Takes a double as input and returns a double</param>
        /// <param name="initialGuess">The starting point for solving</param>
        /// <param name="errorTol">The tollerance which defines when the problem is solved</param>
        /// <param name="maxItterations">Maximum number of itterations to perform. Defaulted to 1000</param>
        /// <param name="bump">The bump size to use in computing the derivative.  Defaulted to 1e-6</param>
        /// <returns></returns>
        public static double MethodSolve(Func<double, double> function, double initialGuess, double errorTol, int maxItterations = 1000, double bump = 1e-6)
        {
            var x = initialGuess;
            var itteration = 0;

            while (itteration < maxItterations && Abs(function(x)) > errorTol)
            {
                var f = function(x);
                var fBumped = function(x + bump);
                var dfdx = (fBumped - f) / bump;
                x -= f / dfdx;
                itteration++;
            }

            return x;
        }

        /// <summary>
        /// Implementation of Newton's method for root finding which uses an exact derivative
        /// </summary>
        /// <param name="function">The function called to evaluate f(x). Takes a double as input and returns a double</param>
        /// <param name="derivativeFunction">The function called to evaluate f'(x). Takes a double as input and returns a double</param>
        /// <param name="initialGuess">The starting point for solving</param>
        /// <param name="errorTol">The tollerance which defines when the problem is solved</param>
        /// <param name="maxItterations">Maximum number of itterations to perform. Defaulted to 1000</param>
        /// <returns></returns>
        public static double MethodSolve(Func<double, double> function, Func<double, double> derivativeFunction, double initialGuess, double errorTol, int maxItterations = 1000)
        {
            var x = initialGuess;
            var itteration = 0;

            while (itteration < maxItterations && Abs(function(x)) > errorTol)
            {
                var f = function(x);
                var dfdx = derivativeFunction(x);
                x -= f / dfdx;
                itteration++;
            }

            return x;
        }

        /// <summary>
        /// Implementation of Newton's method for root finding which uses a numerical estimate of the first derivative
        /// </summary>
        /// <param name="function">The function called to evaluate f(x). Takes a double as input and returns a double</param>
        /// <param name="initialGuess">The starting point for solving</param>
        /// <param name="errorTol">The tollerance which defines when the problem is solved</param>
        /// <param name="maxItterations">Maximum number of itterations to perform. Defaulted to 1000</param>
        /// <param name="bump">The bump size to use in computing the derivative.  Defaulted to 1e-6</param>
        /// <returns></returns>
        public static double MethodSolveWithProgress(Func<double, double> function, double initialGuess, double errorTol, int maxItterations = 1000, double bump = 1e-6)
        {
            var x = initialGuess;
            var itteration = 0;

            var fLast = function(initialGuess);
            while (itteration < maxItterations && Abs(function(x)) > errorTol)
            {
                var f = function(x);
                var fBumped = function(x + bump);
                var dfdx = (fBumped - f) / bump;
                x -= f / dfdx;
                var fNew = function(x);
                if (Abs(fNew) > Abs(fLast))
                    break;
                fLast = fNew;
                itteration++;
            }

            return x;
        }
    }
}
