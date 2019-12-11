using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Qwack.Random.Sobol;

namespace Qwack.Curves.Benchmark
{
    [Config(typeof(SolveConfig))]
    public class SobolBlockGeneration
    {
        private static SobolDirectionNumbers DirectionNumbers()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SobolDirectionNumbers.txt");
            var dirNumbers = new SobolDirectionNumbers(path);
            return dirNumbers;
        }

        private static readonly SobolDirectionNumbers s_directionNumbers = DirectionNumbers();

        [Params(5, 200)]
        public static int NumberOfDimensions { get; set; }

        [Params(65536)]
        public static int NumberOfPaths { get; set; }

        private const int Iterations = 100;

        [GlobalSetup]
        public void AllocateBuffers()
        {
            RentThenRetrun(5, 65536);
            RentThenRetrun(200, 65536);
        }

        private static void RentThenRetrun(int dim, int paths)
        {
            var buffer = System.Buffers.ArrayPool<double>.Shared.Rent(dim * paths);
            System.Buffers.ArrayPool<double>.Shared.Return(buffer);
        }

        [Benchmark(Baseline =true, OperationsPerInvoke = Iterations)]
        public int Basic()
        {
            var dims = NumberOfDimensions;
            var paths = NumberOfPaths;
            var buffer = System.Buffers.ArrayPool<double>.Shared.Rent(dims * paths);

            var sobolGen = new SobolGenerator(s_directionNumbers)
            {
                Dimensions = dims
            };
            sobolGen.GetPathsRaw(paths, buffer);

            System.Buffers.ArrayPool<double>.Shared.Return(buffer);
            return dims * paths;
        }
    }
}
