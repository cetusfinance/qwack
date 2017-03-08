``` ini

BenchmarkDotNet=v0.10.3.18-nightly, OS=Microsoft Windows 10.0.15048
Processor=Intel(R) Xeon(R) CPU E3-1505M v5 2.80GHz, ProcessorCount=8
Frequency=2742187 Hz, Resolution=364.6724 ns, Timer=TSC
dotnet cli version=1.0.0
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  Job-LEABSF : .NET Core 4.6.25009.03, 64bit RyuJIT

Platform=X64  Runtime=Core  IterationTime=2.0000 s  
LaunchCount=2  TargetCount=3  WarmupCount=1  

```
 |                   Method | NumberOfExamples | Dimensions |        Mean |    StdErr |     StdDev | Scaled | Scaled-StdDev |    Gen 0 | Allocated |
 |------------------------- |----------------- |----------- |------------ |---------- |----------- |------- |-------------- |--------- |---------- |
 |            **AccordVersion** |             **1000** |         **25** |   **2.3686 ms** | **0.0650 ms** |  **0.1593 ms** |   **1.00** |          **0.00** |  **63.9124** | **535.57 kB** |
 | TwoDimensionFasterBounds |             1000 |         25 |   2.5240 ms | 0.0040 ms |  0.0098 ms |   1.07 |          0.07 |  45.8333 | 453.94 kB |
 |       TwoDimensionFaster |             1000 |         25 |   4.0010 ms | 0.0046 ms |  0.0112 ms |   1.70 |          0.10 |  74.2188 | 693.17 kB |
 |            **AccordVersion** |             **1000** |         **50** |   **9.8195 ms** | **0.0534 ms** |  **0.1308 ms** |   **1.00** |          **0.00** |   **4.8077** | **935.22 kB** |
 | TwoDimensionFasterBounds |             1000 |         50 |  11.2220 ms | 0.0262 ms |  0.0643 ms |   1.14 |          0.02 |        - | 951.43 kB |
 |       TwoDimensionFaster |             1000 |         50 |  16.5285 ms | 0.1091 ms |  0.2672 ms |   1.68 |          0.03 |  15.6250 |   1.39 MB |
 |            **AccordVersion** |            **10000** |         **25** |  **35.9511 ms** | **0.1578 ms** |  **0.3864 ms** |   **1.00** |          **0.00** | **390.6250** |   **5.35 MB** |
 | TwoDimensionFasterBounds |            10000 |         25 |  24.9792 ms | 0.0469 ms |  0.1150 ms |   0.69 |          0.01 | 575.0000 |    4.2 MB |
 |       TwoDimensionFaster |            10000 |         25 |  44.6026 ms | 0.6275 ms |  1.5371 ms |   1.24 |          0.04 | 256.9444 |   6.59 MB |
 |            **AccordVersion** |            **10000** |         **50** | **143.9647 ms** | **2.8979 ms** |  **7.0985 ms** |   **1.00** |          **0.00** |        **-** |   **9.34 MB** |
 | TwoDimensionFasterBounds |            10000 |         50 | 107.6398 ms | 0.5709 ms |  1.3985 ms |   0.75 |          0.03 | 166.6667 |    8.3 MB |
 |       TwoDimensionFaster |            10000 |         50 | 182.2976 ms | 4.8567 ms | 11.8964 ms |   1.27 |          0.09 |        - |  12.69 MB |
