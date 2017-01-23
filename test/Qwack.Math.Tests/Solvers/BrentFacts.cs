﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Interpolation;
using Xunit;

namespace Qwack.Math.Tests.Solvers
{
    public class BrentFacts
    {
        private double QuadraticFunction1d(double input)
        {
            var x = input;
            var y = 2 * x * x + 22 * x - 17;
            return y;
        }

        [Fact]
        public void CanSolveTameQuadratic1dFact()
        {
            var output = Math.Solvers.Brent.BrentsMethodSolve(QuadraticFunction1d, -10, 10, 1e-8);
            var functionOutput = QuadraticFunction1d(output);
            Assert.Equal(0, functionOutput, 8);
        }
    }
}
