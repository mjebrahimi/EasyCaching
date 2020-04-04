﻿using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaScript.Benchmark
{

    class Program
    {
       
        static void Main(string[] args)
        {
#if DEBUG
            System.Console.ForegroundColor = System.ConsoleColor.Yellow;
            System.Console.WriteLine("*****To achieve accurate results, set project configuration to Release mode.*****");
            return;
#endif
            BenchmarkRunner.Run<GetCountBenchmark>();
        }
    }
}
