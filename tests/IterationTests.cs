using System;
using System.Diagnostics;
using Unmanaged;

namespace Unmanaged
{
    public class BenchmarkTests : UnmanagedTests
    {
        private const uint Scale = 5931428;
        private const double Attempts = 8;

        [Test]
        public void SignedForwardLoop()
        {
            using Allocation allocation = new(Scale);
            USpan<byte> allocationBytes = allocation.AsSpan(0, Scale);
            allocationBytes.Fill(1);
            long ms = 0;
            long min = long.MaxValue;
            long max = long.MinValue;
            ulong sum = 0;
            for (int a = 0; a < Attempts; a++)
            {
                sum = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < Scale; i++)
                {
                    byte x = allocation[(uint)i];
                    sum += x;
                }

                stopwatch.Stop();
                Assert.That(sum, Is.EqualTo(Scale));
                ms += stopwatch.ElapsedMilliseconds;
                min = Math.Min(min, stopwatch.ElapsedMilliseconds);
                max = Math.Max(max, stopwatch.ElapsedMilliseconds);
            }

            Console.WriteLine($"{ms / Attempts}");
            Console.WriteLine(min);
            Console.WriteLine(max);
        }

        [Test]
        public void SignedReverseLoop()
        {
            using Allocation allocation = new(Scale);
            USpan<byte> allocationBytes = allocation.AsSpan(0, Scale);
            allocationBytes.Fill(1);
            long ms = 0;
            long min = long.MaxValue;
            long max = long.MinValue;
            ulong sum = 0;
            for (int a = 0; a < Attempts; a++)
            {
                sum = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int i = (int)(Scale - 1); i >= 0; i--)
                {
                    byte x = allocation[(uint)i];
                    sum += x;
                }

                stopwatch.Stop();
                Assert.That(sum, Is.EqualTo(Scale));
                ms += stopwatch.ElapsedMilliseconds;
                min = Math.Min(min, stopwatch.ElapsedMilliseconds);
                max = Math.Max(max, stopwatch.ElapsedMilliseconds);
            }

            Console.WriteLine($"{ms / Attempts}");
            Console.WriteLine(min);
            Console.WriteLine(max);
        }

        [Test]
        public void UnsignedForwardLoop()
        {
            using Allocation allocation = new(Scale);
            USpan<byte> allocationBytes = allocation.AsSpan(0, Scale);
            allocationBytes.Fill(1);
            long ms = 0;
            long min = long.MaxValue;
            long max = long.MinValue;
            ulong sum = 0;
            for (int a = 0; a < Attempts; a++)
            {
                sum = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                for (uint i = 0; i < Scale; i++)
                {
                    byte x = allocation[i];
                    sum += x;
                }

                stopwatch.Stop();
                Assert.That(sum, Is.EqualTo(Scale));
                ms += stopwatch.ElapsedMilliseconds;
                min = Math.Min(min, stopwatch.ElapsedMilliseconds);
                max = Math.Max(max, stopwatch.ElapsedMilliseconds);
            }

            Console.WriteLine($"{ms / Attempts}");
            Console.WriteLine(min);
            Console.WriteLine(max);
        }

        [Test]
        public void UnsignedReverseLoop()
        {
            using Allocation allocation = new(Scale);
            USpan<byte> allocationBytes = allocation.AsSpan(0, Scale);
            allocationBytes.Fill(1);
            long ms = 0;
            long min = long.MaxValue;
            long max = long.MinValue;
            ulong sum = 0;
            for (int a = 0; a < Attempts; a++)
            {
                sum = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                for (uint i = Scale - 1; i != uint.MaxValue; i--)
                {
                    byte x = allocation[i];
                    sum += x;
                }

                stopwatch.Stop();
                Assert.That(sum, Is.EqualTo(Scale));
                ms += stopwatch.ElapsedMilliseconds;
                min = Math.Min(min, stopwatch.ElapsedMilliseconds);
                max = Math.Max(max, stopwatch.ElapsedMilliseconds);
            }

            Console.WriteLine($"{ms / Attempts}");
            Console.WriteLine(min);
            Console.WriteLine(max);
        }

        [Test]
        public void UnsignedReverseLoopClever()
        {
            using Allocation allocation = new(Scale);
            USpan<byte> allocationBytes = allocation.AsSpan(0, Scale);
            allocationBytes.Fill(1);
            long ms = 0;
            long min = long.MaxValue;
            long max = long.MinValue;
            ulong sum = 0;
            for (int a = 0; a < Attempts; a++)
            {
                sum = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                for (uint i = Scale; i-- > 0;)
                {
                    byte x = allocation[i];
                    sum += x;
                }

                stopwatch.Stop();
                Assert.That(sum, Is.EqualTo(Scale));
                ms += stopwatch.ElapsedMilliseconds;
                min = Math.Min(min, stopwatch.ElapsedMilliseconds);
                max = Math.Max(max, stopwatch.ElapsedMilliseconds);
            }

            Console.WriteLine($"{ms / Attempts}");
            Console.WriteLine(min);
            Console.WriteLine(max);
        }

        [Test]
        public void UnsignedReverseWhileLoop()
        {
            using Allocation allocation = new(Scale);
            USpan<byte> allocationBytes = allocation.AsSpan(0, Scale);
            allocationBytes.Fill(1);
            long ms = 0;
            long min = long.MaxValue;
            long max = long.MinValue;
            ulong sum = 0;
            uint i;
            for (int a = 0; a < Attempts; a++)
            {
                i = Scale;
                sum = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (i != 0)
                {
                    i--;
                    byte x = allocation[i];
                    sum += x;
                }

                stopwatch.Stop();
                Assert.That(sum, Is.EqualTo(Scale));
                ms += stopwatch.ElapsedMilliseconds;
                min = Math.Min(min, stopwatch.ElapsedMilliseconds);
                max = Math.Max(max, stopwatch.ElapsedMilliseconds);
            }

            Console.WriteLine($"{ms / Attempts}");
            Console.WriteLine(min);
            Console.WriteLine(max);
        }
    }
}
