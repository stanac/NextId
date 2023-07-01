using BenchmarkDotNet.Running;

namespace NextId.Performance;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<Benchmarks>();
    }
}