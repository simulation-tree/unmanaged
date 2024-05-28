using System;
using System.Diagnostics;
using Unmanaged.Serialization.Unsafe;

namespace Unmanaged
{
    public unsafe struct BinaryReader : IDisposable
    {
        private UnsafeBinaryReader* reader;

        /// <summary>
        /// Position of the reader in the byte stream.
        /// </summary>
        public readonly uint Position
        {
            get => UnsafeBinaryReader.GetPositionRef(reader);
            set => UnsafeBinaryReader.GetPositionRef(reader) = value;
        }

        /// <summary>
        /// Length of the byte stream.
        /// </summary>
        public readonly uint Length => UnsafeBinaryReader.GetLength(reader);

        public readonly bool IsDisposed => UnsafeBinaryReader.IsDisposed(reader);

        public BinaryReader(ReadOnlySpan<byte> data, uint position = 0)
        {
            reader = UnsafeBinaryReader.Allocate(data, position);
        }

        public BinaryReader(ReadOnlySpan<char> data, uint position = 0)
        {
            fixed (char* ptr = data)
            {
                Span<byte> bytes = new(ptr, data.Length * sizeof(char));
                reader = UnsafeBinaryReader.Allocate(bytes, position);
            }
        }

        public BinaryReader()
        {
            reader = UnsafeBinaryReader.Allocate(default);
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            UnsafeBinaryReader.Free(ref reader);
        }

        /// <summary>
        /// Returns the data of the reader as a span.
        /// </summary>
        public readonly ReadOnlySpan<byte> AsSpan()
        {
            ThrowIfDisposed();
            return new(reader + 1, (int)Length);
        }

        /// <summary>
        /// Returns the remaining bytes.
        /// </summary>
        public readonly ReadOnlySpan<byte> GetRemainingBytes()
        {
            return AsSpan()[(int)Position..];
        }

        [Conditional("TRACK_ALLOCATIONS")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(BinaryReader));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfReadingPastLength(uint position)
        {
            if (position > Length)
            {
                throw new InvalidOperationException("Reading past end of data.");
            }
        }

        public readonly T PeekValue<T>() where T : unmanaged
        {
            return PeekValue<T>(Position);
        }

        public readonly T PeekValue<T>(uint position) where T : unmanaged
        {
            uint size = (uint)sizeof(T);
            ThrowIfReadingPastLength(position + size);
            nint address = (nint)(((nint)(reader + 1)) + position);
            return *(T*)address;
        }

        public readonly T ReadValue<T>() where T : unmanaged
        {
            T value = PeekValue<T>();
            Advance<T>();
            return value;
        }

        public readonly void Advance(uint size)
        {
            ref uint position = ref UnsafeBinaryReader.GetPositionRef(reader);
            ThrowIfReadingPastLength(position + size);
            position += size;
        }

        public readonly void Advance<T>(uint length = 1) where T : unmanaged
        {
            Advance((uint)sizeof(T) * length);
        }

        /// <summary>
        /// Reads a span of values from the reader with the specified length.
        /// </summary>
        public readonly ReadOnlySpan<T> ReadSpan<T>(uint length) where T : unmanaged
        {
            ref uint position = ref UnsafeBinaryReader.GetPositionRef(reader);
            ReadOnlySpan<T> span = PeekSpan<T>(Position, length);
            position += (uint)(sizeof(T) * length);
            return span;
        }

        public readonly ReadOnlySpan<T> PeekSpan<T>(uint length) where T : unmanaged
        {
            return PeekSpan<T>(Position, length);
        }

        /// <summary>
        /// Reads a span starting at the given position in bytes.
        /// </summary>
        public readonly ReadOnlySpan<T> PeekSpan<T>(uint position, uint length) where T : unmanaged
        {
            ThrowIfReadingPastLength(position + (uint)(sizeof(T) * length));
            nint address = (nint)(((nint)(reader + 1)) + position);
            Span<T> span = new((T*)address, (int)length);
            return span;
        }

        public readonly T ReadObject<T>() where T : unmanaged, ISerializable
        {
            T value = default;
            value.Read(this);
            return value;
        }
    }
}