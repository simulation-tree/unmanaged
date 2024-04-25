using Unmanaged;

namespace Tests
{
    public class AllocationTests
    {
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
        public void AccessBadInstance()
        {
            Allocation obj = default;
            Assert.Throws<NullReferenceException>(() => { obj.Dispose(); });
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
