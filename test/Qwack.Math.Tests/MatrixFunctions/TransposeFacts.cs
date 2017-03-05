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

            for (int r = 0; r < roundTrip.Length; r++)
            {
                for (int c = 0; c < roundTrip[0].Length; c++)
                {
                        Assert.Equal(a[r][c],roundTrip[r][c], 10);
                }
            }
        }

        [Fact]
        public void CanTransposeWithFast()
        {
            var a = new double[][]
            {
                new double[] { 1, 2, 3 },
                new double[] { 1, 2, 3 },
                new double[] { 1, 2, 3 },
            };

            var transpose = Matrix.DoubleArrayFunctions.Transpose(a);
            var fastrows = new Matrix.FastMatrixRowsFirst(3,3);
            var trans2 = Matrix.FastMatrixColumnsFirst.Transpose(fastrows);

            for (int r = 0; r < a.Length; r++)
            {
                for (int c = 0; c < a[0].Length; c++)
                {
                    fastrows[r,c] = a[r][c];
                }
            }

            for (int r = 0; r < transpose.Length; r++)
            {
                for (int c = 0; c < transpose[0].Length; c++)
                {
                    Assert.Equal(transpose[r][c], trans2[r,c], 10);
                }
            }
        }
    }
}
