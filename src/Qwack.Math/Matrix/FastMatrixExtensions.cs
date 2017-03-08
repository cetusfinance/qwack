using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Math.Matrix
{
    public static class FastMatrixExtensions
    {
        public unsafe static double[][] Multiply(this IFastMatrix self, IFastMatrix other)
        {
            if(self.Columns != other.Rows) throw new InvalidOperationException("Non - conformable matrices");

            var output = DoubleArrayFunctions.MatrixCreate(self.Rows, other.Columns);
            var aRows = self.Rows;
            var bCols = other.Columns;
            var bRows = other.Rows;

            double* selfPtr = self.Pointer;
            double* otherPtr = other.Pointer;

            for(int i = 0;i < aRows; i++)
            {
                for(int j = 0; j < bCols; j++)
                {
                    for(int k = 0; k < bRows; k++)
                    {
                        output[i][j] += selfPtr[self.GetIndex(i,k)] * otherPtr[other.GetIndex(k,j)];
                    }
                }
            }
            return output;
        }

        public unsafe static double[][] Multiply(this double[][] self, IFastMatrix other)
        {
            int aRows = self.Length;
            int aCols = self[0].Length;
            int bRows = other.Rows;
            int bCols = other.Columns;
            if (aCols != bRows) throw new InvalidOperationException("Non-conformable matrices");

            var output = DoubleArrayFunctions.MatrixCreate(aRows, other.Columns);
            double* otherPtr = other.Pointer;

            for (int i = 0; i < aRows; i++)
            {
                for (int j = 0; j < bCols; j++)
                {
                    for (int k = 0; k < bRows; k++)
                    {
                        output[i][j] += self[i][k] * otherPtr[other.GetIndex(k, j)];
                    }
                }
            }
            return output;
        }

        //public unsafe static double[][] TransposeMultipliedByProduct(double* ptr, int rows, int columns)
        //{
        //    var output = new double[rows][];
            
        //    for(int i = 0; i < rows; i++)
        //    {
        //        var outRow = new double[rows];
        //        output[i] = outRow;
        //    }
        //}
    }
}
