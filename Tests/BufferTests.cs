using Unmanaged;

namespace Tests
{
    public class BufferTests
    {
        [Test]
        public void CreateDestroyBuffer()
        {
            UnmanagedBuffer buffer = new(4, sizeof(int), false);
            Assert.That(buffer.IsDisposed, Is.False);
            buffer.Dispose();
            Assert.That(buffer.IsDisposed, Is.True);
        }

        [Test]
        public void ThrowOnDisposeTwice()
        {
            UnmanagedBuffer buffer = new(4, sizeof(int));
            buffer.Dispose();
            Assert.Throws<ObjectDisposedException>(() => buffer.Dispose());
        }

        [Test]
        public void CreateClearBuffer()
        {
            using UnmanagedBuffer buffer = new(4, sizeof(int));
            Span<int> bufferSpan = buffer.AsSpan<int>();
            Assert.That(bufferSpan.Length, Is.EqualTo(buffer.length));
            Assert.That(bufferSpan[0], Is.EqualTo(0));
            Assert.That(bufferSpan[1], Is.EqualTo(0));
            Assert.That(bufferSpan[2], Is.EqualTo(0));
            Assert.That(bufferSpan[3], Is.EqualTo(0));
        }

        [Test]
        public void ModifyingThroughDifferentInterfaces()
        {
            using UnmanagedBuffer buffer = new(4, sizeof(int));
            Span<int> bufferSpan = buffer.AsSpan<int>();
            buffer.Set(0, 5);
            Assert.That(bufferSpan[0], Is.EqualTo(5));
            bufferSpan[0] *= 2;
            Assert.That(buffer.Get<int>(0), Is.EqualTo(10));
        }
    }
}
