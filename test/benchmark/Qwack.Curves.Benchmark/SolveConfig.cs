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
                With(Jit.RyuJit).
                WithInvocationCount(5).
                With(Runtime.Clr).
                WithLaunchCount(2).
                WithWarmupCount(1).
                WithUnrollFactor(5).
                WithTargetCount(3));
#if NET461
            Add(new BenchmarkDotNet.Diagnostics.Windows.MemoryDiagnoser());
#endif
        }
    }
}
