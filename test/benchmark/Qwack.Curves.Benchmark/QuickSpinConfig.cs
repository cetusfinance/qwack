using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace Qwack.Curves.Benchmark
{
    public class QuickSpinConfig : ManualConfig
    {
        public QuickSpinConfig()
        {
            Add(Job.Default.
                With(Platform.X64).
                With(Runtime.Core).
                WithLaunchCount(2).
                WithIterationTime(new BenchmarkDotNet.Horology.TimeInterval(2000, BenchmarkDotNet.Horology.TimeUnit.Millisecond)).
                WithWarmupCount(1).
                WithIterationCount(3));
            Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
        }
    }
}
