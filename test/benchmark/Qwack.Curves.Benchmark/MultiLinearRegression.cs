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
        private static readonly double s_w1 = 5;
        private static readonly double s_w2 = -2;
        private double[][] predictors;

        [Params(2000)]
        public int NumberOfExamples { get; set; }

        [Setup]
        public void Setup()
        {
            predictors = new double[NumberOfExamples][];
            var R = new System.Random();
            for (var e = 0; e < predictors.Length; e++)
            {
                predictors[e] = new double[2] { R.NextDouble(), R.NextDouble() };
            }
        }
        
        [Benchmark(Baseline = true)]
        public double[] TwoDimensionNR()
        {
            double[] ws0 = new double[] { s_intercept, s_w1, s_w2 };

            Func<double[], double[], double> testFunc = new Func<double[], double[], double>((xs, ws) =>
            {
                var intercept = ws[0];
                var w1 = ws[1];
                var w2 = ws[2];
                return intercept + xs[0] * w1 + xs[1] * w2;
            });

            Func<double[], double[]> solveFunc = new Func<double[], double[]>(ws =>
            {
                return predictors.Select(x => testFunc(x, ws) - testFunc(x, ws0)).ToArray();
            });

            var solver = new Math.Solvers.GaussNewton
            {
                ObjectiveFunction = solveFunc,
                InitialGuess = new double[3]
            };

            return solver.Solve();
        }

        [Benchmark]
        public double[] TwoDimensionFaster()
        {
            Func<double[], double> testFunc = new Func<double[], double>(xs =>
            {
                return s_intercept + xs[0] * s_w1 + xs[1] * s_w2;
            });

            var predictions = new double[predictors.Length];

            for (int e = 0; e < predictions.Length; e++)
            {
                 predictions[e] = testFunc(predictors[e]);
            }

            return Math.Regression.MultipleLinearRegression.Regress(predictors, predictions);
        }

        [Benchmark]
        public double[] TwoDimensionFasterNoTranspose()
        {
            Func<double[], double> testFunc = new Func<double[], double>(xs =>
            {
                return s_intercept + xs[0] * s_w1 + xs[1] * s_w2;
            });

            var predictions = new double[predictors.Length];

            for (int e = 0; e < predictions.Length; e++)
            {
                predictions[e] = testFunc(predictors[e]);
            }

            return Math.Regression.MultipleLinearRegression.RegressFaster(predictors, predictions);
        }

    }
}
