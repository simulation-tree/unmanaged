using System;
using Unmanaged;
using Unmanaged.Collections;

namespace Tests
{
    public class BinaryReaderWriterTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAny();
        }

        [Test]
        public void CloneBigObject()
        {
            using BigObject bigObject = new(1);
            bigObject.Add("Apple");
            bigObject.Add("Boo");
            bigObject.Add("Cherry");

            Assert.That(bigObject.Get(0).ToString(), Is.EqualTo("Apple"));
            Assert.That(bigObject.Get(1).ToString(), Is.EqualTo("Boo"));
            Assert.That(bigObject.Get(2).ToString(), Is.EqualTo("Cherry"));

            using BigObject bigObjectAgain = bigObject.Clone();
            Assert.That(bigObject.Count, Is.EqualTo(bigObjectAgain.Count));
            Assert.That(bigObject.Get(0).ToString(), Is.EqualTo(bigObjectAgain.Get(0).ToString()));
            Assert.That(bigObject.Get(1).ToString(), Is.EqualTo(bigObjectAgain.Get(1).ToString()));
            Assert.That(bigObject.Get(2).ToString(), Is.EqualTo(bigObjectAgain.Get(2).ToString()));
        }

        public struct BigObject : ISerializable, IDisposable
        {
            private UnmanagedList<Something> items;

            public readonly uint Count => items.Count;

            public BigObject(uint capacity)
            {
                items = UnmanagedList<Something>.Create(capacity);
            }

            public readonly void Add(ReadOnlySpan<char> name)
            {
                items.Add(new Something(name));
            }

            public readonly FixedString Get(uint index)
            {
                return items[index].Name;
            }

            public void Dispose()
            {
                items.Dispose();
            }

            void ISerializable.Read(BinaryReader reader)
            {
                if (items.IsDisposed)
                {
                    items = UnmanagedList<Something>.Create();
                }

                items.Clear();
                uint count = reader.ReadValue<uint>();
                for (uint i = 0; i < count; i++)
                {
                    Something item = reader.ReadObject<Something>();
                    items.Add(item);
                }
            }

            void ISerializable.Write(BinaryWriter writer)
            {
                writer.WriteValue(items.Count);
                foreach (Something item in items)
                {
                    writer.WriteObject(item);
                    writer.WriteValue<byte>(0);
                }
            }
        }

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

            public Something(ReadOnlySpan<char> name)
            {
                this.name = name;
            }

            void ISerializable.Write(BinaryWriter writer)
            {
                writer.WriteUTF8Span(name);
            }

            void ISerializable.Read(BinaryReader reader)
            {
                Span<char> buffer = stackalloc char[FixedString.MaxLength];
                int length = reader.ReadUTF8Span(buffer);
                name = new FixedString(buffer[..length]);
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
            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteValue(32);
            Assert.That(writer.Position, Is.EqualTo(sizeof(int)));
            writer.WriteValue(64);
            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 2));
            writer.WriteValue(128);
            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 3));

            Assert.That(writer.Position, Is.EqualTo(sizeof(int) * 3));
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
            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteSpan("Hello there".AsSpan());

            using BinaryReader reader = new(writer.AsSpan());
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
            Span<char> sample = stackalloc char[15];
            reader.ReadUTF8Span(sample);
            Assert.That(sample.ToString(), Is.EqualTo("﻿<Project Sdk\0\0"));
        }

        [Test]
        public void WriteUTF8Text()
        {
            string myString = "Hello, 你好, 🌍";
            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteUTF8Span(myString.AsSpan());
            using BinaryReader reader = new(writer.AsSpan());
            Span<char> sample = stackalloc char[32];
            int length = reader.ReadUTF8Span(sample);
            ReadOnlySpan<char> result = sample.Slice(0, length);
            string resultString = new string(result);
            Assert.That(resultString, Is.EqualTo(myString));
        }

        [Test]
        public void ReuseWriter()
        {
            BinaryWriter writer = BinaryWriter.Create();
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
