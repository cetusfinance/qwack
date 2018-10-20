using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Math.Regression;
using Xunit;

namespace Qwack.Math.Tests.Regression
{
    public class MultipleLinearRegressionFacts
    {
        [Fact]
        public void Dim2Facts()
        {
            double intercept = 76;
            double w1 = 5;
            double w2 = -2;

            var testFunc = new Func<double[], double>(xs =>
            {
                return intercept + xs[0] * w1 + xs[1] * w2;
            });

            var nExamples = 7;
            var predictors = new double[nExamples][];
            var predictions = new double[nExamples];

            var R = new System.Random(777);

            for (var e = 0; e < nExamples; e++)
            {
                predictors[e] = new double[2] { R.NextDouble(), R.NextDouble() };
                predictions[e] = testFunc(predictors[e]);
            }

            var weights = MultipleLinearRegression.Regress(predictors, predictions);
            Assert.Equal(intercept, weights[0], 8);
            Assert.Equal(w1, weights[1], 8);
            Assert.Equal(w2, weights[2], 8);

            var weights2 = MultipleLinearRegression.RegressHistorical(predictors, predictions);
            Assert.Equal(weights, weights2);

            var badPredictions = new double[nExamples - 1];
            Assert.Throws<InvalidOperationException>(() => MultipleLinearRegression.RegressHistorical(predictors, badPredictions));
            Assert.Throws<InvalidOperationException>(() => MultipleLinearRegression.Regress(predictors, badPredictions));

            var z = new MultipleLinearRegressor(weights);
            var q = z.Regress(new[] { 0.0, 0.0 });
        }

        [Fact]
        public void Dim2NRFacts()
        {
            var ws0 = new double[] { 76, 5, -2 };

            var nExamples = 100;
            var predictors = new double[nExamples][];
            var predictions = new double[nExamples];

            var R = new System.Random();
            for (var e = 0; e < nExamples; e++)
            {
                predictors[e] = new double[2] { R.NextDouble(), R.NextDouble() };
            }

            var testFunc = new Func<double[], double[], double>((xs, ws) =>
            {
                var intercept = ws[0];
                var w1 = ws[1];
                var w2 = ws[2];
                return intercept + xs[0] * w1 + xs[1] * w2;
            });

            var solveFunc = new Func<double[], double[]>(ws =>
            {
                return predictors.Select(x => testFunc(x, ws) - testFunc(x, ws0)).ToArray();
            });

            var solver = new Math.Solvers.GaussNewton
            {
                ObjectiveFunction = solveFunc,
                InitialGuess = new double[3]
            };

            var weights = solver.Solve();
            Assert.Equal(ws0[0], weights[0], 8);
            Assert.Equal(ws0[1], weights[1], 8);
            Assert.Equal(ws0[2], weights[2], 8);

        }
    }
}
