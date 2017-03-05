``` ini

BenchmarkDotNet=v0.10.3.18-nightly, OS=Microsoft Windows 10.0.15048
Processor=Intel(R) Xeon(R) CPU E3-1505M v5 2.80GHz, ProcessorCount=8
Frequency=2742186 Hz, Resolution=364.6726 ns, Timer=TSC
dotnet cli version=1.0.0-rc4-004771
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  Job-WOEACL : .NET Core 4.6.24628.01, 64bit RyuJIT

Platform=X64  Runtime=Core  InvocationCount=10  
LaunchCount=2  TargetCount=3  UnrollFactor=5  
WarmupCount=1  

```
 |                   Method | NumberOfExamples | Dimensions |        Mean |    StdErr |     StdDev | Scaled | Scaled-StdDev | Allocated |
 |------------------------- |----------------- |----------- |------------ |---------- |----------- |------- |-------------- |---------- |
 |            AccordVersion |            10000 |         50 | 232.6942 ms | 5.1982 ms | 12.7328 ms |   1.00 |          0.00 |   9.34 MB |
 | TwoDimensionFasterBounds |            10000 |         50 | 252.1211 ms | 2.8981 ms |  7.0989 ms |   1.09 |          0.06 |  12.69 MB |
