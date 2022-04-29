using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Qwack.Math.Matrix;

namespace Qwack.Math.Regression
{
    public class MultipleLinearRegression
    {
        //http://dept.stat.lsa.umich.edu/~kshedden/Courses/Stat401/Notes/401-multreg.pdf
        //examples in rows, weights in columns
        public static double[] RegressHistorical(double[][] predictors, double[] predictions)
        {
            if (predictors.Length != predictions.Length)
                throw new InvalidOperationException("Number of predictor rows should equal the number of predictions");

            var designMatrix = DoubleArrayFunctions.MatrixCreate(predictors.Length, predictors[0].Length + 1);

            for (var r = 0; r < predictions.Length; r++)
            {
                designMatrix[r][0] = 1.0;
                for (var c = 0; c < predictors[0].Length; c++)
                {
                    designMatrix[r][c + 1] = predictors[r][c];
                }
            }

            //do the math
            var X = designMatrix;
            var Xt = DoubleArrayFunctions.Transpose(X);
            var Z1 = DoubleArrayFunctions.MatrixProduct(Xt, X);
            var Z2 = DoubleArrayFunctions.InvertMatrix(Z1);
            var Z3 = DoubleArrayFunctions.MatrixProduct(Z2, Xt);
            var weights = DoubleArrayFunctions.MatrixProduct(Z3, predictions);
            return weights;
        }

        //http://dept.stat.lsa.umich.edu/~kshedden/Courses/Stat401/Notes/401-multreg.pdf
        //examples in rows, weights in columns
        public unsafe static double[] Regress(double[][] predictors, double[] predictions)
        {
            if (predictors.Length != predictions.Length)
                throw new InvalidOperationException("Number of predictor rows should equal the number of predictions");

            var numberOfCols = predictors[0].Length + 1;
            var designMatrix = DoubleArrayFunctions.MatrixCreate(predictors[0].Length + 1, predictors.Length);

            for (var r = 0; r < predictions.Length; r++)
            {
                designMatrix[0][r] = 1.0;
                for (var c = 0; c < predictors[0].Length; c++)
                {
                    designMatrix[c + 1][r] = predictors[r][c];
                }
            }

            var result = new double[designMatrix.Length][];
            var iterations = result.Length * result.Length;
            var vectors = designMatrix[0].Length / Vector<double>.Count;

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new double[result.Length];
            }

            for (var counter = 0; counter < iterations; counter++)
            {
                var column1 = counter / result.Length;
                var column2 = counter % result.Length;
                if (column2 > column1)
                {
                    continue;
                }
                var sum = 0.0;
                var vectorList1 = Unsafe.As<Vector<double>[]>(designMatrix[column1]);
                var vectorList2 = Unsafe.As<Vector<double>[]>(designMatrix[column2]);
                for (var i = 0; i < vectors; i++)
                {
                    sum += Vector.Dot(vectorList1[i], vectorList2[i]);
                }
                for (var i = vectors * Vector<double>.Count; i < designMatrix[0].Length; i++)
                {
                    sum += designMatrix[column1][i] * designMatrix[column2][i];
                }
                result[column1][column2] = sum;
                result[column2][column1] = sum;
            }

            var Z1 = result;
            var Z2 = DoubleArrayFunctions.InvertMatrix(Z1);
            var Z3 = DoubleArrayFunctions.MatrixProductBounds(Z2, designMatrix);
            var weights = DoubleArrayFunctions.MatrixProductBounds(Z3, predictions);
            return weights;
        }

    }
}
