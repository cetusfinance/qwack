using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Qwack.Curves.Benchmark
{
    [Config(typeof(QuickSpinConfig))]
    public class InterpolationBenchmark
    {
        public const int Interpolations = 5000;

        private static double[] _x;
        private static double[] _y;
        private static double[] _guesses;

        [Params(500)]
        public static int NumberOfPillars { get; set; }

        [Setup]
        public static void Setup()
        {
            var rnd = new Random(7777);
            _x = new double[NumberOfPillars];
            _y = new double[NumberOfPillars];
            var step = 1.0 / NumberOfPillars;
            for (int i = 0; i < NumberOfPillars; i++)
            {
                _x[i] = i * step;
                _y[i] = rnd.NextDouble();
            }
            rnd = new Random(99999);
            _guesses = new double[Interpolations];
            for (int i = 0; i < Interpolations; i++)
            {
                _guesses[i] = rnd.NextDouble();
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Interpolations)]
        public static void UsingBinarySearch()
        {
            var interp = new Math.Interpolation.LinearInterpolatorFlatExtrap(_x, _y);
            var g = _guesses;
            for (int i = 0; i < Interpolations; i++)
            {
                var interpValue = interp.Interpolate(g[i]);
            }
        }

        [Benchmark(OperationsPerInvoke = Interpolations,Baseline =true)]
        public static void SimpleLoop()
        {
            var interp = new Math.Interpolation.LinearInterpolatorFlatExtrapNoBinSearch(_x, _y);
            var g = _guesses;
            for (int i = 0; i < Interpolations; i++)
            {
                var interpValue = interp.Interpolate(g[i]);
            }
        }
    }
}
