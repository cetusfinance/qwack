using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Math.Matrix;
using Xunit;

namespace Qwack.Math.Tests.MatrixFunctions
{
    public class EigenvalueFacts
    {
        //https://en.wikipedia.org/wiki/Rayleigh_quotient_iteration

        [Fact]
        public void RayleighFacts()
        {
            var a = new double[][]
            {
                new double[] { 2, 1 },
                new double[] { 1, 2 },
            };

            var (eigenValue, eigenVector) = a.RayleighQuotient(1e-6, new[] { 1.0, 1.0 }, 100);
            Assert.Equal(3.0, eigenValue, 6);

            a = new double[][]
            {
                new double[] { 2, 0,0 },
                new double[] { 0,3,4 },
                new double[] { 0,4,9 },
            };

            (eigenValue, eigenVector) = a.RayleighQuotient(1e-6, new[] { 1.0, 1.0, 1.0 }, 100);
            Assert.Equal(11.0, eigenValue, 6);
        }

    }
}
