``` ini

BenchmarkDotNet=v0.10.3.18-nightly, OS=Microsoft Windows 10.0.15048
Processor=Intel(R) Xeon(R) CPU E3-1505M v5 2.80GHz, ProcessorCount=8
Frequency=2742187 Hz, Resolution=364.6724 ns, Timer=TSC
dotnet cli version=1.0.0
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  Job-CKVEIM : .NET Core 4.6.25009.03, 64bit RyuJIT

Platform=X64  Runtime=Core  IterationTime=2.0000 s  
LaunchCount=2  TargetCount=3  WarmupCount=1  

```
 |                             Method | NumberOfExamples | Dimensions |        Mean |    StdErr |     StdDev | Scaled | Scaled-StdDev |    Gen 0 | Allocated |
 |----------------------------------- |----------------- |----------- |------------ |---------- |----------- |------- |-------------- |--------- |---------- |
 |                      **AccordVersion** |             **1000** |         **25** |   **2.3145 ms** | **0.0297 ms** |  **0.0729 ms** |   **1.00** |          **0.00** |  **60.2679** | **535.57 kB** |
 | TwoDimensionCominedTransposeProuct |             1000 |         25 |   2.7588 ms | 0.0821 ms |  0.2011 ms |   1.19 |          0.09 |  40.7986 | 453.94 kB |
 |                 TwoDimensionFaster |             1000 |         25 |   4.3808 ms | 0.1153 ms |  0.2825 ms |   1.89 |          0.12 |  72.9167 | 693.18 kB |
 |       TwoDimensionCominedTPReflect |             1000 |         25 |   2.2436 ms | 0.0351 ms |  0.0859 ms |   0.97 |          0.04 |  47.0679 | 453.94 kB |
 |                      **AccordVersion** |             **1000** |         **50** |  **10.4667 ms** | **0.1126 ms** |  **0.2758 ms** |   **1.00** |          **0.00** |   **4.8077** | **935.22 kB** |
 | TwoDimensionCominedTransposeProuct |             1000 |         50 |  12.0723 ms | 0.1991 ms |  0.4876 ms |   1.15 |          0.05 |        - | 951.43 kB |
 |                 TwoDimensionFaster |             1000 |         50 |  17.3439 ms | 0.2293 ms |  0.5616 ms |   1.66 |          0.06 |        - |   1.39 MB |
 |       TwoDimensionCominedTPReflect |             1000 |         50 |  10.1567 ms | 0.2434 ms |  0.5962 ms |   0.97 |          0.06 |  24.0385 | 951.42 kB |
 |                      **AccordVersion** |            **10000** |         **25** |  **38.2165 ms** | **0.4834 ms** |  **1.1842 ms** |   **1.00** |          **0.00** | **286.4583** |   **5.35 MB** |
 | TwoDimensionCominedTransposeProuct |            10000 |         25 |  26.0008 ms | 0.4047 ms |  0.9913 ms |   0.68 |          0.03 | 558.3333 |    4.2 MB |
 |                 TwoDimensionFaster |            10000 |         25 |  46.9991 ms | 0.6387 ms |  1.5644 ms |   1.23 |          0.05 | 229.1667 |   6.59 MB |
 |       TwoDimensionCominedTPReflect |            10000 |         25 |  20.8908 ms | 0.1510 ms |  0.3699 ms |   0.55 |          0.02 | 672.6190 |    4.2 MB |
 |                      **AccordVersion** |            **10000** |         **50** | **147.8834 ms** | **1.3918 ms** |  **3.4092 ms** |   **1.00** |          **0.00** |        **-** |   **9.34 MB** |
 | TwoDimensionCominedTransposeProuct |            10000 |         50 | 118.2047 ms | 1.8019 ms |  4.4136 ms |   0.80 |          0.03 |        - |    8.3 MB |
 |                 TwoDimensionFaster |            10000 |         50 | 212.2709 ms | 5.9176 ms | 14.4951 ms |   1.44 |          0.09 |        - |  12.69 MB |
 |       TwoDimensionCominedTPReflect |            10000 |         50 | 101.4592 ms | 1.1766 ms |  2.8821 ms |   0.69 |          0.02 | 375.0000 |    8.3 MB |
