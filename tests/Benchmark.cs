using System;
using System.Diagnostics;

namespace Unmanaged.Tests
{
    public readonly struct Benchmark
    {
        private readonly Action action;

        [Obsolete("Default constructor not supported", true)]
        public Benchmark()
        {
            throw new NotSupportedException();
        }

        public Benchmark(Action action)
        {
            this.action = action;
        }

        public readonly void Run(out double elapsed, out double average, uint iterations = 10000)
        {
            action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

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

        public readonly string Run(uint iterations = 10000)
        {
            Run(out double elapsed, out double average, iterations);
            if (iterations == 0)
            {
                return "Did not run";
            }
            else if (iterations == 1)
            {
                return $"{elapsed:0.00000}ms";
            }
            else
            {
                return $"AVG: {average:0.00000}ms";
            }
        }
    }
}