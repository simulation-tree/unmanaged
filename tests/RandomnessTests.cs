using System;
using System.Diagnostics;
using Unmanaged;

namespace Tests
{
    public class RandomnessTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
        }

        [Test]
        public void UniformDistribution()
        {
            RandomGenerator rng = new(1337);
            const int iterations = 1000000;
            float min = 1f;
            float max = 0f;
            float avg = 0f;
            for (int i = 0; i < iterations; i++)
            {
                float value = rng.NextFloat();
                min = Math.Min(min, value);
                max = Math.Max(max, value);
                avg += value;
            }

            avg /= iterations;
            Assert.That(min, Is.LessThan(0.1f));
            Assert.That(max, Is.GreaterThan(0.9f));
            Assert.That(avg, Is.EqualTo(0.5f).Within(0.05f));
            rng.Dispose();
        }

        [Test]
        public void UniformDistributionDouble()
        {
            RandomGenerator rng = new(1337);
            const int iterations = 1000000;
            double min = 1d;
            double max = 0d;
            double avg = 0d;
            for (int i = 0; i < iterations; i++)
            {
                double value = rng.NextDouble();
                min = Math.Min(min, value);
                max = Math.Max(max, value);
                avg += value;
            }

            avg /= iterations;
            Assert.That(min, Is.LessThan(0.1d));
            Assert.That(max, Is.GreaterThan(0.9d));
            Assert.That(avg, Is.EqualTo(0.5d).Within(0.05d));
            rng.Dispose();
        }

        [Test]
        public void DistributionOfInitialSeed()
        {
            const int iterations = 100000;
            float min = 1f;
            float max = 0f;
            float avg = 0f;
            for (int i = 0; i < iterations; i++)
            {
                RandomGenerator rng = new();
                float value = rng.NextFloat();
                min = Math.Min(min, value);
                max = Math.Max(max, value);
                avg += value;
                rng.Dispose();
            }

            avg /= iterations;
            Assert.That(min, Is.LessThan(0.1f));
            Assert.That(max, Is.GreaterThan(0.9f));
            Assert.That(avg, Is.EqualTo(0.5f).Within(0.05f));
        }

        [Test]
        public void BenchmarkAgainstSystemRandom()
        {
            const int iterations = 1000000;
            float min = 1f;
            float max = 0f;
            float avg = 0f;
            Stopwatch stopwatch = Stopwatch.StartNew();
            Random systemRandom = new(1337);
            for (int i = 0; i < iterations; i++)
            {
                float value = (float)systemRandom.NextDouble();
                min = Math.Min(min, value);
                max = Math.Max(max, value);
                avg += value;
            }

            stopwatch.Stop();
            Console.WriteLine($"System.Random: {stopwatch.ElapsedMilliseconds}ms");

            min = 1f;
            max = 0f;
            avg = 0f;
            stopwatch.Restart();
            RandomGenerator rng = new(1337);
            for (int i = 0; i < iterations; i++)
            {
                float value = rng.NextFloat();
                min = Math.Min(min, value);
                max = Math.Max(max, value);
                avg += value;
            }

            stopwatch.Stop();
            Console.WriteLine($"RandomGenerator: {stopwatch.ElapsedMilliseconds}ms");
            rng.Dispose();
        }
    }
}
