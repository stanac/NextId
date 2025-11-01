using BenchmarkDotNet.Attributes;
using NextId.Tests;

namespace NextId.Performance;

public class Benchmarks
{
    private static readonly string ValueToParse = UserId.NewId().ToString();
    private static string _s = "";

    [Benchmark]
    public void NewId_1000()
    {
        for (int i = 0; i < 1000; i++)
        {
            _s = UserId.NewId().Value;
        }
    }

    [Benchmark]
    public void NewId_1000_NumberValue()
    {
        for (int i = 0; i < 1000; i++)
        {
            _s = UserId.NewId().NumberValue;
        }
    }

    [Benchmark]
    public void Parse_1000()
    {
        for (int i = 0; i < 1000; i++)
        {
            UserId.Parse(ValueToParse);
        }
    }
}