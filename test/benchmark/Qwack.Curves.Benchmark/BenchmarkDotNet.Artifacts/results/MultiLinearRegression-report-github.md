``` ini

BenchmarkDotNet=v0.10.6, OS=Windows 10.0.16199
Processor=Intel Xeon CPU E3-1505M v5 2.80GHz, ProcessorCount=8
Frequency=2742186 Hz, Resolution=364.6726 ns, Timer=TSC
dotnet cli version=2.0.0-preview1-005977
  [Host]     : .NET Core 4.6.25009.03, 64bit RyuJIT
  Job-OHDERR : .NET Core 4.6.25211.01, 64bit RyuJIT

Platform=X64  Runtime=Core  IterationTime=2.0000 s  
LaunchCount=2  TargetCount=3  WarmupCount=1  

```
 |                        Method | NumberOfExamples | Dimensions |     Mean |    Error |   StdDev |      Gen 0 |     Gen 1 |     Gen 2 | Allocated |
 |------------------------------ |----------------- |----------- |---------:|---------:|---------:|-----------:|----------:|----------:|----------:|
 | TwoDimensionCombinedTPReflect |            10000 |         50 | 239.5 ms | 10.91 ms | 3.891 ms | 12375.0000 | 6437.5000 | 1916.6667 |   1.35 KB |
