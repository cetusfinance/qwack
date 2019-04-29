using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Solvers
{
    public class Newton1dFacts
    {
        private double QuadraticFunction1d(double input)
        {
            var x = input;
            var y = 2 * x * x + 22 * x - 17;
            return y;
        }

        private double QuadraticFunction1dDerivative(double input)
        {
            var x = input;
            var y = 2 * x + 22;
            return y;
        }

        [Fact]
        public void CanSolveTameQuadratic1dFact()
        {
            var output = Math.Solvers.Newton1D.MethodSolve(QuadraticFunction1d, 10, 1e-8);
            var functionOutput = QuadraticFunction1d(output);
            Assert.Equal(0, functionOutput, 8);

            output = Math.Solvers.Newton1D.MethodSolveWithProgress(QuadraticFunction1d, 10, 1e-8);
            Assert.Equal(0, functionOutput, 8);
        }

        [Fact]
        public void CanSolveTameQuadratic1dWithDerivative()
        {
            var output = Math.Solvers.Newton1D.MethodSolve(QuadraticFunction1d, QuadraticFunction1dDerivative, 10, 1e-8);
            var functionOutput = QuadraticFunction1d(output);
            Assert.Equal(0, functionOutput, 8);
        }
    }
}
