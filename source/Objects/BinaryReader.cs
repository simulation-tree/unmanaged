using System;
using System.Diagnostics;
using Unmanaged.Collections;

namespace Unmanaged
{
    public unsafe struct BinaryReader : IDisposable
    {
        private UnmanagedList<byte> data;
        private uint position;

        public uint Position
        {
            readonly get => position;
            set
            {
                position = value;
            }
        }

        public readonly uint Length => data.Count;
        public readonly bool IsDisposed => data.IsDisposed;

        public BinaryReader(ReadOnlySpan<byte> data)
        {
            this.data = new(data);
            position = 0;
        }

        public BinaryReader(UnmanagedList<byte> data)
        {
            this.data = data;
            position = 0;
        }

        public BinaryReader()
        {
            data = new();
            position = 0;
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            data.Dispose();
            data = default;
        }

        /// <summary>
        /// Returns the data of the reader as a span.
        /// </summary>
        public readonly ReadOnlySpan<byte> AsSpan()
        {
            return data.AsSpan();
        }

        [Conditional("TRACK_ALLOCATIONS")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(BinaryReader));
            }
        }

        public readonly T PeekValue<T>() where T : unmanaged
        {
            return PeekValue<T>(position);
        }

        public readonly T PeekValue<T>(uint position) where T : unmanaged
        {
            uint size = (uint)sizeof(T);
            byte* ptr = stackalloc byte[(int)size];
            for (uint i = 0; i < size; i++)
            {
                ptr[i] = data[position + i];
            }

            return *(T*)ptr;
        }

        public readonly ReadOnlySpan<T> PeekSpan<T>(uint length) where T : unmanaged
        {
            return PeekSpan<T>(position, length);
        }

        public T ReadValue<T>() where T : unmanaged
        {
            T value = PeekValue<T>();
            Advance<T>();
            return value;
        }

        public void Advance(uint size)
        {
            position += size;
        }

        public void Advance<T>(uint length = 1) where T : unmanaged
        {
            Advance((uint)sizeof(T) * length);
        }

        /// <summary>
        /// Reads a span of values from the reader with the specified length.
        /// </summary>
        public ReadOnlySpan<T> ReadSpan<T>(uint length) where T : unmanaged
        {
            if (length + position > data.Count)
            {
                throw new InvalidOperationException("Read past end of data.");
            }

            ReadOnlySpan<T> span = PeekSpan<T>(position, length);
            position += (uint)(sizeof(T) * length);
            return span;
        }

        /// <summary>
        /// Reads a span starting at the given position in bytes.
        /// </summary>
        public readonly ReadOnlySpan<T> PeekSpan<T>(uint position, uint length) where T : unmanaged
        {
            nint address = (nint)(data.Address + position);
            Span<T> span = new((T*)address, (int)length);
            return span;
        }

        public T ReadSerializable<T>() where T : unmanaged, IDeserializable
        {
            T value = default;
            value.Deserialize(ref this);
            return value;
        }
    }
}