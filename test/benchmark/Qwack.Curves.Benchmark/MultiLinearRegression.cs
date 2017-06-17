using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Qwack.Curves.Benchmark
{
    [Config(typeof(QuickSpinConfig))]
    public class MultiLinearRegression
    {
        private static readonly double s_intercept = 76;
        private static double[] solveForValues;
        private double[][] predictors;
        private double[] predictions;
        
        [Params(1000,10000)]
        public int NumberOfExamples { get; set; }
        [Params(25,50)]
        public int Dimensions { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            predictors = new double[NumberOfExamples][];
            var R = new System.Random();
            for (var e = 0; e < predictors.Length; e++)
            {
                predictors[e] = new double[Dimensions];
                for(var x = 0; x < Dimensions;x++)
                {
                    predictors[e][x] = R.NextDouble();
                }
            }
            solveForValues = new double[Dimensions];
            for(var e = 0; e < solveForValues.Length;e++)
            {
                solveForValues[e] = R.NextDouble();
            }
            predictions = new double[predictors.Length];

            for (var e = 0; e < predictions.Length; e++)
            {
                predictions[e] = TestFunc(predictors[e],solveForValues);
            }

            double TestFunc(double[] xs, double[] factors)
            {
                var returnValue = s_intercept;
                for(var i = 0; i < xs.Length;i++)
                {
                    returnValue += xs[i] * factors[i];
                }
                return returnValue;
            }
        }

        [Benchmark]
        public double[] TwoDimensionCominedTPReflect() => Math.Regression.MultipleLinearRegression.Regress(predictors, predictions);
    }
}
