using System;
using System.Diagnostics;
using System.IO;
using Unmanaged.Collections;
using Unmanaged.Serialization.Unsafe;

namespace Unmanaged
{
    public unsafe struct BinaryReader : IDisposable
    {
        private UnsafeBinaryReader* value;

        /// <summary>
        /// Position of the reader in the byte stream.
        /// </summary>
        public readonly uint Position
        {
            get => UnsafeBinaryReader.GetPositionRef(value);
            set => UnsafeBinaryReader.GetPositionRef(this.value) = value;
        }

        /// <summary>
        /// Length of the byte stream.
        /// </summary>
        public readonly uint Length => UnsafeBinaryReader.GetLength(value);

        public readonly bool IsDisposed => UnsafeBinaryReader.IsDisposed(value);

        /// <summary>
        /// Creates a new binary reader from the data in the span.
        /// </summary>
        public BinaryReader(ReadOnlySpan<byte> data, uint position = 0)
        {
            value = UnsafeBinaryReader.Allocate(data, position);
        }

        public BinaryReader(UnmanagedArray<byte> data, uint position = 0)
        {
            value = UnsafeBinaryReader.Allocate(data.AsSpan(), position);
        }

        public BinaryReader(UnmanagedList<byte> data, uint position = 0)
        {
            value = UnsafeBinaryReader.Allocate(data.AsSpan(), position);
        }

        /// <summary>
        /// Creates a new binary reader using/sharing the data from the writer.
        /// <para>Disposal of the reader instance is still required.</para>
        /// </summary>
        public BinaryReader(BinaryWriter writer)
        {
            value = UnsafeBinaryReader.Allocate(writer.AsSpan());
        }

        /// <summary>
        /// Duplicates the reader into a new instance while sharing the data.
        /// <para>
        /// Disposing of this instance won't dispose the original reader, or
        /// the shared data.
        /// </para>
        /// </summary>
        public BinaryReader(BinaryReader reader)
        {
            value = UnsafeBinaryReader.Allocate(reader.value);
        }

        /// <summary>
        /// Creates a new binary reader using the data inside the stream.
        /// </summary>
        public BinaryReader(Stream stream, uint position = 0)
        {
            value = UnsafeBinaryReader.Allocate(stream, position);
        }

        private BinaryReader(UnsafeBinaryReader* value)
        {
            this.value = value;
        }

#if NET5_0_OR_GREATER
        public BinaryReader()
        {
            this.value = UnsafeBinaryReader.Allocate(Array.Empty<byte>());
        }
#endif
        public void Dispose()
        {
            ThrowIfDisposed();
            UnsafeBinaryReader.Free(ref value);
        }

        public override string ToString()
        {
            return $"Position: {Position}, Length: {Length}";
        }

        /// <summary>
        /// Returns all bytes in the reader.
        /// </summary>
        public readonly ReadOnlySpan<byte> AsSpan()
        {
            ThrowIfDisposed();
            Allocation allocation = UnsafeBinaryReader.GetData(value);
            return allocation.AsSpan(0, Length);
        }

        /// <summary>
        /// Returns the remaining bytes.
        /// </summary>
        public readonly ReadOnlySpan<byte> GetRemainingBytes()
        {
            return AsSpan()[(int)Position..];
        }

        [Conditional("DEBUG")]
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

        /// <summary>
        /// Peeks the next UTF-8 character in the stream.
        /// </summary>
        /// <returns>Amount of bytes read.</returns>
        public readonly byte PeekUTF8(uint position, out char low, out char high)
        {
            ReadOnlySpan<byte> bytes = AsSpan();
            return bytes.PeekUTF8(position, out low, out high);
        }

        /// <summary>
        /// Peeks the next UTF-8 character in the stream.
        /// </summary>
        /// <returns>Amount of bytes read.</returns>
        public readonly byte PeekUTF8(out char low, out char high)
        {
            return PeekUTF8(Position, out low, out high);
        }

        /// <summary>
        /// Peeks the next <typeparamref name="T"/> value.
        /// </summary>
        public readonly T PeekValue<T>() where T : unmanaged
        {
            return PeekValue<T>(Position);
        }

        public readonly T PeekValue<T>(uint position) where T : unmanaged
        {
            uint size = (uint)sizeof(T);
            if (position + size > Length)
            {
                return default;
            }

            nint address = UnsafeBinaryReader.GetData(value).Address + (nint)position;
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
            ref uint position = ref UnsafeBinaryReader.GetPositionRef(value);
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
            ref uint position = ref UnsafeBinaryReader.GetPositionRef(value);
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
            nint address = UnsafeBinaryReader.GetData(value).Address + (nint)position;
            Span<T> span = new((T*)address, (int)length);
            return span;
        }

        public readonly int PeekUTF8Span(uint position, uint length, Span<char> buffer)
        {
            ReadOnlySpan<byte> bytes = AsSpan();
            return bytes.PeekUTF8Span(position, length, buffer);
        }

        public readonly byte ReadUTF8(out char low, out char high)
        {
            byte length = PeekUTF8(out low, out high);
            Advance(length);
            return length;
        }

        /// <summary>
        /// Reads UTF8 bytes as characters into the given buffer
        /// until a terminator is found, or no bytes are left.
        /// </summary>
        public readonly int ReadUTF8Span(Span<char> buffer)
        {
            ref uint position = ref UnsafeBinaryReader.GetPositionRef(value);
            uint start = position;
            int read = PeekUTF8Span(start, (uint)buffer.Length, buffer);
            position += (uint)read;
            return read;
        }

        public readonly T ReadObject<T>() where T : unmanaged, ISerializable
        {
            T value = default;
            value.Read(this);
            return value;
        }

        public static BinaryReader CreateFromUTF8(ReadOnlySpan<char> text)
        {
            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteUTF8Span(text);
            return new BinaryReader(writer.AsSpan());
        }

        public static BinaryReader Create()
        {
            UnsafeBinaryReader* value = UnsafeBinaryReader.Allocate(Array.Empty<byte>());
            return new BinaryReader(value);
        }
    }
}