``` ini

BenchmarkDotNet=v0.10.8, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i5-6200U CPU 2.30GHz (Skylake), ProcessorCount=4
Frequency=2343751 Hz, Resolution=426.6665 ns, Timer=TSC
dotnet cli version=1.0.4
  [Host] : .NET Core 4.6.25211.01, 64bit RyuJITDEBUG [AttachedDebugger]

Platform=X64  Runtime=Core  InvocationCount=10  
LaunchCount=2  TargetCount=3  UnrollFactor=5  
WarmupCount=1  

```
 |                 Method | Mean | Error | Scaled | ScaledSD | Allocated |
 |----------------------- |-----:|------:|-------:|---------:|----------:|
 |      InitialOisAttempt |   NA |    NA |      ? |        ? |       N/A |
 |                 Staged |   NA |    NA |      ? |        ? |       N/A |
 | StagedAnalyticJacobian |   NA |    NA |      ? |        ? |       N/A |

Benchmarks with issues:
  SolvingOisBenchmark.InitialOisAttempt: Job-SRRUQP(Platform=X64, Runtime=Core, InvocationCount=10, LaunchCount=2, TargetCount=3, UnrollFactor=5, WarmupCount=1)
  SolvingOisBenchmark.Staged: Job-SRRUQP(Platform=X64, Runtime=Core, InvocationCount=10, LaunchCount=2, TargetCount=3, UnrollFactor=5, WarmupCount=1)
  SolvingOisBenchmark.StagedAnalyticJacobian: Job-SRRUQP(Platform=X64, Runtime=Core, InvocationCount=10, LaunchCount=2, TargetCount=3, UnrollFactor=5, WarmupCount=1)
