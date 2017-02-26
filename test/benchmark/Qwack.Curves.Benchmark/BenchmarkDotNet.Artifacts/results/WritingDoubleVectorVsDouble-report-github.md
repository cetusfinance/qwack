``` ini

BenchmarkDotNet=v0.10.2.368-develop, OS=Microsoft Windows 10.0.15031
Processor=Intel(R) Xeon(R) CPU E3-1505M v5 2.80GHz, ProcessorCount=8
Frequency=2742186 Hz, Resolution=364.6726 ns, Timer=TSC
dotnet cli version=1.0.0-rc4-004771
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  Job-FSQEQJ : .NET Core 4.6.24628.01, 64bit RyuJIT

Platform=X64  Runtime=Core  IterationTime=1.0000 s  
LaunchCount=2  TargetCount=3  WarmupCount=1  

```
                    Method |          Mean |     StdErr |     StdDev | Scaled | Scaled-StdDev |    Gen 0 |   Gen 1 |  Gen 2 | Allocated |
-------------------------- |-------------- |----------- |----------- |------- |-------------- |--------- |-------- |------- |---------- |
        RandsWithNoVectors |   765.5261 us | 14.3940 us | 35.2581 us |   1.00 |          0.00 | 227.4904 | 18.6782 |      - |   1.18 MB |
 RandsWithNoVectorsNormInv | 1,332.5093 us |  4.6792 us | 11.4616 us |   1.74 |          0.08 | 209.2014 |  1.7361 |      - |   1.18 MB |
   RandsWithVectorsNormInv | 1,342.8652 us |  9.0837 us | 22.2505 us |   1.76 |          0.08 | 212.6736 |  5.2083 |      - |   1.18 MB |
          RandsWithVectors |   678.4752 us |  2.5620 us |  6.2755 us |   0.89 |          0.04 | 232.0789 | 23.2975 |      - |   1.18 MB |
      RandsPointersNormInv | 1,101.6310 us |  3.9772 us |  9.7420 us |   1.44 |          0.06 | 216.5948 |  9.3391 |      - |   1.18 MB |
             RandsPointers |   539.7842 us |  2.1797 us |  5.3391 us |   0.71 |          0.03 | 235.3142 | 26.2978 | 3.0738 |   1.18 MB |
