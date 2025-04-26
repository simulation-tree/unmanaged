using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Unmanaged.Tests
{
    public class AllocationTests : UnmanagedTests
    {
        [Test]
        public void DefaultSizelessAllocation()
        {
            MemoryAddress allocation = MemoryAddress.AllocateEmpty();
            Assert.That(allocation == default, Is.False);
            allocation.Dispose();
            Assert.That(allocation == default, Is.True);
        }

        [Test]
        public void WriteMultipleValues()
        {
            using MemoryAddress allocation = MemoryAddress.Allocate((sizeof(uint) * 4));
            allocation.Write(0 * sizeof(uint), 5);
            allocation.Write(1 * sizeof(uint), 15);
            allocation.Write(2 * sizeof(uint), 25);
            allocation.Write(3 * sizeof(uint), 50);

            Span<uint> bufferSpan = allocation.AsSpan<uint>(0, 4);
            Assert.That(bufferSpan.Length, Is.EqualTo(4));
            Assert.That(bufferSpan[0], Is.EqualTo(5));
            Assert.That(bufferSpan[1], Is.EqualTo(15));
            Assert.That(bufferSpan[2], Is.EqualTo(25));
            Assert.That(bufferSpan[3], Is.EqualTo(50));
        }

        [Test]
        public void WriteSpan()
        {
            using MemoryAddress a = MemoryAddress.Allocate((sizeof(int) * 4));
            a.Write(0, [2, 3, 4, 5]);
            Span<int> bufferSpan = a.AsSpan<int>(0, 4);
            Assert.That(bufferSpan.Length, Is.EqualTo(4));
            Assert.That(bufferSpan[0], Is.EqualTo(2));
            Assert.That(bufferSpan[1], Is.EqualTo(3));
            Assert.That(bufferSpan[2], Is.EqualTo(4));
            Assert.That(bufferSpan[3], Is.EqualTo(5));
        }

        [Test]
        public void ResizeAllocation()
        {
            MemoryAddress a = MemoryAddress.Allocate(sizeof(int));
            a.Write(0 * sizeof(int), 1337);
            MemoryAddress.Resize(ref a, sizeof(int) * 2);
            a.Write(1 * sizeof(int), 1338);

            Span<int> span = a.AsSpan<int>(0, 2);
            Assert.That(span[0], Is.EqualTo(1337));
            Assert.That(span[1], Is.EqualTo(1338));
            a.Dispose();
        }

        [Test]
        public void ReadPartsOfTuple()
        {
            using MemoryAddress tuple = MemoryAddress.Allocate(8);
            tuple.Write(0, (5, 1337));

            int a = tuple.Read<int>(0 * sizeof(int));
            int b = tuple.Read<int>(1 * sizeof(int));
            Assert.That(a, Is.EqualTo(5));
            Assert.That(b, Is.EqualTo(1337));

            tuple.Write(1 * sizeof(int), 23);

            a = tuple.Read<int>(0 * sizeof(int));
            b = tuple.Read<int>(1 * sizeof(int));
            Assert.That(a, Is.EqualTo(5));
            Assert.That(b, Is.EqualTo(23));
        }

        [Test]
        public void CreateAndDestroy()
        {
            MemoryAddress obj = MemoryAddress.Allocate(sizeof(int));
            Assert.That(obj == default, Is.False);
            obj.Dispose();
            Assert.That(obj == default, Is.True);
        }

        [Test]
        public void CheckDefault()
        {
            using MemoryAddress obj = MemoryAddress.Allocate(sizeof(long));
            obj.Clear(sizeof(long));
            Assert.That(obj == default, Is.False);

            Span<byte> data = obj.AsSpan<byte>(0, sizeof(long));
            Assert.That(data.Length, Is.EqualTo(sizeof(long)));
            ulong value = BitConverter.ToUInt64(data);
            Assert.That(value, Is.EqualTo(0));
        }

#if DEBUG
        [Test]
        public void ThrowOnDisposeTwice()
        {
            MemoryAddress obj = MemoryAddress.Allocate(sizeof(int));
            obj.Dispose();
            Assert.Throws<InvalidOperationException>(() => obj.Dispose());
        }

        [Test]
        public void ThrowOnDisposeTwiceThroughACopy()
        {
            MemoryAddress obj = MemoryAddress.Allocate(sizeof(int));
            MemoryAddress copy = obj;
            obj.Dispose();
            Assert.Throws<ObjectDisposedException>(() => copy.Dispose());
        }

        [Test]
        public void AccessOutOfBounds()
        {
            MemoryAddress obj = MemoryAddress.Allocate(sizeof(int));
            obj.Read<int>(0);
            obj.ReadElement<int>(0);
            Assert.Throws<IndexOutOfRangeException>(() => obj.Read(4));
            Assert.Throws<IndexOutOfRangeException>(() => obj.Read<int>(1));
            Assert.Throws<IndexOutOfRangeException>(() => obj.ReadElement<int>(1));
            obj.Dispose();
        }
#endif

        [Test]
        public void ClearAllocation()
        {
            using MemoryAddress obj = MemoryAddress.Allocate((sizeof(int) * 4));
            Span<int> span = obj.AsSpan<int>(0, 4);
            span[0] = 5;
            Assert.That(obj.AsSpan<int>(0, 1)[0], Is.EqualTo(5));
            obj.Clear(sizeof(int) * 4);
            Assert.That(obj.AsSpan<int>(0, 1)[0], Is.EqualTo(0));

            Span<int> bufferSpan = obj.AsSpan<int>(0, 4);
            Assert.That(bufferSpan.Length, Is.EqualTo(4));
            Assert.That(bufferSpan[0], Is.EqualTo(0));
            Assert.That(bufferSpan[1], Is.EqualTo(0));
            Assert.That(bufferSpan[2], Is.EqualTo(0));
            Assert.That(bufferSpan[3], Is.EqualTo(0));
        }

        [Test]
        public void ModifyingThroughDifferentInterfaces()
        {
            using MemoryAddress obj = MemoryAddress.Allocate(sizeof(int));
            Span<int> bufferSpan = obj.AsSpan<int>(0, 1);
            ref int x = ref obj.AsSpan<int>(0, 1)[0];
            x = 5;
            Assert.That(bufferSpan[0], Is.EqualTo(5));
            bufferSpan[0] *= 2;
            Assert.That(obj.AsSpan<int>(0, 1)[0], Is.EqualTo(10));
        }

        [Test]
        public void CopyingIntoAnother()
        {
            using MemoryAddress a = MemoryAddress.Allocate(sizeof(int) * 4);
            using MemoryAddress b = MemoryAddress.Allocate(sizeof(int) * 8);
            a.Write(0 * sizeof(int), 1);
            a.Write(1 * sizeof(int), 2);
            a.Write(2 * sizeof(int), 3);
            a.Write(3 * sizeof(int), 4);
            a.CopyTo(b, 0, 0, sizeof(int) * 4);
            b.Write((4 + 0) * sizeof(int), 5);
            b.Write((4 + 1) * sizeof(int), 6);
            b.Write((4 + 2) * sizeof(int), 7);
            b.Write((4 + 3) * sizeof(int), 8);
            Span<int> bufferSpan = b.AsSpan<int>(0, 8);
            Assert.That(bufferSpan.Length, Is.EqualTo(8));
            Assert.That(bufferSpan[0], Is.EqualTo(1));
            Assert.That(bufferSpan[1], Is.EqualTo(2));
            Assert.That(bufferSpan[2], Is.EqualTo(3));
            Assert.That(bufferSpan[3], Is.EqualTo(4));
            Assert.That(bufferSpan[4], Is.EqualTo(5));
            Assert.That(bufferSpan[5], Is.EqualTo(6));
            Assert.That(bufferSpan[6], Is.EqualTo(7));
            Assert.That(bufferSpan[7], Is.EqualTo(8));
        }

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