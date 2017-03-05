using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Math.Tests.MatrixFunctions
{
    public class InvertAndMultiply
    {
        [Fact]
        public void CanInvert()
        {
            var a = new double[][]
            {
                new double[] { 1, 2, 3 },
            };
            var inverse = Matrix.DoubleArrayFunctions.InvertMatrix(a);
            var transpose = Matrix.DoubleArrayFunctions.Transpose(a);
            var unity = Matrix.DoubleArrayFunctions.MatrixProduct(inverse[0], transpose);
        }

        [Fact]
        public void IdentityRecovered()
        {
            var a = new double[][] 
            {
                new double[] { 1, 2, 3 },
                new double[] { 2, 1, 2 },
                new double[] { 3, 2, 3 }
            }; 
            
            var inverse = Matrix.DoubleArrayFunctions.InvertMatrix(a);
            var unity = Matrix.DoubleArrayFunctions.MatrixProduct(inverse, a);

            for(int r=0;r<unity.Length;r++)
            {
                for(int c=0;c<unity[0].Length;c++)
                {
                    if (r == c)
                        Assert.Equal(1.0, unity[r][c], 10);
                    else
                        Assert.Equal(0.0, unity[r][c], 10);
                }
            }
        }
    }
}
