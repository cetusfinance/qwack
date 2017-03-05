``` ini

BenchmarkDotNet=v0.10.2.368-develop, OS=Microsoft Windows 10.0.15046
Processor=Intel(R) Xeon(R) CPU E3-1505M v5 2.80GHz, ProcessorCount=8
Frequency=2742185 Hz, Resolution=364.6727 ns, Timer=TSC
dotnet cli version=1.0.0-rc4-004771
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  Job-MCMETY : .NET Core 4.6.24628.01, 64bit RyuJIT

Platform=X64  Runtime=Core  IterationTime=1.0000 s  
LaunchCount=2  TargetCount=3  WarmupCount=1  

```
                        Method | NumberOfExamples |        Mean |    StdDev | Scaled | Scaled-StdDev |    Gen 0 | Allocated |
------------------------------ |----------------- |------------ |---------- |------- |-------------- |--------- |---------- |
                TwoDimensionNR |             2000 | 714.7977 us | 4.7893 us |   1.00 |          0.00 | 115.6367 | 597.51 kB |
            TwoDimensionFaster |             2000 | 175.1510 us | 0.8615 us |   0.25 |          0.00 |  46.1931 | 225.18 kB |
 TwoDimensionFasterNoTranspose |             2000 | 261.0814 us | 5.6589 us |   0.37 |          0.01 |  14.6350 | 113.38 kB |
.8855 us** |   **1.00** |          **0.00** | **48.8154** | **301.91 kB** |
 TwoDimensionFaster |             1000 | 129.3885 us | 2.5907 us |  6.3460 us |   0.25 |          0.01 | 21.5503 | 113.25 kB |
