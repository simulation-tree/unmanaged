using System;
using Unmanaged.Collections;

namespace Unmanaged
{
    public readonly unsafe struct BinaryWriter : IDisposable
    {
        private readonly UnmanagedList<byte> data;

        public readonly bool IsDisposed => data.IsDisposed;

        public BinaryWriter()
        {
            data = new();
        }

        public BinaryWriter(UnmanagedList<byte> data)
        {
            this.data = data;
        }

        public readonly void WriteValue<T>(T value) where T : unmanaged
        {
            byte* ptr = (byte*)&value;
            int size = sizeof(T);
            for (int i = 0; i < size; i++)
            {
                data.Add(ptr[i]);
            }
        }

        public readonly void WriteSpan<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            fixed (T* ptr = span)
            {
                byte* bytePtr = (byte*)ptr;
                int size = sizeof(T) * span.Length;
                for (int i = 0; i < size; i++)
                {
                    data.Add(bytePtr[i]);
                }
            }
        }

        public readonly void WriteSerializable<T>(T value) where T : unmanaged, ISerializable
        {
            value.Serialize(this);
        }

        public readonly void Dispose()
        {
            data.Dispose();
        }

        public readonly ReadOnlySpan<byte> AsSpan()
        {
            return data.AsSpan();
        }
    }

    public interface ISerializable
    {
        /// <summary>
        /// Serializes the object into the writer.
        /// </summary>
        void Serialize(BinaryWriter writer);
    }

    public interface IDeserializable
    {
        /// <summary>
        /// Deserializes the object from it's <c>default</c> state, with
        /// the data in the reader.
        /// </summary>
        void Deserialize(ref BinaryReader reader);
    }
}