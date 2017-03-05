using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Math.Matrix
{
    public static class FastMatrixExtensions
    {
        public static double[][] Multiply(this IFastMatrix self, IFastMatrix other)
        {
            if(self.Columns != other.Rows) throw new InvalidOperationException("Non - conformable matrices");

            var output = DoubleArrayFunctions.MatrixCreate(self.Rows, other.Columns);
            var aRows = self.Rows;
            var bCols = other.Columns;
            var bRows = other.Rows;

            for(int i = 0;i < aRows; i++)
            {
                for(int j = 0; j < bCols; j++)
                {
                    for(int k = 0; k < bRows; k++)
                    {
                        output[i][j] += self[i,k] * other[k,j];
                    }
                }
            }
            return output;
        }

        public static double[][] Multiply(this double[][] self, IFastMatrix other)
        {
            int aRows = self.Length;
            int aCols = self[0].Length;
            int bRows = other.Rows;
            int bCols = other.Columns;
            if (aCols != bRows) throw new InvalidOperationException("Non-conformable matrices");

            var output = DoubleArrayFunctions.MatrixCreate(aRows, other.Columns);
            
            for (int i = 0; i < aRows; i++)
            {
                for (int j = 0; j < bCols; j++)
                {
                    for (int k = 0; k < bRows; k++)
                    {
                        output[i][j] += self[i][k] * other[k, j];
                    }
                }
            }
            return output;
        }
    }
}
