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

        private double[] _x;
        private double[] _y;
        private double[] _guesses;

        [Params(500)]
        public static int NumberOfPillars { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var rnd = new System.Random(7777);
            _x = new double[NumberOfPillars];
            _y = new double[NumberOfPillars];
            var step = 1.0 / NumberOfPillars;
            for (var i = 0; i < NumberOfPillars; i++)
            {
                _x[i] = i * step;
                _y[i] = rnd.NextDouble();
            }
            rnd = new System.Random(99999);
            _guesses = new double[Interpolations];
            for (var i = 0; i < Interpolations; i++)
            {
                _guesses[i] = rnd.NextDouble();
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Interpolations)]
        public void UsingBinarySearch()
        {
            var interp = new Math.Interpolation.LinearInterpolatorFlatExtrap(_x, _y);
            var g = _guesses;
            for (var i = 0; i < Interpolations; i++)
            {
                var interpValue = interp.Interpolate(g[i]);
            }
        }

        [Benchmark(OperationsPerInvoke = Interpolations)]
        public void SimpleLoop()
        {
            var interp = new Math.Interpolation.LinearInterpolatorFlatExtrapNoBinSearch(_x, _y);
            var g = _guesses;
            for (var i = 0; i < Interpolations; i++)
            {
                var interpValue = interp.Interpolate(g[i]);
            }
        }
    }
}
