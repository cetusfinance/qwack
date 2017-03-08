using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.Statistics.Models.Regression.Linear;
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

        [Setup]
        public void Setup()
        {
            predictors = new double[NumberOfExamples][];
            var R = new System.Random();
            for (var e = 0; e < predictors.Length; e++)
            {
                predictors[e] = new double[Dimensions];
                for(int x = 0; x < Dimensions;x++)
                {
                    predictors[e][x] = R.NextDouble();
                }
            }
            solveForValues = new double[Dimensions];
            for(int e = 0; e < solveForValues.Length;e++)
            {
                solveForValues[e] = R.NextDouble();
            }
            predictions = new double[predictors.Length];

            for (int e = 0; e < predictions.Length; e++)
            {
                predictions[e] = TestFunc(predictors[e],solveForValues);
            }

            double TestFunc(double[] xs, double[] factors)
            {
                var returnValue = s_intercept;
                for(int i = 0; i < xs.Length;i++)
                {
                    returnValue += xs[i] * factors[i];
                }
                return returnValue;
            }
        }

        [Benchmark(Baseline = true)]
        public double[] AccordVersion()
        {
            var model = MultipleLinearRegression.FromData(predictors, predictions);
            var weights = model.Weights;
            return weights;
        }


        [Benchmark]
        public double[] TwoDimensionFasterBounds()
        {
            return Math.Regression.MultipleLinearRegression.RegressBounds(predictors, predictions);
        }

        [Benchmark]
        public double[] TwoDimensionFaster()
        {
            return Math.Regression.MultipleLinearRegression.Regress(predictors, predictions);
        }

        //[Benchmark]
        //public double[] TwoDimensionFasterNoTranspose()
        //{
        //    return Math.Regression.MultipleLinearRegression.RegressFaster(predictors, predictions);
        //}

        //[Benchmark()]
        ////public double[] TwoDimensionNR()
        //{
        //    double[] ws0 = new double[] { s_intercept, s_w1, s_w2 };

        //    Func<double[], double[], double> testFunc = new Func<double[], double[], double>((xs, ws) =>
        //    {
        //        var intercept = ws[0];
        //        var w1 = ws[1];
        //        var w2 = ws[2];
        //        return intercept + xs[0] * w1 + xs[1] * w2;
        //    });

        //    Func<double[], double[]> solveFunc = new Func<double[], double[]>(ws =>
        //    {
        //        return predictors.Select(x => testFunc(x, ws) - testFunc(x, ws0)).ToArray();
        //    });

        //    var solver = new Math.Solvers.GaussNewton
        //    {
        //        ObjectiveFunction = solveFunc,
        //        InitialGuess = new double[3]
        //    };

        //    return solver.Solve();
        //}


    }
}
