using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Math.Matrix;
using Xunit;

namespace Qwack.Math.Tests.MatrixFunctions
{
    public class DeterminantFacts
    {
        //https://www.mathsisfun.com/algebra/matrix-determinant.html

        [Fact]
        public void Det1x1()
        {
            var a = new double[][]
            {
                new double[] { 4 }
            };

            var det = a.Determinant();
            Assert.Equal(4, det, 10);
        }

        [Fact]
        public void Det2x2()
        {
            var a = new double[][]
            {
                new double[] { 4, 6 },
                new double[] { 3, 8 },
            };

            var det = a.Determinant();
            Assert.Equal(14, det, 10);
        }

        [Fact]
        public void Det3x3()
        {
            var a = new double[][]
            {
                new double[] { 6, 1, 1 },
                new double[] { 4,-2, 5 },
                new double[] { 2,8, 7 },
            };

            var det = a.Determinant();
            Assert.Equal(-306, det, 10);
        }
    }
}
