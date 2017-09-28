using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Math.Tests.MatrixFunctions
{
    public class CholeskyFacts
    {
        [Fact]
        public void CanDecompose()
        {
            var a = new double[][]
            {
                new double[] { 1.0, 0.5 },
                new double[] { 0.5, 1.0 },

            };

            var expected = new double[][]
            {
                new double[] { 1.0, 0 },
                new double[] { 0.5, 0.8660254037844386 },

            };

            var decomp = Matrix.DoubleArrayFunctions.Cholesky(a);

            for (var r = 0; r < a.Length; r++)
            {
                for (var c = 0; c < a[0].Length; c++)
                {
                    Assert.Equal(expected[r][c], decomp[r][c], 10);
                }
            }
        }

    }
}
