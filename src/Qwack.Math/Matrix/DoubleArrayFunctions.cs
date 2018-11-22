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

        public static double VectorProduct(double[] vectorA, double[] vectorB)
        {
            var aCols = vectorA.Length;
            var bRows = vectorB.Length;
            if (aCols != bRows) throw new InvalidOperationException("Non-conformable vectors");

            var result = 0.0;

            for (var k = 0; k < bRows; ++k)
            {
                result += vectorA[k] * vectorB[k];
            }

            return result;
        }

        public static double[][] RowVectorToMatrix(double[] vectorA)
        {
            var result = new double[1][];
            result[0] = vectorA;
            return result;
        }

        public static double[][] ColumnVectorToMatrix(double[] vectorA)
        {
            var result = new double[vectorA.Length][];
            for (var i = 0; i < vectorA.Length; i++)
                result[i] = new double[] { vectorA[i] };
            return result;
        }

        public static double[][] MatrixSubtract(this double[][] a, double[][] b)
        {
            if (a.Length != b.Length || a[0].Length != b[0].Length) throw new InvalidOperationException("Non-conformable matrices");

            var o = new double[a.Length][];
            for (var i = 0; i < o.Length; i++)
            {
                o[i] = new double[a[i].Length];

                for (var j = 0; j < o[i].Length; j++)
                {
                    o[i][j] = a[i][j] - b[i][j];
                }
            }
            return o;
        }

        public static double[][] MatrixAdd(this double[][] a, double[][] b)
        {
            if (a.Length != b.Length || a[0].Length != b[0].Length) throw new InvalidOperationException("Non-conformable matrices");

            var o = new double[a.Length][];
            for (var i = 0; i < o.Length; i++)
            {
                o[i] = new double[a[i].Length];

                for (var j = 0; j < o[i].Length; j++)
                {
                    o[i][j] = a[i][j] + b[i][j];
                }
            }
            return o;
        }


        public static double[][] Transpose(this double[][] matrix)
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

        public static double MaxElement(this double[][] matrix) => matrix.Max(x => x.Max());

        public static double MaxAbsElement(this double[][] matrix) => matrix.Max(x => x.Max(y => Abs(y)));

        public static double MinElement(this double[][] matrix) => matrix.Min(x => x.Min());

        public static double[][] GetColumn(this double[][] matrix, int col)
        {
            var o = new double[matrix[0].Length][];
            for (var i = 0; i < o.Length; i++)
            {
                o[i] = new[] { matrix[i][col] };
            }
            return o;
        }

        public static double[] GetColumnVector(this double[][] matrix, int col)
        {
            var o = new double[matrix[0].Length];
            for (var i = 0; i < o.Length; i++)
            {
                o[i] = matrix[i][col];
            }
            return o;
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

        public static double[][] Cholesky2(this double[][] a)
        {
            var n = a[0].Length;

            var ret = new double[n][];
            for (var r = 0; r < n; r++)
            {
                ret[r] = new double[n];
                for (var c = 0; c <= r; c++)
                {
                    if (c == r)
                    {
                        double sum = 0;
                        for (var j = 0; j < c; j++)
                        {
                            sum += ret[c][j] * ret[c][j];
                        }
                        ret[c][c] = Sqrt(a[c][c] - sum);
                    }
                    else
                    {
                        double sum = 0;
                        for (var j = 0; j < c; j++)
                            sum += ret[r][j] * ret[c][j];
                        ret[r][c] = 1.0 / ret[c][c] * (a[r][c] - sum);
                    }
                }
            }

            return ret;
        }

        public static double Norm(this double[] vectorA) => vectorA.Select(x => Abs(x)).Sum();
        public static double EuclidNorm(this double[] vectorA) => Sqrt(vectorA.Select(x => x * x).Sum());
        public static double[] Normalize(this double[] vectorA) => vectorA.Select(x => x / Norm(vectorA)).ToArray();
        public static double[] EuclidNormalize(this double[] vectorA) => vectorA.Select(x => x / EuclidNorm(vectorA)).ToArray();
        public static double Norm(this double[][] matrixA) => matrixA.Select(x => x.Select(y => Abs(y)).Sum()).Sum();
        public static double EuclidNorm(this double[][] matrixA) => Sqrt(matrixA.Select(x => x.Select(y => y * y).Sum()).Sum());

        public static double[][] DiagonalMatrix(double element, int size)
        {
            var o = new double[size][];
            for (var i = 0; i < size; i++)
            {
                o[i] = new double[size];
                o[i][i] = element;
            }
            return o;
        }

        public static double[][] EmptyMatrix(int rows, int cols)
        {
            var o = new double[rows][];
            for (var i = 0; i < rows; i++)
            {
                o[i] = new double[cols];
            }
            return o;
        }

        public static double Determinant(this double[][] a)
        {
            if (!a.IsSquare())
                throw new Exception("Cannot calculate determinant of a non-square matrix");
            var size = a.Length;
            if (size == 1)
                return a[0][0];
            if (size == 2)
                return a[0][0] * a[1][1] - a[1][0] * a[0][1];

            var total = 0.0;
            for (var i = 0; i < size; i++)
            {
                var subMatrix = new double[size - 1][];
                for (var j = 0; j < subMatrix.Length; j++)
                {
                    subMatrix[j] = new double[subMatrix.Length];
                }
                var sign = i % 2 == 1 ? -1.0 : 1.0;
                for (var j = 0; j < subMatrix.Length; j++)
                {
                    var colShift = j >= i ? 1 : 0;
                    for (var k = 0; k < subMatrix.Length; k++)
                    {
                        subMatrix[k][j] = a[k + 1][j + colShift];
                    }
                }
                total += a[0][i] * Determinant(subMatrix) * sign;
            }
            return total;
        }

        public static (double eigenValue, double[] eigenVector) RayleighQuotient(this double[][] a, double epsilon, double[] initialEigenVector, double initialEigenValue)
        {
            var size = a.Length;
            var norm = initialEigenVector.Norm();
            var eigenVector = initialEigenVector.Normalize();
            var eigenValue = initialEigenValue;


            var err = double.MaxValue;
            var breakkout = 0;
            while (Abs(err) > epsilon)
            {
                var muI = DiagonalMatrix(eigenValue, size);
                var r = a.MatrixSubtract(muI);
                r = InvertMatrix(r);
                eigenVector = MatrixProduct(r, eigenVector);
                eigenVector = eigenVector.Normalize();

                var q = VectorProduct(MatrixProduct(eigenVector, a), eigenVector);
                q /= VectorProduct(eigenVector, eigenVector);
                eigenValue = q;
                var y = MatrixSubtract(a, DiagonalMatrix(eigenValue, size));
                err = y.Determinant();

                if (breakkout > 10000)
                    throw new Exception("Failed to find eigen values / vectors");

                breakkout++;
            }

            return (eigenValue, eigenVector);
        }

        public static double[] ScalarDivide(this double[] vector, double divisor) => vector.Select(x => x / divisor).ToArray();
        public static double[][] ScalarDivide(this double[][] matrixA, double divisor) => matrixA.Select(x => x.Select(y => y / divisor).ToArray()).ToArray();
        public static double[] ScalarSubtract(this double[] vector, double subtractor) => vector.Select(x => x - subtractor).ToArray();
        public static double[][] ScalarSubtract(this double[][] matrixA, double subtractor) => matrixA.Select(x => x.Select(y => y - subtractor).ToArray()).ToArray();
        public static double VTV(this double[] v) => v.Select(x => x * x).Sum();

        public static double[][] Clone(double[][] matrix)
        {
            var o = new double[matrix.Length][];
            for (var i = 0; i < o.Length; i++)
            {
                o[i] = new double[matrix[i].Length];
                Array.Copy(matrix[i], o[i], o[i].Length);
            }
            return o;
        }

        private static double[][] ComputeMinor(this double[][] mat, int d)
        {
            var o = EmptyMatrix(mat.Length, mat[0].Length);
            for (var i = 0; i < d; i++)
                o[i][i] = 1.0;
            for (var i = d; i < o.Length; i++)
                for (var j = d; j < o[0].Length; j++)
                    o[i][j] = mat[i][j];
            return o;
        }

        private static double[] Vmadd(double[] a, double[] b, double s) => a.Select((x, ix) => x + s * b[ix]).ToArray();

        private static double[][] ComputeHouseholderFactor(double[] v)
        {
            var n = v.Length;
            var mat = EmptyMatrix(n, n);
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    mat[i][j] = -2 * v[i] * v[j];
            for (var i = 0; i < n; i++)
                mat[i][i] += 1;

            return mat;
        }

        public static (double[][] Q, double[][] R) QRHouseholder(this double[][] mat)
        {
            var m = mat.Length;
            var n = mat[0].Length;

            // array of factor Q1, Q2, ... Qm
            var qv = new double[m][][];

            // temp array
            var z = Clone(mat);
            double[][] z1;

            for (var k = 0; k < n && k < m - 1; k++)
            {
                var e = new double[m];
                // compute minor
                z1 = z.ComputeMinor(k);

                // extract k-th column into x
                var x = GetColumnVector(z1, k);

                var a = x.EuclidNorm();
                if (mat[k][k] > 0) a = -a;

                for (var i = 0; i < e.Length; i++)
                    e[i] = (i == k) ? 1 : 0;

                // e = x + a*e
                e = Vmadd(x, e, a);

                // e = e / ||e||
                e = e.EuclidNormalize();

                // qv[k] = I - 2 *e*e^T
                qv[k] = ComputeHouseholderFactor(e);

                // z = qv[k] * z1
                z = MatrixProduct(qv[k], z1);
            }

            var Q = qv[0];

            // after this loop, we will obtain Q (up to a transpose operation)
            for (var i = 1; i < n && i < m - 1; i++)
            {
                Q = MatrixProduct(qv[1], Q);
            }

            var R = MatrixProduct(Q, mat);
            Q = Transpose(Q);
            return (Q, R);
        }

        public static double[] QREigenValues(this double[][] a, double epsilon)
        {
            var A = a;

            var err = double.MaxValue;
            var breakkout = 0;
            while (Abs(err) > epsilon)
            {
                var (Q, R) = A.QRHouseholder();
                A = MatrixProduct(Q.Transpose(), MatrixProduct(A, Q));

                err = 0;
                for (var i = 1; i < A.Length; i++)
                    for (var j = 0; j < i; j++)
                    {
                        err += Abs(A[i][j]);
                    }

                if (breakkout > 100000)
                    throw new Exception("Failed to find eigen values / vectors");

                breakkout++;
            }
            var eigenValues = new double[A.Length];
            for (var i = 0; i < A.Length; i++)
                eigenValues[i] = A[i][i];
            return eigenValues;
        }

    }
}
