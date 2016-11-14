using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Math.Matrix
{
    public static class DestructiveFunctions
    {
        public static double[] MatrixProduct(double[] vectorA, double[][] matrixB)
        {
            int aCols = vectorA.Length;
            int bRows = matrixB.Length;
            int bCols = matrixB[0].Length;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices");
            var result = new double[vectorA.Length];
            for (int j = 0; j < bCols; ++j) // each col of B
            {
                for (int k = 0; k < bRows; ++k)
                {// could use k < bRows
                    result[j] += vectorA[k] * matrixB[k][j];
                }
            }
            return result;
        }
    }
}
