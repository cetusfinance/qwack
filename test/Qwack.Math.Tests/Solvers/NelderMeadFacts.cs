using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Qwack.Math.Solvers;
using Xunit;

namespace Qwack.Math.Tests.Solvers
{
    public class NelderMeadFacts
    {
        private double QuadraticFunction1d(double[] inputs)
        {
            //1d only so only use first input
            var x = inputs[0];
            var y = 2 * x * x + 22 * x - 17;
            return y*y;
        }

        private double QuadraticFunction2d(double[] inputs)
        {
            //2d only so only use first two inputs
            var x = inputs[0];
            var y = inputs[1];
            var a = 3 * x * y + 44 * x - 100;
            return a*a;
        }

        [Fact]
        public void CanSolveTameQuadratic1dFact()
        {
            var output = NelderMead.MethodSolve(QuadraticFunction1d, new double[] { 0 }, new double[] { 0.5 }, 1e-8, 10000);
            var functionOutput = QuadraticFunction1d(output);
            Assert.Equal(0, functionOutput, 4);
        }

        [Fact]
        public void CanSolveTameQuadratic2dFact()
        {
            var output = NelderMead.MethodSolve(QuadraticFunction2d, new double[] { 0,0 }, new double[] { 0.5,0.5 }, 1e-8, 10000);
            var functionOutput = QuadraticFunction2d(output);
            Assert.Equal(0, functionOutput, 4);
        }
    }
}
