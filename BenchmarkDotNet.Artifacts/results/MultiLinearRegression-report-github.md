``` ini

BenchmarkDotNet=v0.10.3.18-nightly, OS=Microsoft Windows 10.0.15048
Processor=Intel(R) Xeon(R) CPU E3-1505M v5 2.80GHz, ProcessorCount=8
Frequency=2742187 Hz, Resolution=364.6724 ns, Timer=TSC
dotnet cli version=1.0.0
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  Job-EFMOJF : .NET Core 4.6.25009.03, 64bit RyuJIT

Platform=X64  Runtime=Core  IterationTime=2.0000 s  
LaunchCount=2  TargetCount=3  WarmupCount=1  

```
 |                       Method | NumberOfExamples | Dimensions |        Mean |    StdErr |    StdDev | Scaled | Scaled-StdDev |    Gen 0 | Allocated |
 |----------------------------- |----------------- |----------- |------------ |---------- |---------- |------- |-------------- |--------- |---------- |
 |                **AccordVersion** |             **1000** |         **25** |   **2.9848 ms** | **0.0597 ms** | **0.1462 ms** |   **1.00** |          **0.00** |  **58.1395** | **535.57 kB** |
 | TwoDimensionCominedTPReflect |             1000 |         25 |   2.4303 ms | 0.0022 ms | 0.0055 ms |   0.82 |          0.03 |  47.6763 | 453.95 kB |
 |                **AccordVersion** |             **1000** |         **50** |  **13.4208 ms** | **0.0584 ms** | **0.1429 ms** |   **1.00** |          **0.00** |        **-** | **935.26 kB** |
 | TwoDimensionCominedTPReflect |             1000 |         50 |  11.2484 ms | 0.0308 ms | 0.0755 ms |   0.84 |          0.01 |  15.6250 | 951.45 kB |
 |                **AccordVersion** |            **10000** |         **25** |  **48.7544 ms** | **0.3258 ms** | **0.7981 ms** |   **1.00** |          **0.00** | **180.5556** |   **5.35 MB** |
 | TwoDimensionCominedTPReflect |            10000 |         25 |  23.8610 ms | 0.0919 ms | 0.2251 ms |   0.49 |          0.01 | 645.8333 |    4.2 MB |
 |                **AccordVersion** |            **10000** |         **50** | **201.2647 ms** | **1.5873 ms** | **3.8881 ms** |   **1.00** |          **0.00** |        **-** |   **9.34 MB** |
 | TwoDimensionCominedTPReflect |            10000 |         50 | 114.1702 ms | 0.2509 ms | 0.6147 ms |   0.57 |          0.01 | 125.0000 |    8.3 MB |
