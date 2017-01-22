using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Solvers
{
    public class NewtonMultiDimFacts
    {
        private double[] QuadraticFunction1d(double[] inputs)
        {
            //1d only so only use first input
            var x = inputs[0];
            var y = 2 * x * x + 22 * x - 17;
            return new double[] { y };
        }

        private double[] QuadraticFunction2d(double[] inputs)
        {
            //2d only so only use first two inputs
            var x = inputs[0];
            var y = inputs[1];
            var a = 3 * x * y + 44 * x - 100;
            var b = 1 * x * x + 22 * y - 10;
            return new double[] { a, b };
        }

        [Fact]
        public void CanSolveTameQuadratic1dFact()
        {
            var n2Sol = new Math.Solvers.NewtonRaphsonMultiDimensionalSolver
            {
                ObjectiveFunction = QuadraticFunction1d,
                InitialGuess = new double[] { 0 },
                Tollerance = 1e-8
            };

            var output = n2Sol.Solve();
            var functionOutput = QuadraticFunction1d(output);
            Assert.Equal(0, functionOutput[0], 8);
        }

        [Fact]
        public void CanSolveTameQuadratic2dFact()
        {
            var n2Sol = new Math.Solvers.NewtonRaphsonMultiDimensionalSolver
            {
                ObjectiveFunction = QuadraticFunction2d,
                InitialGuess = new double[] { 0,0 },
                Tollerance = 1e-8
            };

            var output = n2Sol.Solve();
            var functionOutput = QuadraticFunction2d(output);
            Assert.Equal(0, functionOutput[0], 8);
            Assert.Equal(0, functionOutput[1], 8);
        }
    }
}
