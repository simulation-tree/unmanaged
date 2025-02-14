using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Unmanaged.Tests
{
    public class SpanTests : UnmanagedTests
    {
        [Test]
        public void VerifyRangeProperties()
        {
            URange a = new(5, 8);
            Assert.That(a.start, Is.EqualTo(5));
            Assert.That(a.end, Is.EqualTo(8));
            Assert.That(a.Length, Is.EqualTo(3));

            URange b = new(5, 8);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.ToString(), Is.EqualTo("5..8"));
            Assert.That(a.ToString(), Is.EqualTo(b.ToString()));
        }

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

#if DEBUG
        [Test]
        public void ThrowIfAccessingSpanOutOfRange()
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
        public void ThrowIfSlicingOutOfRange()
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
        public void ThrowIfReinterpretSpanOfDifferentSize()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                USpan<byte> data = stackalloc byte[8];
                data[0] = 1;
                data[1] = 2;

                USpan<int> intData = data.As<int>();
            });
        }
#endif

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

            URange range = new(2, 4);
            USpan<byte> slice = data.Slice(range);
            Span<byte> referenceSlice = referenceData[(Range)range];

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
        public unsafe void NonPowerOf2SizedSpan()
        {
            uint typeSize = (uint)sizeof(StrangeType);
            USpan<StrangeType> array = stackalloc StrangeType[4];
            array[0] = new StrangeType(1, 2, new Vector4(3, 4, 5, 6), 7);
            array[1] = new StrangeType(8, 9, new Vector4(10, 11, 12, 13), 14);
            array[2] = new StrangeType(15, 16, new Vector4(17, 18, 19, 20), 21);
            array[3] = new StrangeType(22, 23, new Vector4(24, 25, 26, 27), 28);

            using Allocation allocation = Allocation.Create(array);

            StrangeType firstRead = allocation.Read<StrangeType>(typeSize * 0);
            Assert.That(firstRead.a, Is.EqualTo(1));
            Assert.That(firstRead.b, Is.EqualTo(2));
            Assert.That(firstRead.c, Is.EqualTo(new Vector4(3, 4, 5, 6)));
            Assert.That(firstRead.d, Is.EqualTo(7));

            StrangeType secondRead = allocation.Read<StrangeType>(typeSize * 1);
            Assert.That(secondRead.a, Is.EqualTo(8));
            Assert.That(secondRead.b, Is.EqualTo(9));
            Assert.That(secondRead.c, Is.EqualTo(new Vector4(10, 11, 12, 13)));
            Assert.That(secondRead.d, Is.EqualTo(14));

            StrangeType thirdRead = allocation.Read<StrangeType>(typeSize * 2);
            Assert.That(thirdRead.a, Is.EqualTo(15));
            Assert.That(thirdRead.b, Is.EqualTo(16));
            Assert.That(thirdRead.c, Is.EqualTo(new Vector4(17, 18, 19, 20)));
            Assert.That(thirdRead.d, Is.EqualTo(21));

            StrangeType fourthRead = allocation.Read<StrangeType>(typeSize * 3);
            Assert.That(fourthRead.a, Is.EqualTo(22));
            Assert.That(fourthRead.b, Is.EqualTo(23));
            Assert.That(fourthRead.c, Is.EqualTo(new Vector4(24, 25, 26, 27)));
            Assert.That(fourthRead.d, Is.EqualTo(28));

            Allocation first = allocation.Read(0);
            Assert.That(first.Read<byte>(0), Is.EqualTo(1));
            Assert.That(first.Read<float>(1), Is.EqualTo(2));
            Assert.That(first.Read<Vector4>(5), Is.EqualTo(new Vector4(3, 4, 5, 6)));
            Assert.That(first.Read<uint>(21), Is.EqualTo(7));

            Allocation secondElementThirdField = allocation.Read(typeSize + sizeof(byte) + sizeof(float));
            Vector4 value = secondElementThirdField.Read<Vector4>();

            Assert.That(value, Is.EqualTo(new Vector4(10, 11, 12, 13)));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct StrangeType
        {
            public byte a;
            public float b;
            public Vector4 c;
            public uint d;

            public StrangeType(byte a, float b, Vector4 c, uint d)
            {
                this.a = a;
                this.b = b;
                this.c = c;
                this.d = d;
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

#if DEBUG
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                USpan<byte> data = stackalloc byte[8];
                data[0] = 1;
                data[1] = 2;
                USpan<byte> lessData = stackalloc byte[1];
                data.CopyTo(lessData);
            });
#endif
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
        public void GetDefaultSpanFromEmpty()
        {
            USpan<char> emptyUnmanaged = default;
            Span<char> emptySystem = emptyUnmanaged;
            Assert.That(emptySystem.IsEmpty, Is.True);
        }

        [Test]
        public void CopyEmptySpan()
        {
            USpan<char> empty = default;
            USpan<char> destination = stackalloc char[5];
            empty.CopyTo(destination);
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

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                uint total = 0;
                foreach (byte x in systemSpan)
                {
                    total += x;
                }
            }

            benchmark.Stop();
            Console.WriteLine($"System.Span.ForEach: {benchmark.ElapsedMilliseconds}ms");

            benchmark.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                uint total = 0;
                foreach (byte x in unmanagedSpan)
                {
                    total += x;
                }
            }

            benchmark.Stop();
            Console.WriteLine($"USpan.ForEach: {benchmark.ElapsedMilliseconds}ms");
        }
    }
}
