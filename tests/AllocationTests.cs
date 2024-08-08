﻿using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using Unmanaged;

namespace Tests
{
    public class AllocationTests
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
            allocation.Write(5, 0 * sizeof(uint));
            allocation.Write(15, 1 * sizeof(uint));
            allocation.Write(25, 2 * sizeof(uint));
            allocation.Write(50, 3 * sizeof(uint));

            Span<uint> bufferSpan = allocation.AsSpan<uint>(0, 4);
            Assert.That(bufferSpan.Length, Is.EqualTo(4));
            Assert.That(bufferSpan[0], Is.EqualTo(5));
            Assert.That(bufferSpan[1], Is.EqualTo(15));
            Assert.That(bufferSpan[2], Is.EqualTo(25));
            Assert.That(bufferSpan[3], Is.EqualTo(50));
        }

        [Test]
        public void ResizeAllocation()
        {
            using Allocation a = new(sizeof(int));
            a.Write(1337, 0 * sizeof(int));
            a.Resize(sizeof(int) * 2);
            a.Write(1338, 1 * sizeof(int));
            Assert.That(Allocations.Count, Is.EqualTo(1));

            Span<int> span = a.AsSpan<int>(0, 2);
            Assert.That(span[0], Is.EqualTo(1337));
            Assert.That(span[1], Is.EqualTo(1338));
        }

        [Test]
        public void ReadPartsOfTuple()
        {
            using Allocation tuple = new(8);
            tuple.Write((5, 1337));

            int a = tuple.Read<int>(0 * sizeof(int));
            int b = tuple.Read<int>(1 * sizeof(int));
            Assert.That(a, Is.EqualTo(5));
            Assert.That(b, Is.EqualTo(1337));

            tuple.Write(23, 1 * sizeof(int));

            a = tuple.Read<int>(0 * sizeof(int));
            b = tuple.Read<int>(1 * sizeof(int));
            Assert.That(a, Is.EqualTo(5));
            Assert.That(b, Is.EqualTo(23));
        }

        [Test]
        public unsafe void AllocateAndFree()
        {
            void* pointer = Allocations.Allocate(sizeof(int));
            Assert.That(Allocations.IsNull(pointer), Is.False);
            Allocations.Free(ref pointer);
            Assert.That(Allocations.IsNull(pointer), Is.True);
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

            Span<byte> data = obj.AsSpan<byte>(0, sizeof(long));
            Assert.That(data.Length, Is.EqualTo(sizeof(long)));
            ulong value = BitConverter.ToUInt64(data);
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
        public void ClearAllocation()
        {
            using Allocation obj = new(sizeof(int) * 4);
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
        public void AccessDefaultInstanceError()
        {
            Allocation obj = default;
            Assert.Throws<NullReferenceException>(() => { obj.Dispose(); });
        }

        [Test]
        public void AccessSpanOutOfBoundsError()
        {
            using Allocation obj = new(sizeof(int));
            Assert.Throws<IndexOutOfRangeException>(() => { obj.AsSpan<int>(0, 1)[1] = 5; });
            //Assert.Throws<ArgumentOutOfRangeException>(() => { obj.AsSpan<int>(1, 1)[0] = 5; });
            //Assert.Throws<ArgumentOutOfRangeException>(() =>
            //{
            //    Span<byte> bufferSpan = obj.AsSpan<byte>(0, 5);
            //});

            Span<byte> okBuffer = obj.AsSpan<byte>(0, 4);
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
            using Allocation a = new(sizeof(int) * 4);
            using Allocation b = new(sizeof(int) * 8);
            a.Write(1, 0 * sizeof(int));
            a.Write(2, 1 * sizeof(int));
            a.Write(3, 2 * sizeof(int));
            a.Write(4, 3 * sizeof(int));
            a.CopyTo(b, 0, 0, sizeof(int) * 4);
            b.Write(5, (4 + 0) * sizeof(int));
            b.Write(6, (4 + 1) * sizeof(int));
            b.Write(7, (4 + 2) * sizeof(int));
            b.Write(8, (4 + 3) * sizeof(int));
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
    }
}
