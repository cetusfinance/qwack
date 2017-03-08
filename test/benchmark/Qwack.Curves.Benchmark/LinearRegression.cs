using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Qwack.Curves.Benchmark
{
    [Config(typeof(QuickSpinConfig))]
    public class LinearRegression
    {
        public static double[] XValues;
        public static double[] YValues;
        
        public static void Setup()
        {
            int numberOfItems = 200;
            var rnd = new System.Random(7777);
            XValues = new double[numberOfItems];
            YValues = new double[numberOfItems];

            for(int i = 0;i< numberOfItems; i++)
            {
                XValues[i] = rnd.NextDouble();
                YValues[i] = rnd.NextDouble();
            }
        }

        [Benchmark(Baseline = true)]
        public double AccordBaseline()
        {
            return 1.0;
        }

        [Benchmark]
        public double SimpleLinearRegression()
        {
            return Math.LinearRegression.LinearRegressionNoVector(XValues, YValues, false).Beta;
        }

        [Benchmark]
        public double SimpleVectorLinearRegression()
        {
            return Math.LinearRegression.LinearRegressionVector(XValues,YValues).Beta;
        }

    }
}
