using System;

namespace Unmanaged.Tests
{
    public class BinaryReaderWriterTests : UnmanagedTests
    {
        [Test]
        public void CloneSomething()
        {
            Something apple = new("Apple 2 xx");
            Something apple2 = apple.Clone();
            Assert.That(apple.Name, Is.EqualTo(apple2.Name));
        }

        public struct Something : ISerializable, IEquatable<Something>
        {
            private FixedString name;

            public readonly FixedString Name => name;

            public Something(string name)
            {
                this.name = new(name);
            }

            void ISerializable.Write(BinaryWriter writer)
            {
                writer.WriteUTF8Text(name);
            }

            void ISerializable.Read(BinaryReader reader)
            {
                USpan<char> buffer = stackalloc char[(int)FixedString.Capacity];
                uint length = reader.ReadUTF8Span(buffer);
                name = new FixedString(buffer.Slice(0, length));
            }

            public override bool Equals(object? obj)
            {
                return obj is Something something && Equals(something);
            }

            public bool Equals(Something other)
            {
                return name.Equals(other.name);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(name);
            }

            public static bool operator ==(Something left, Something right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Something left, Something right)
            {
                return !(left == right);
            }
        }

        [Test]
        public void WriteValues()
        {
            using BinaryWriter writer = new();
            writer.WriteValue(32);
            Assert.That(writer.Position, Is.EqualTo(sizeof(int)));
            writer.WriteValue(64);
            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 2));
            writer.WriteValue(128);
            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 3));

            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 3));
            byte[] bytes = writer.GetBytes().ToArray();
            using BinaryReader reader = new(bytes);
            byte[] readerBytes = reader.GetBytes().ToArray();
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(32));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(64));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(128));
        }

        [Test]
        public void WriteSpan()
        {
            using BinaryWriter writer = new();
            writer.WriteSpan("Hello there".AsUSpan());

            using BinaryReader reader = new(writer.GetBytes());
            Assert.That(reader.ReadSpan<char>(11).ToString(), Is.EqualTo("Hello there"));
        }

        [Test]
        public void CreateReaderFromBytes()
        {
            using var stream = new System.IO.MemoryStream();
            using System.IO.BinaryWriter binWriter = new(stream);
            binWriter.Write(32);
            binWriter.Write(64);
            binWriter.Write(128);
            byte[] bytes = stream.ToArray();
            using BinaryReader reader = new(bytes);
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(32));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(64));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(128));
        }

        [Test]
        public void CreateReaderFromStream()
        {
            using var stream = new System.IO.MemoryStream();
            stream.Write([32, 0, 0, 0, 64, 0, 0, 0, 128, 0, 0, 0]);
            stream.Position = 0;
            using BinaryReader reader = new(stream);
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(32));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(64));
            Assert.That(reader.ReadValue<int>(), Is.EqualTo(128));
        }

        [Test]
        public void ReadUTF8Text()
        {
            byte[] data = new byte[] { 239, 187, 191, 60, 80, 114, 111, 106, 101, 99, 116, 32, 83, 100, 107 };
            using BinaryReader reader = new(data);
            USpan<char> sample = stackalloc char[15];
            reader.ReadUTF8Span(sample);
            Assert.That(sample.ToString(), Is.EqualTo("﻿<Project Sdk\0\0"));
        }

        [Test]
        public void WriteUTF8Text()
        {
            string myString = "Hello, 你好, 🌍";
            using BinaryWriter writer = new();
            writer.WriteUTF8Text(MemoryExtensions.AsSpan(myString));
            using BinaryReader reader = new(writer.GetBytes());
            USpan<char> sample = stackalloc char[32];
            uint length = reader.ReadUTF8Span(sample);
            USpan<char> result = sample.Slice(0, length);
            string resultString = new string(result.AsSystemSpan());
            Assert.That(resultString, Is.EqualTo(myString));
        }

        [Test]
        public void ReuseWriter()
        {
            BinaryWriter writer = new();
            writer.WriteValue(32);
            writer.WriteValue(64);
            writer.WriteValue(128);

            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 3));
            int[] values = writer.AsSpan<int>().ToArray();
            writer.Position = 0;
            Assert.That(writer.Position, Is.EqualTo(0));
            Assert.That(values, Has.Length.EqualTo(3));
            Assert.That(values, Contains.Item(32));
            Assert.That(values, Contains.Item(64));
            Assert.That(values, Contains.Item(128));
            Assert.That(writer.AsSpan<int>().Length, Is.EqualTo(0));

            writer.Dispose();
        }
    }
}
