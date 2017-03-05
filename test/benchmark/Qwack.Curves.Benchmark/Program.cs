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
        private static readonly Dictionary<string, Type> _benchmarks = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["SolveOis"] = typeof(SolvingOisBenchmark),
            ["LinInterp"] = typeof(InterpolationBenchmark),
            ["VectorWrite"] = typeof(WritingDoubleVectorVsDouble),
            ["MultiRegression"] = typeof(MultiLinearRegression)
        };

        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Running full benchmarks suite");
                _benchmarks.Select(pair => pair.Value).ToList().ForEach(action => BenchmarkRunner.Run(action));
                return;
            }

            if (args.Length == 0 || !_benchmarks.ContainsKey(args[0]))
            {
                Console.WriteLine("Please, select benchmark, list of available:");
                _benchmarks
                    .Select(pair => pair.Key)
                    .ToList()
                    .ForEach(Console.WriteLine);
                Console.WriteLine("All");
                return;
            }

            BenchmarkRunner.Run(_benchmarks[args[0]]);

            Console.Read();
        }
    }
}
