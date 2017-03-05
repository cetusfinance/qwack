using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Solvers
{
    public class GaussNewtonFacts
    {
        double[] years = new double[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        double[] population = new double[] { 8.3, 11, 14.7, 19.7, 26.7, 35.2, 44.4, 55.9 };
        //https://www.math.lsu.edu/system/files/MunozGroup1%20-%20Paper.pdf

        private double[] Residuals(double[] inputs)
        {
            //2d only so only use first two inputs
            var x1 = inputs[0];
            var x2 = inputs[1];
            var residuals = years.Select((t, ix) => x1 * System.Math.Exp(x2 * t) - population[ix]);
            return residuals.ToArray();
        }


        [Fact]
        public void CanSolveTameExample()
        {
            var n2Sol = new Math.Solvers.GaussNewton
            {
                ObjectiveFunction = Residuals,
                InitialGuess = new double[] { 6, 0.3 },
                Tollerance = 1e-8
            };

            var output = n2Sol.Solve();
            var functionOutput = Residuals(output);
            var functionOutputUp = Residuals(output.Select(x => x + 0.0001).ToArray());
            var functionOutputDown = Residuals(output.Select(x => x - 0.0001).ToArray());

            //verify that the solution is better than two neighboruing solutions
            Assert.True(functionOutput.Select(x => x * x).Sum() < functionOutputUp.Select(x => x * x).Sum());
            Assert.True(functionOutput.Select(x => x * x).Sum() < functionOutputDown.Select(x => x * x).Sum());
        }
    }
}
