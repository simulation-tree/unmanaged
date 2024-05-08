using Unmanaged;

namespace Tests
{
    public class AllocationTests
    {
        [Test]
        public void DefaultSizelessAllocation()
        {
            Allocation allocation = new();
            Assert.That(allocation.IsDisposed, Is.False);
            Assert.That(allocation.Length, Is.EqualTo(0));
            allocation.Dispose();
            Assert.That(Allocations.Any, Is.False);
        }

        [Test]
        public void WriteMultipleValues()
        {
            using Allocation allocation = new(sizeof(uint) * 4);
            allocation.Write(0, 5);
            allocation.Write(1, 15);
            allocation.Write(2, 25);
            allocation.Write(3, 50);

            Span<uint> bufferSpan = allocation.AsSpan<uint>();
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
            a.Write(0, 1337);
            a.Resize(sizeof(int) * 2);
            a.Write(1, 1338);
            Assert.That(Allocations.Count, Is.EqualTo(1));
            Assert.That(a.AsSpan<int>()[0], Is.EqualTo(1337));
            Assert.That(a.AsSpan<int>()[1], Is.EqualTo(1338));
        }

        [Test]
        public void AllocateAndFree()
        {
            nint pointer = 1337;
            Allocations.Register(pointer);
            Allocations.Unregister(pointer);
            Assert.Throws<ObjectDisposedException>(() => { Allocations.Unregister(pointer); });
            Assert.That(Allocations.IsNull(pointer), Is.True);
        }

        [Test]
        public void ThrowNeverAllocatedPointer()
        {
            nint pointer = Guid.NewGuid().GetHashCode();
            Assert.Throws<NullReferenceException>(() => { Allocations.Unregister(pointer); });
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
            Assert.That(obj.IsDisposed, Is.False);

            Span<byte> data = obj.AsSpan<byte>();
            Assert.That(data.Length, Is.EqualTo(sizeof(long)));
        }

        [Test]
        public void ThrowOnDisposeTwice()
        {
            Allocation obj = new(sizeof(int));
            obj.Dispose();
            Assert.Throws<ObjectDisposedException>(() => obj.Dispose());
        }

        [Test]
        public void ClearAllocation()
        {
            using Allocation obj = new(sizeof(int) * 4);
            obj.AsSpan<int>()[0] = 5;
            Assert.That(obj.AsSpan<int>()[0], Is.EqualTo(5));
            obj.Clear();
            Assert.That(obj.AsSpan<int>()[0], Is.EqualTo(0));

            Span<int> bufferSpan = obj.AsSpan<int>();
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
            Assert.Throws<IndexOutOfRangeException>(() => { obj.AsSpan<int>()[1] = 5; });
            Assert.Throws<IndexOutOfRangeException>(() => { obj.AsSpan<int>()[-1] = 5; });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Span<byte> bufferSpan = obj.AsSpan<byte>(0, 5);
            });

            Span<byte> okBuffer = obj.AsSpan<byte>(0, 4);
        }

        [Test]
        public void CheckAllocationsForDebugging()
        {
            Allocation a = new(sizeof(int));
            Allocation b = new(sizeof(int));
            Allocation c = new(sizeof(int));

            Assert.That(Allocations.Any, Is.True);
            Assert.That(Allocations.All.Count(), Is.EqualTo(3));

            a.Dispose();
            b.Dispose();
            c.Dispose();

            Assert.That(Allocations.Any, Is.False);
        }

        [Test]
        public void ReadWithTypeOfDifferentSizeError()
        {
            using Allocation obj = new(sizeof(int));
            Assert.Throws<InvalidCastException>(() => { obj.AsRef<long>(); });
        }

        [Test]
        public void ModifyingThroughDifferentInterfaces()
        {
            using Allocation obj = new(sizeof(int));
            Span<int> bufferSpan = obj.AsSpan<int>();
            ref int x = ref obj.AsSpan<int>()[0];
            x = 5;
            Assert.That(bufferSpan[0], Is.EqualTo(5));
            bufferSpan[0] *= 2;
            Assert.That(obj.AsSpan<int>()[0], Is.EqualTo(10));
        }
    }
}
