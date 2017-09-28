using System;
using System.Linq;
using static System.Math;

namespace Qwack.Math.Matrix
{
    public static class DoubleArrayFunctions
    {
        public static double[][] InvertMatrix(double[][] a)
        {
            var n = a.Length;
            //e will represent each column in the identity matrix
            //x will hold the inverse matrix to be returned
            var x = new double[n][];
            for (var i = 0; i < n; i++)
            {
                x[i] = new double[a[i].Length];
            }

            //Get the LU matrix and P matrix (as an array)
            var results = LupDecomposition(a);

            var lu = results.Item1;
            var p = results.Item2;

            /*
            * Solve AX = e for each column ei of the identity matrix using LUP decomposition
            * */
            for (var i = 0; i < n; i++)
            {
                var e = new double[a[i].Length];
                e[i] = 1;
                var solve = LupSolve(lu, p, e);
                for (var j = 0; j < solve.Length; j++)
                {
                    x[j][i] = solve[j];
                }
            }
            return x;
        }

        public static double[] LupSolve(double[][] lu, int[] pi, double[] b)
        {
            var n = lu.Length - 1;
            var x = new double[n + 1];
            var y = new double[n + 1];

            /*
            * Solve for y using formward substitution
            * */
            for (var i = 0; i <= n; i++)
            {
                double suml = 0;
                for (var j = 0; j <= i - 1; j++)
                {
                    /*
                    * Since we've taken L and U as a singular matrix as an input
                    * the value for L at index i and j will be 1 when i equals j, not LU[i][j], since
                    * the diagonal values are all 1 for L.
                    * */
                    var lij = i == j ? 1 : lu[i][j];
                    suml = suml + (lij * y[j]);
                }
                y[i] = b[pi[i]] - suml;
            }
            //Solve for x by using back substitution
            for (var i = n; i >= 0; i--)
            {
                var sumu = 0.0;
                for (var j = i + 1; j <= n; j++)
                {
                    sumu = sumu + (lu[i][j] * x[j]);
                }
                x[i] = (y[i] - sumu) / lu[i][i];
            }
            return x;
        }

        public static Tuple<double[][], int[]> LupDecomposition(double[][] A)
        {
            var a = new double[A.Length][];
            for (var i = 0; i < a.Length; i++)
            {
                a[i] = new double[A[i].Length];
                Array.Copy(A[i], a[i], a[i].Length);
            }

            var n = a.Length - 1;
            /*
            * pi represents the permutation matrix.  We implement it as an array
            * whose value indicates which column the 1 would appear.  We use it to avoid 
            * dividing by zero or small numbers.
            * */
            var pi = new int[n + 1];
            var kp = 0;

            //Initialize the permutation matrix, will be the identity matrix
            for (var j = 0; j <= n; j++)
            {
                pi[j] = j;
            }

            for (var k = 0; k <= n; k++)
            {
                /*
                * In finding the permutation matrix p that avoids dividing by zero
                * we take a slightly different approach.  For numerical stability
                * We find the element with the largest 
                * absolute value of those in the current first column (column k).  If all elements in
                * the current first column are zero then the matrix is singluar and throw an
                * error.
                * */
                double p = 0;
                for (var i = k; i <= n; i++)
                {
                    if (Abs(a[i][k]) <= p) continue;
                    p = Abs(a[i][k]);
                    kp = i;
                }
                //if (p == 0)
                //{
                //    throw new Exception("singular matrix");
                //}
                /*
                * These lines update the pivot array (which represents the pivot matrix)
                * by exchanging pi[k] and pi[kp].
                * */
                var pik = pi[k];
                var pikp = pi[kp];
                pi[k] = pikp;
                pi[kp] = pik;

                /*
                * Exchange rows k and kpi as determined by the pivot
                * */
                for (var i = 0; i <= n; i++)
                {
                    var aki = a[k][i];
                    var akpi = a[kp][i];
                    a[k][i] = akpi;
                    a[kp][i] = aki;
                }

                /*
                    * Compute the Schur complement
                    * */
                for (var i = k + 1; i <= n; i++)
                {
                    a[i][k] = a[i][k] / a[k][k];
                    for (var j = k + 1; j <= n; j++)
                    {
                        a[i][j] = a[i][j] - (a[i][k] * a[k][j]);
                    }
                }
            }
            return Tuple.Create(a, pi);
        }
        public static double[] MatrixProduct(double[] vectorA, double[][] matrixB)
        {
            var aCols = vectorA.Length;
            var bRows = matrixB.Length;
            var bCols = matrixB[0].Length;
            if (aCols != bRows) throw new InvalidOperationException("Non-conformable matrices");

            var result = new double[vectorA.Length];
            for (var j = 0; j < bCols; ++j) // each col of B
            {
                for (var k = 0; k < bRows; ++k)
                {// could use k < bRows
                    result[j] += vectorA[k] * matrixB[k][j];
                }
            }
            return result;
        }


