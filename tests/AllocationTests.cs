using System;

namespace Unmanaged.Tests
{
    public class AllocationTests : UnmanagedTests
    {
        [Test]
        public void DefaultSizelessAllocation()
        {
            Allocation allocation = Allocation.Create();
            Assert.That(allocation.IsDisposed, Is.False);
            Assert.That(Allocations.Count, Is.EqualTo(1));
            allocation.Dispose();
            Assert.That(allocation.IsDisposed, Is.True);
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void WriteMultipleValues()
        {
            using Allocation allocation = new(sizeof(uint) * 4);
            allocation.Write(0 * sizeof(uint), 5);
            allocation.Write(1 * sizeof(uint), 15);
            allocation.Write(2 * sizeof(uint), 25);
            allocation.Write(3 * sizeof(uint), 50);

            USpan<uint> bufferSpan = allocation.AsSpan<uint>(0, 4);
            Assert.That(bufferSpan.Length, Is.EqualTo(4));
            Assert.That(bufferSpan[0], Is.EqualTo(5));
            Assert.That(bufferSpan[1], Is.EqualTo(15));
            Assert.That(bufferSpan[2], Is.EqualTo(25));
            Assert.That(bufferSpan[3], Is.EqualTo(50));
        }

        [Test]
        public void ResizeAllocation()
        {
            Allocation a = new(sizeof(int));
            a.Write(0 * sizeof(int), 1337);
            Allocation.Resize(ref a, sizeof(int) * 2);
            a.Write(1 * sizeof(int), 1338);
            Assert.That(Allocations.Count, Is.EqualTo(1));

            USpan<int> span = a.AsSpan<int>(0, 2);
            Assert.That(span[0], Is.EqualTo(1337));
            Assert.That(span[1], Is.EqualTo(1338));
            a.Dispose();
            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void ReadPartsOfTuple()
        {
            using Allocation tuple = new(8);
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
        public unsafe void AllocateAndFree()
        {
            void* pointer = Allocations.Allocate(sizeof(int));
            Assert.That(pointer is null, Is.False);
            Allocations.Free(ref pointer);
            Assert.That(pointer is null, Is.True);
        }

        [Test]
        public void CreateAndDestroy()
        {
            Allocation obj = new(sizeof(int));
            Assert.That(obj.IsDisposed, Is.False);
            obj.Dispose();
            Assert.That(obj.IsDisposed, Is.True);
        }

        [Test]
        public void CheckDefault()
        {
            using Allocation obj = new(sizeof(long));
            obj.Clear(sizeof(long));
            Assert.That(obj.IsDisposed, Is.False);

            USpan<byte> data = obj.AsSpan<byte>(0, sizeof(long));
            Assert.That(data.Length, Is.EqualTo(sizeof(long)));
            ulong value = BitConverter.ToUInt64(data.AsSystemSpan());
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void ThrowOnDisposeTwice()
        {
            Allocation obj = new(sizeof(int));
            obj.Dispose();
            Assert.Throws<NullReferenceException>(() => obj.Dispose());
        }

        [Test]
        public void ThrowIfLeaks()
        {
            Allocation obj = new(sizeof(int));
            Assert.Throws<Exception>(() => Allocations.ThrowIfAny());
            obj.Dispose();
        }

        [Test]
        public void ThrowIfIndexingOutOfBounds()
        {
            Allocation obj = new(4);
            Assert.Throws<IndexOutOfRangeException>(() => obj[4] = 5);
            obj.Dispose();

            obj = new(16);
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                unchecked
                {
                    obj[(uint)-1] = 232;
                }
            });

            obj.Dispose();
        }

        [Test]
        public void ClearAllocation()
        {
            using Allocation obj = new(sizeof(int) * 4);
            USpan<int> span = obj.AsSpan<int>(0, 4);
            span[0] = 5;
            Assert.That(obj.AsSpan<int>(0, 1)[0], Is.EqualTo(5));
            obj.Clear(sizeof(int) * 4);
            Assert.That(obj.AsSpan<int>(0, 1)[0], Is.EqualTo(0));

            USpan<int> bufferSpan = obj.AsSpan<int>(0, 4);
            Assert.That(bufferSpan.Length, Is.EqualTo(4));
            Assert.That(bufferSpan[0], Is.EqualTo(0));
            Assert.That(bufferSpan[1], Is.EqualTo(0));
            Assert.That(bufferSpan[2], Is.EqualTo(0));
            Assert.That(bufferSpan[3], Is.EqualTo(0));
        }

        [Test]
        public void AccessDefaultInstanceError()
        {
            Allocation obj = default;
            Assert.Throws<NullReferenceException>(() => { obj.Dispose(); });
        }

        [Test]
        public void AccessSpanOutOfBoundsError()
        {
            using Allocation obj = new(sizeof(int));
            Assert.Throws<ArgumentOutOfRangeException>(() => { obj.AsSpan<int>(0, 1)[1] = 5; });
            USpan<byte> okBuffer = obj.AsSpan<byte>(0, 4);
        }

        [Test]
        public void CheckAllocationsForDebugging()
        {
            Allocation a = new(sizeof(int));
            Allocation b = new(sizeof(int));
            Allocation c = new(sizeof(int));

            Assert.That(Allocations.Count, Is.EqualTo(3));

            a.Dispose();
            b.Dispose();
            c.Dispose();

            Assert.That(Allocations.Count, Is.EqualTo(0));
        }

        [Test]
        public void ModifyingThroughDifferentInterfaces()
        {
            using Allocation obj = new(sizeof(int));
            USpan<int> bufferSpan = obj.AsSpan<int>(0, 1);
            ref int x = ref obj.AsSpan<int>(0, 1)[0];
            x = 5;
            Assert.That(bufferSpan[0], Is.EqualTo(5));
            bufferSpan[0] *= 2;
            Assert.That(obj.AsSpan<int>(0, 1)[0], Is.EqualTo(10));
        }

        [Test]
        public void CopyingIntoAnother()
        {
            using Allocation a = new(sizeof(int) * 4);
            using Allocation b = new(sizeof(int) * 8);
            a.Write(0 * sizeof(int), 1);
            a.Write(1 * sizeof(int), 2);
            a.Write(2 * sizeof(int), 3);
            a.Write(3 * sizeof(int), 4);
            a.CopyTo(b, 0, 0, sizeof(int) * 4);
            b.Write((4 + 0) * sizeof(int), 5);
            b.Write((4 + 1) * sizeof(int), 6);
            b.Write((4 + 2) * sizeof(int), 7);
            b.Write((4 + 3) * sizeof(int), 8);
            USpan<int> bufferSpan = b.AsSpan<int>(0, 8);
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
    }
}
