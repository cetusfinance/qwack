using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace Qwack.Curves.Benchmark
{
    public class SolveConfig : ManualConfig
    {
        public SolveConfig()
        {
            Add(Job.Default.
                With(Platform.X64).
                With(CoreRuntime.Core21).
                WithInvocationCount(10).
                WithLaunchCount(2).
                WithWarmupCount(1).
                WithUnrollFactor(5).
                WithIterationCount(3));
            Add(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
            
        }
    }
}
