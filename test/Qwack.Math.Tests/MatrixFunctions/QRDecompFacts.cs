using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Math.Matrix;
using Xunit;

namespace Qwack.Math.Tests.MatrixFunctions
{
    public class QRDecompFacts
    {
        [Fact]
        public void QRFacts()
        {
            var a = new double[][]
            {
                new double[] { 12, -51, 4 },
                new double[] { 6, 167, -68 },
                new double[] { -4, 24, -41 },
            };

            var (Q, R) = a.QRHouseholder();

            var result = DoubleArrayFunctions.MatrixProduct(Q, R);

            for (var i = 0; i < a.Length; i++)
                for (var j = 0; j < a[0].Length; j++)
                    Assert.Equal(a[i][j], result[i][j], 8);
        }
    }
}
