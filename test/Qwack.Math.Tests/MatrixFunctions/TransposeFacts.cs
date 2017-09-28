using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Math.Tests.MatrixFunctions
{
    public class TransposeFacts
    {
        [Fact]
        public void CanTranspose()
        {
            var a = new double[][]
            {
                new double[] { 1, 2, 3 },
                                new double[] { 1, 2, 3 },
                                                new double[] { 1, 2, 3 },
            };

            var transpose = Matrix.DoubleArrayFunctions.Transpose(a);
            var roundTrip = Matrix.DoubleArrayFunctions.Transpose(transpose);

            for (var r = 0; r < roundTrip.Length; r++)
            {
                for (var c = 0; c < roundTrip[0].Length; c++)
                {
                        Assert.Equal(a[r][c],roundTrip[r][c], 10);
                }
            }
        }
    }
}
