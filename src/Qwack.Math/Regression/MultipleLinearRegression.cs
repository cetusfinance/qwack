using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Qwack.Math.Matrix;

namespace Qwack.Math.Regression
{
    public class MultipleLinearRegression
    {
        //http://dept.stat.lsa.umich.edu/~kshedden/Courses/Stat401/Notes/401-multreg.pdf
        //examples in rows, weights in columns
        public static double[] Regress(double[][] predictors, double[] predictions)
        {
            if (predictors.Length != predictions.Length)
                throw new InvalidOperationException("Number of predictor rows should equal the number of predictions");

            double[][] designMatrix = Matrix.DoubleArrayFunctions.MatrixCreate(predictors.Length, predictors[0].Length + 1);

            for (int r = 0; r < predictions.Length; r++)
            {
                designMatrix[r][0] = 1.0;
                for (int c = 0; c < predictors[0].Length; c++)
                {
                    designMatrix[r][c + 1] = predictors[r][c];
                }
            }

            //do the math
            var X = designMatrix;
            var Xt = Matrix.DoubleArrayFunctions.Transpose(X);
            var Z1 = Matrix.DoubleArrayFunctions.MatrixProduct(Xt, X);
            var Z2 = Matrix.DoubleArrayFunctions.InvertMatrix(Z1);
            var Z3 = Matrix.DoubleArrayFunctions.MatrixProduct(Z2, Xt);
            var weights = Matrix.DoubleArrayFunctions.MatrixProduct(Z3, predictions);
            return weights;
        }

        //http://dept.stat.lsa.umich.edu/~kshedden/Courses/Stat401/Notes/401-multreg.pdf
        //examples in rows, weights in columns
        public unsafe static double[] RegressBounds(double[][] predictors, double[] predictions)
        {
            if (predictors.Length != predictions.Length)
                throw new InvalidOperationException("Number of predictor rows should equal the number of predictions");
                        
            var numberOfCols = predictors[0].Length + 1; 
            var designMatrix = Matrix.DoubleArrayFunctions.MatrixCreate(predictors[0].Length + 1, predictors.Length);
            
            for (int r = 0; r < predictions.Length; r++)
            {
                designMatrix[0][ r] = 1.0;
                for (int c = 0; c < predictors[0].Length; c++)
                {
                    designMatrix[c + 1][r] = predictors[r][c];
                }
            }

            ////do the math
            //var X = designMatrix;
            //for(var r = 0; r < predictions.Length; r++)
            //{
            //    for(var c = 0; c < designMatrix.Length; c += Vector<double>.Count)
            //    {
            //        //var vector = Unsafe.Read<Vector<double>
            //    }
            //}

            var result = new double[designMatrix.Length][];
            var iterations = result.Length * result.Length;
            for(int i = 0; i < result.Length;i++)
            {
                result[i] = new double[result.Length];
            }

            for (int counter = 0; counter < iterations; counter++)
            {
                var column1 = counter / result.Length;
                var column2 = counter % result.Length;
                double sum = 0.0;
                for (int i = 0; i < designMatrix[0].Length; i++)
                {
                    sum += designMatrix[column1][i] * designMatrix[column2][i];
                }
                result[column1][column2] = sum;
            }

            //var X = designMatrix;
            //var Xt = Matrix.DoubleArrayFunctions.Transpose(X);
            var Z1 = result; // Matrix.DoubleArrayFunctions.MatrixProductBounds(Xt, X);
            var Z2 = Matrix.DoubleArrayFunctions.InvertMatrix(Z1);
            var Z3 = Matrix.DoubleArrayFunctions.MatrixProductBounds(Z2, designMatrix);
            var weights = Matrix.DoubleArrayFunctions.MatrixProductBounds(Z3, predictions);
            return weights;
        }

        public unsafe static double[] RegressBetter(double[][] predictors, double[] predictions)
        {
            if (predictors.Length != predictions.Length)
                throw new InvalidOperationException("Number of predictor rows should equal the number of predictions");

            var numberOfCols = predictors[0].Length + 1;
            var designMatrix = Matrix.DoubleArrayFunctions.MatrixCreate(predictors[0].Length + 1, predictors.Length);

            for (int r = 0; r < predictions.Length; r++)
            {
                designMatrix[0][r] = 1.0;
                for (int c = 0; c < predictors[0].Length; c++)
                {
                    designMatrix[c + 1][r] = predictors[r][c];
                }
            }
            
            var result = new double[designMatrix.Length][];
            var iterations = result.Length * result.Length;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new double[result.Length];
            }

            for (int counter = 0; counter < iterations; counter++)
            {
                var column1 = counter / result.Length;
                var column2 = counter % result.Length;
                if(column2 > column1)
                {
                    continue;
                }
                double sum = 0.0;
                for (int i = 0; i < designMatrix[0].Length; i++)
                {
                    sum += designMatrix[column1][i] * designMatrix[column2][i];
                }
                result[column1][column2] = sum;
                result[column2][column1] = sum;
            }

            var Z1 = result;
            var Z2 = Matrix.DoubleArrayFunctions.InvertMatrix(Z1);
            var Z3 = Matrix.DoubleArrayFunctions.MatrixProductBounds(Z2, designMatrix);
            var weights = Matrix.DoubleArrayFunctions.MatrixProductBounds(Z3, predictions);
            return weights;
        }
        
    }
}
