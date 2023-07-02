using BenchmarkDotNet.Running;

namespace NextId.Performance;

/*

866,551 generated ids per second per core
369,822 parsed ids per second per core

BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19045.3086/22H2/2022Update)
AMD Ryzen 7 2700X, 1 CPU, 16 logical and 8 physical cores
.NET SDK=7.0.304
  [Host]     : .NET 7.0.7 (7.0.723.27404), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.7 (7.0.723.27404), X64 RyuJIT AVX2


|     Method |     Mean |     Error |    StdDev |
|----------- |---------:|----------:|----------:|
| NewId_1000 | 1.154 ms | 0.0054 ms | 0.0050 ms |
| Parse_1000 | 2.704 ms | 0.0309 ms | 0.0258 ms |

 */

internal class Program
{
    static void Main(string[] args)
    { 
        BenchmarkRunner.Run<Benchmarks>();
    }
}