        public static double[][] Transpose(double[][] matrix)
        {
            var o = new double[matrix[0].Length][];
            for (var r = 0; r < matrix[0].Length; r++)
            {
                o[r] = new double[matrix.Length];
                for (var c = 0; c < matrix.Length; c++)
                {
                    o[r][c] = matrix[c][r];
                }
            }

            return o;
        }

        public static bool IsSquare(this double[][] matrix)
        {
            var rows = matrix.Length;
            var cols = matrix[0].Length;
            return rows == cols;
        }

        public static double MaxElement(this double[][] matrix)
        {
            return matrix.Max(x => x.Max());
        }

        public static double MaxAbsElement(this double[][] matrix)
        {
            return matrix.Max(x => x.Max(y=>Abs(y)));
        }

        public static double MinElement(this double[][] matrix)
        {
            return matrix.Min(x => x.Min());
        }

        public static double[][] MatrixProductBounds(double[][] matrixA, double[][] matrixB)
        {
            var aRows = matrixA.Length;
            var aCols = matrixA[0].Length;
            var bCols = matrixB[0].Length;
            if (aCols != matrixB.Length) throw new InvalidOperationException("Non-conformable matrices");

            var result = new double[aRows][];

            for (var i = 0; i < matrixA.Length; ++i) // each row of A
            {
                var resultRow = new double[bCols];
                var matrixARow = matrixA[i];
                for (var j = 0; j < bCols; ++j) // each col of B
                {
                    for (var k = 0; k < matrixB.Length; ++k)
                    {
                        resultRow[j] += matrixARow[k] * matrixB[k][j];
                    }
                }
                result[i] = resultRow;
            }

            return result;
        }

        public static double[][] MatrixProduct(double[][] matrixA, double[][] matrixB)
        {
            var aRows = matrixA.Length;
            var aCols = matrixA[0].Length;
            var bRows = matrixB.Length;
            var bCols = matrixB[0].Length;
            if (aCols != bRows) throw new InvalidOperationException("Non-conformable matrices");

            var result = MatrixCreate(aRows, bCols);

            for (var i = 0; i < aRows; ++i) // each row of A
            {
                for (var j = 0; j < bCols; ++j) // each col of B
                {
                    for (var k = 0; k < bRows; ++k)
                    {
                        result[i][j] += matrixA[i][k] * matrixB[k][j];
                    }
                }
            }

            return result;
        }
        public static double[][] MatrixCreate(int rows, int cols)
        {
            var result = new double[rows][];
            for (var i = 0; i < rows; ++i)
            {
                result[i] = new double[cols];
            }

            return result;
        }

        public static double[] MatrixProduct(double[][] matrixA, double[] vectorB)
        {
            var aRows = matrixA.Length;
            var aCols = matrixA[0].Length;
            var bRows = vectorB.Length;
            if (aCols != bRows) throw new InvalidOperationException("Non-conformable matrices in MatrixProduct");

            var result = new double[aRows];
            for (var i = 0; i < aRows; ++i) // each row of A
            {
                for (var k = 0; k < aCols; ++k)
                {
                    result[i] += matrixA[i][k] * vectorB[k];
                }
            }

            return result;
        }

        public static double[] MatrixProductBounds(double[][] matrixA, double[] vectorB)
        {
            var aRows = matrixA.Length;
            var aCols = matrixA[0].Length;
            var bRows = vectorB.Length;
            if (aCols != bRows) throw new InvalidOperationException("Non-conformable matrices in MatrixProduct");

            var result = new double[aRows];
            for (var i = 0; i < matrixA.Length; ++i) // each row of A
            {
                var rowA = matrixA[i];
                for (var k = 0; k < aCols; ++k)
                {
                    result[i] += rowA[k] * vectorB[k];
                }
            }
            return result;
        }

        /// <summary>
        /// The Cholesky decomposition
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static double[][] Cholesky(this double[][] matrix)
        {
            if (!matrix.IsSquare()) throw new InvalidOperationException("Matrix must be square");

            var N = matrix.Length;

            var result = new double[N][];
            for (var r = 0; r < result.Length; r++)
            {
                result[r] = new double[N];
            }

            for (var r = 0; r < N; r++) // each row of A
            {
                for (var c = 0; c < N; c++)
                {
                    var element = matrix[r][c];

                    for (var k = 0; k < r; k++)
                    {
                        element -= result[r][k] * result[c][k];
                    }

                    if (r == c)
                    {
                        result[r][c] = Sqrt(element);
                    }
                    else if (r < c)
                    {
                        result[c][r] = element / result[r][r];
                    }

                }
            }
            return result;
        }
    }
}
