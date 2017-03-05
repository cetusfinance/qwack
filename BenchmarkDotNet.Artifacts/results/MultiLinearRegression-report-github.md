``` ini

BenchmarkDotNet=v0.10.2.368-develop, OS=Microsoft Windows 10.0.15046
Processor=Intel(R) Xeon(R) CPU E3-1505M v5 2.80GHz, ProcessorCount=8
Frequency=2742185 Hz, Resolution=364.6727 ns, Timer=TSC
dotnet cli version=1.0.0-rc4-004771
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  Job-MBKOSQ : .NET Core 4.6.24628.01, 64bit RyuJIT

Platform=X64  Runtime=Core  IterationTime=1.0000 s  
LaunchCount=2  TargetCount=3  WarmupCount=1  

```
                        Method | NumberOfExamples |        Mean |    StdDev | Scaled | Scaled-StdDev |    Gen 0 | Allocated |
------------------------------ |----------------- |------------ |---------- |------- |-------------- |--------- |---------- |
                TwoDimensionNR |             2000 | 709.2094 us | 2.7299 us |   1.00 |          0.00 | 116.6667 | 597.51 kB |
            TwoDimensionFaster |             2000 | 174.8033 us | 0.7083 us |   0.25 |          0.00 |  46.7466 | 225.18 kB |
      TwoDimensionFasterBounds |             2000 | 168.6384 us | 0.7770 us |   0.24 |          0.00 |  47.0395 | 225.18 kB |
 TwoDimensionFasterNoTranspose |             2000 | 269.4526 us | 5.4624 us |   0.38 |          0.01 |  13.7727 | 113.38 kB |
us | 2.5907 us |  6.3460 us |   0.25 |          0.01 | 21.5503 | 113.25 kB |
