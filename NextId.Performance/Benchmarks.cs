﻿using BenchmarkDotNet.Attributes;
using NextId.Tests;

namespace NextId.Performance;

public class Benchmarks
{
    private static readonly string _valueToParse = UserId.NewId().ToString();

    [Benchmark]
    public void NewId_1000()
    {
        for (int i = 0; i < 1000; i++)
        {
            UserId.NewId();
        }
    }

    [Benchmark]
    public void Parse_1000()
    {
        for (int i = 0; i < 1000; i++)
        {
            UserId.Parse(_valueToParse);
        }
    }
}