using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Math.Extensions;
using Qwack.Math.Matrix;

namespace Qwack.Math.Regression
{
    public class MultipleLinearRegressor
    {
        public double[] Betas { get; }
        public double Alpha { get; }

        public MultipleLinearRegressor(double[] weights)
        {
            Alpha = weights[0];
            Betas = weights.SubArray(1, weights.Length - 1);
        }

        public double Regress(double[] values)
        {
            return DoubleArrayFunctions.VectorProduct(values, Betas) + Alpha;
        }
    }
}
