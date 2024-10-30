using System;
using System.Diagnostics;

namespace Unmanaged
{
    public class SpanTests : UnmanagedTests
    {
        [Test]
        public void CreatingUsingStackalloc()
        {
            USpan<byte> data = stackalloc byte[8];
            Assert.That(data.Length, Is.EqualTo(8));
            data[0] = 1;
            data[1] = 2;

            Assert.That(data[0], Is.EqualTo(1));
            Assert.That(data[1], Is.EqualTo(2));
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void AccessingSpanOutOfRange()
        {
            USpan<byte> data = stackalloc byte[8];
            try
            {
                data[8] = 0;
                Assert.Fail();
            }
            catch
            {
            }
        }

        [Test]
        public void Slicing()
        {
            USpan<byte> data = stackalloc byte[8];
            Span<byte> referenceData = stackalloc byte[8];
            for (uint i = 0; i < 8; i++)
            {
                data[i] = (byte)i;
                referenceData[(int)i] = (byte)i;
            }

            USpan<byte> slice = data.Slice(2, 4);
            Span<byte> referenceSlice = referenceData.Slice(2, 4);

            Assert.That(slice.Length, Is.EqualTo(referenceSlice.Length));
            Assert.That(slice.ToArray(), Is.EqualTo(referenceSlice.ToArray()));

            using RandomGenerator rng = new();
            for (uint i = 0; i < 32; i++)
            {
                uint length = rng.NextUInt(8, 16);
                uint sliceLength = rng.NextUInt(1, 4);
                uint sliceStart = rng.NextUInt(0, length - sliceLength);
                TestRandomSlice(length, sliceStart, sliceLength);
            }

            static void TestRandomSlice(uint length, uint sliceStart, uint sliceLength)
            {
                USpan<byte> data = stackalloc byte[(int)length];
                Span<byte> referenceData = stackalloc byte[(int)length];
                for (uint i = 0; i < length; i++)
                {
                    data[i] = (byte)i;
                    referenceData[(int)i] = (byte)i;
                }

                USpan<byte> slice = data.Slice(sliceStart, sliceLength);
                Span<byte> referenceSlice = referenceData.Slice((int)sliceStart, (int)sliceLength);
                Assert.That(slice.Length, Is.EqualTo(referenceSlice.Length));
                Assert.That(slice.ToArray(), Is.EqualTo(referenceSlice.ToArray()));
            }
        }

        [Test]
        public void SliceText()
        {
            USpan<char> text = ['H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 'd'];
            USpan<char> slice = text.Slice(0, 5);
            Assert.That(slice.Length, Is.EqualTo(5));
            Assert.That(slice[0], Is.EqualTo('H'));
            Assert.That(slice[1], Is.EqualTo('e'));
            Assert.That(slice[2], Is.EqualTo('l'));
            Assert.That(slice[3], Is.EqualTo('l'));
            Assert.That(slice[4], Is.EqualTo('o'));

            slice = text.Slice(2, 2);
            Assert.That(slice.Length, Is.EqualTo(2));
            Assert.That(slice[0], Is.EqualTo('l'));
            Assert.That(slice[1], Is.EqualTo('l'));
        }

        [Test]
        public void SliceOutOfRange()
        {
            Span<byte> data = stackalloc byte[8];
            try
            {
                Span<byte> slice = data.Slice(8, 1);
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch
            {
                Assert.Fail();
            }

            USpan<byte> uData = stackalloc byte[8];
            try
            {
                USpan<byte> slice = uData.Slice(8, 1);
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch
            {
                Assert.Fail();
            }
        }

        [Test]
        public void FillingAndClearing()
        {
            USpan<byte> data = stackalloc byte[8];
            for (uint i = 0; i < 8; i++)
            {
                Assert.That(data[i], Is.EqualTo(0));
            }

            data.Fill(1);
            for (uint i = 0; i < 8; i++)
            {
                Assert.That(data[i], Is.EqualTo(1));
            }

            data.Clear();
            for (uint i = 0; i < 8; i++)
            {
                Assert.That(data[i], Is.EqualTo(0));
            }
        }

        [Test]
        public void CopyIntoAnotherSpan()
        {
            USpan<byte> data = stackalloc byte[8];
            data[0] = 1;
            data[1] = 2;
            USpan<byte> otherData = stackalloc byte[8];
            data.CopyTo(otherData);

            Assert.That(otherData[0], Is.EqualTo(1));
            Assert.That(otherData[1], Is.EqualTo(2));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                USpan<byte> data = stackalloc byte[8];
                data[0] = 1;
                data[1] = 2;
                USpan<byte> lessData = stackalloc byte[1];
                data.CopyTo(lessData);
            });
        }

        [Test]
        public void CreateFromCollectionExpression()
        {
            USpan<byte> data = [1, 2, 3, 4, 5];
            Assert.That(data.Length, Is.EqualTo(5));
            Assert.That(data[0], Is.EqualTo(1));
            Assert.That(data[1], Is.EqualTo(2));
            Assert.That(data[2], Is.EqualTo(3));
            Assert.That(data[3], Is.EqualTo(4));
            Assert.That(data[4], Is.EqualTo(5));
        }

        [Test]
        public void ToStringWithChars()
        {
            USpan<char> text = ['H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 'd'];
            Assert.That(text.ToString(), Is.EqualTo("Hello World"));
        }

        [Test]
        public void BenchmarkAgainstSystemSpan()
        {
            //indexof, contains, fill, clear, slice
            const int length = 1024;
            Span<byte> systemSpan = new byte[length];
            USpan<byte> unmanagedSpan = stackalloc byte[length];
            for (int i = 0; i < length; i++)
            {
                systemSpan[i] = (byte)(i % 255);
                unmanagedSpan[(uint)i] = (byte)(i % 255);
            }

            Stopwatch benchmark = new();
            benchmark.Start();
            for (int i = 0; i < 1000000; i++)
            {
                systemSpan.IndexOf((byte)0);
            }

            benchmark.Stop();
            Console.WriteLine($"System.Span.IndexOf: {benchmark.ElapsedMilliseconds}ms");

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                unmanagedSpan.IndexOf((byte)0);
            }

            benchmark.Stop();
            Console.WriteLine($"USpan.IndexOf: {benchmark.ElapsedMilliseconds}ms");

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                systemSpan.Contains((byte)0);
            }

            benchmark.Stop();
            Console.WriteLine($"System.Span.Contains: {benchmark.ElapsedMilliseconds}ms");

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                unmanagedSpan.Contains((byte)0);
            }

            benchmark.Stop();
            Console.WriteLine($"USpan.Contains: {benchmark.ElapsedMilliseconds}ms");

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                systemSpan.Fill(4);
            }

            benchmark.Stop();
            Console.WriteLine($"System.Span.Fill: {benchmark.ElapsedMilliseconds}ms");

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                unmanagedSpan.Fill(4);
            }

            benchmark.Stop();
            Console.WriteLine($"USpan.Fill: {benchmark.ElapsedMilliseconds}ms");

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                systemSpan.Clear();
            }

            benchmark.Stop();
            Console.WriteLine($"System.Span.Clear: {benchmark.ElapsedMilliseconds}ms");

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                unmanagedSpan.Clear();
            }

            benchmark.Stop();
            Console.WriteLine($"USpan.Clear: {benchmark.ElapsedMilliseconds}ms");

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                systemSpan.Slice(0, 512);
            }

            benchmark.Stop();
            Console.WriteLine($"System.Span.Slice: {benchmark.ElapsedMilliseconds}ms");

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                unmanagedSpan.Slice(0, 512);
            }

            benchmark.Stop();
            Console.WriteLine($"USpan.Slice: {benchmark.ElapsedMilliseconds}ms");
        }
    }
}
