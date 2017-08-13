using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Qwack.Curves.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var types = new Type[]
            {
                typeof(SolvingOisBenchmark),
                typeof(LinearRegression),
                typeof(InterpolationBenchmark),
                typeof(MultiLinearRegression),
                typeof(WritingDoubleVectorVsDouble),
            };
            var switcher = BenchmarkSwitcher.FromTypes(types);
            switcher.Run(args);
        }
    }
}
