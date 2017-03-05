using System;
using System.Collections.Generic;
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
    
            double[] weights = new double[predictors[0].Length + 1]; //first element is intercept
            double[][] designMatrix = Matrix.DoubleArrayFunctions.MatrixCreate(predictors.Length, predictors[0].Length + 1);

            for(int r=0;r<predictions.Length;r++)
            {
                designMatrix[r][0] = 1.0;
                for(int c=0;c<predictors[0].Length;c++)
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
            weights = Matrix.DoubleArrayFunctions.MatrixProduct(Z3, predictions);
            return weights;
        }

        public static double[] RegressFaster(double[][] predictors, double[] predictions)
        {
            if (predictors.Length != predictions.Length)
                throw new InvalidOperationException("Number of predictor rows should equal the number of predictions");

            double[] weights = new double[predictors[0].Length + 1]; //first element is intercept
            var designMatrix = new Matrix.FastMatrixRowsFirst(predictors.Length, predictors[0].Length + 1);

            for (int r = 0; r < predictions.Length; r++)
            {
                designMatrix[r,0] = 1.0;
                for (int c = 0; c < predictors[0].Length; c++)
                {
                    designMatrix[r,c + 1] = predictors[r][c];
                }
            }

            //do the math
            var X = designMatrix;
            var Xt = FastMatrixColumnsFirst.Transpose(X);
            var Z1 = Xt.Multiply(X);
            var Z2 = DoubleArrayFunctions.InvertMatrix(Z1);
            var Z3 = FastMatrixExtensions.Multiply(Z2, Xt);
            weights = DoubleArrayFunctions.MatrixProduct(Z3, predictions);
            return weights;
        }
    }
}
