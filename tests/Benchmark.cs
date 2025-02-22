using System;
using System.Diagnostics;

namespace Unmanaged.Tests
{
    public readonly struct Benchmark
    {
        public readonly double elapsed;
        public readonly double average;

        public Benchmark(Action action, uint iterations = 10000)
        {
            for (int w = 0; w < 8; w++)
            {
                action();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Stopwatch stopwatch = new();
            stopwatch.Restart();
            for (uint i = 0; i < iterations; i++)
            {
                action();
            }

            stopwatch.Stop();
            elapsed = stopwatch.ElapsedMilliseconds;
            average = stopwatch.ElapsedMilliseconds / (double)iterations;
        }

        public override string ToString()
        {
            return $"Elapsed: {elapsed}ms, Average: {average}ms";
        }
    }
}