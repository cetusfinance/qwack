``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Xeon CPU E3-1505M v5 2.80GHz, ProcessorCount=8
.NET Core SDK=2.0.2-vspre-006949
  [Host]     : .NET Core ? (Framework 4.6.00001.0), 64bit RyuJIT
  Job-YEOZOW : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT

Platform=X64  Runtime=Core  InvocationCount=10  
LaunchCount=2  TargetCount=3  UnrollFactor=5  
WarmupCount=1  

```
 | Method | NumberOfDimensions | NumberOfPaths |          Mean |       Error |      StdDev | Scaled |    Gen 0 |    Gen 1 |    Gen 2 |    Allocated |
 |------- |------------------- |-------------- |--------------:|------------:|------------:|-------:|---------:|---------:|---------:|-------------:|
 |  **Basic** |                  **5** |           **256** |      **40.92 us** |    **33.83 us** |    **12.07 us** |   **1.00** |        **-** |        **-** |        **-** |      **2.96 KB** |
 |  **Basic** |                  **5** |         **65536** |   **4,726.74 us** | **1,540.40 us** |   **549.34 us** |   **1.00** |        **-** |        **-** |        **-** |    **512.99 KB** |
 |  **Basic** |                **200** |           **256** |     **399.96 us** |   **192.97 us** |    **68.82 us** |   **1.00** |        **-** |        **-** |        **-** |     **34.95 KB** |
 |  **Basic** |                **200** |         **65536** | **108,509.36 us** | **7,002.77 us** | **2,497.33 us** |   **1.00** | **633.3333** | **633.3333** | **633.3333** | **102947.04 KB** |
