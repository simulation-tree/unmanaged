using System;
using System.Diagnostics;
using Unmanaged.Collections;

namespace Unmanaged
{
    public unsafe struct BinaryReader(ReadOnlySpan<byte> data) : IDisposable
    {
        private readonly UnmanagedList<byte> data = new(data);
        private uint position;

        public readonly void Dispose()
        {
            ThrowIfDisposed();
            data.Dispose();
        }

        [Conditional("TRACK_ALLOCATIONS")]
        private readonly void ThrowIfDisposed()
        {
            if (data.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(BinaryReader));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfEndOfData()
        {
            if (position >= data.Count)
            {
                throw new InvalidOperationException("End of data.");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfReadPastEnd(uint size)
        {
            if (position + size > data.Count)
            {
                throw new InvalidOperationException("Read past end of data.");
            }
        }

        public T ReadValue<T>() where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfEndOfData();

            uint size = (uint)(sizeof(T));
            ThrowIfReadPastEnd(size);

            byte* ptr = stackalloc byte[(int)size];
            for (uint i = 0; i < size; i++)
            {
                ptr[i] = data[position + i];
            }

            position += size;
            return *(T*)ptr;
        }

        public ReadOnlySpan<T> ReadSpan<T>(uint length) where T : unmanaged
        {
            ThrowIfDisposed();
            ThrowIfEndOfData();

            uint size = (uint)(sizeof(T) * length);
            ThrowIfReadPastEnd(size);

            nint address = (nint)(data.Address + position);
            var span = new Span<T>((T*)address, (int)length);
            position += size;
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