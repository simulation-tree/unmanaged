using System;
using Unmanaged;

namespace Tests
{
    public class BinaryReaderWriterTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
        }

        [Test]
        public void WriteValues()
        {
            using BinaryWriter writer = new();
            writer.WriteValue(32);
            writer.WriteValue(64);
            writer.WriteValue(128);
            Assert.That(writer.Length, Is.EqualTo(sizeof(int) * 3));
            byte[] bytes = writer.AsSpan().ToArray();
            using BinaryReader reader = new(bytes);
            byte[] readerBytes = reader.AsSpan().ToArray();
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(32));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(64));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(128));
        }

        [Test]
        public void WriteSpan()
        {
            using BinaryWriter writer = new();
            writer.WriteSpan("Hello there".AsSpan());

            using BinaryReader reader = new(writer.AsSpan());
            Assert.That(reader.ReadSpan<char>(11).ToString(), Is.EqualTo("Hello there"));
        }
    }
}
