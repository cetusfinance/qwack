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
                With(Jit.RyuJit).
                With(Runtime.Clr).
                WithLaunchCount(2).
                WithIterationTime(new BenchmarkDotNet.Horology.TimeInterval(1000, BenchmarkDotNet.Horology.TimeUnit.Millisecond)).
                WithWarmupCount(1).
                WithTargetCount(3));
//#if NET461
//            Add(new BenchmarkDotNet.Diagnostics.Windows.MemoryDiagnoser());
//#endif
        }
    }
}