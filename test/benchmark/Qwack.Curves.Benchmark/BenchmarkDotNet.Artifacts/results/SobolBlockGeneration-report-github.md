``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Xeon CPU E3-1505M v5 2.80GHz, ProcessorCount=8
.NET Core SDK=2.0.2-vspre-006949
  [Host]     : .NET Core ? (Framework 4.6.00001.0), 64bit RyuJIT
  Job-RLTYUY : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT

Platform=X64  Runtime=Core  InvocationCount=10  
LaunchCount=2  TargetCount=3  UnrollFactor=5  
WarmupCount=1  

```
 | Method | NumberOfDimensions | NumberOfPaths |        Mean |     Error |    StdDev | Scaled |  Gen 0 |  Gen 1 |  Gen 2 |  Allocated |
 |------- |------------------- |-------------- |------------:|----------:|----------:|-------:|-------:|-------:|-------:|-----------:|
 |  **Basic** |                  **5** |         **65536** |    **51.61 us** |  **17.89 us** |  **6.381 us** |   **1.00** | **1.0000** | **1.0000** | **1.0000** |    **5.14 KB** |
 |  **Basic** |                **200** |         **65536** | **1,027.93 us** | **106.30 us** | **37.908 us** |   **1.00** | **7.0000** | **7.0000** | **7.0000** | **1029.45 KB** |
