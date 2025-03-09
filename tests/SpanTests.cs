using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Unmanaged.Tests
{
    public class SpanTests : UnmanagedTests
    {
#if DEBUG
        [Test]
        public void ThrowIfAccessingSpanOutOfRange()
        {
            Span<byte> data = stackalloc byte[8];
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

            Span<byte> uData = stackalloc byte[8];
            try
            {
                Span<byte> slice = uData.Slice(8, 1);
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
            Assert.Throws<InvalidCastException>(() =>
            {
                Span<byte> data = stackalloc byte[8];
                data[0] = 1;
                data[1] = 2;

                Span<int> intData = data.As<byte, int>();
            });
        }
#endif

        [Test]
        public unsafe void NonPowerOf2SizedSpan()
        {
            int typeSize = sizeof(StrangeType);
            Span<StrangeType> array = stackalloc StrangeType[4];
            array[0] = new StrangeType(1, 2, new Vector4(3, 4, 5, 6), 7);
            array[1] = new StrangeType(8, 9, new Vector4(10, 11, 12, 13), 14);
            array[2] = new StrangeType(15, 16, new Vector4(17, 18, 19, 20), 21);
            array[3] = new StrangeType(22, 23, new Vector4(24, 25, 26, 27), 28);

            using MemoryAddress allocation = MemoryAddress.Allocate(array);

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

            MemoryAddress first = allocation.Read(0);
            Assert.That(first.Read<byte>(0), Is.EqualTo(1));
            Assert.That(first.Read<float>(1), Is.EqualTo(2));
            Assert.That(first.Read<Vector4>(5), Is.EqualTo(new Vector4(3, 4, 5, 6)));
            Assert.That(first.Read<uint>(21), Is.EqualTo(7));

            MemoryAddress secondElementThirdField = allocation.Read(typeSize + sizeof(byte) + sizeof(float));
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
    }
}
