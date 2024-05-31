using System;
using System.Diagnostics;
using System.IO;
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
        /// Creates a new binary reader using the data inside the span.
        /// </summary>
        public BinaryReader(ReadOnlySpan<byte> data, uint position = 0)
        {
            value = UnsafeBinaryReader.Allocate(data, position);
        }

        /// <summary>
        /// Duplicates the reader into a new instance while sharing the data.
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

        /// <summary>
        /// Creates a new empty reader.
        /// </summary>
        public BinaryReader()
        {
            value = UnsafeBinaryReader.Allocate([]);
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            UnsafeBinaryReader.Free(ref value);
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

        /// <summary>
        /// Peeks the next UTF-8 character in the stream.
        /// </summary>
        /// <returns>Amount of bytes read.</returns>
        public readonly byte PeekUTF8(uint position, out char low, out char high)
        {
            high = default;
            byte firstByte = PeekValue<byte>(position);
            int codePoint;
            byte additional;
            if ((firstByte & 0x80) == 0)
            {
                additional = 0;
                codePoint = firstByte;
            }
            else if ((firstByte & 0xE0) == 0xC0)
            {
                additional = 1;
                codePoint = firstByte & 0x1F;
            }
            else if ((firstByte & 0xF0) == 0xE0)
            {
                additional = 2;
                codePoint = firstByte & 0x0F;
            }
            else if ((firstByte & 0xF8) == 0xF0)
            {
                additional = 3;
                codePoint = firstByte & 0x07;
            }
            else
            {
                throw new InvalidDataException("Invalid UTF-8 byte sequence");
            }

            for (uint j = 1; j <= additional; j++)
            {
                byte next = PeekValue<byte>(position + j);
                if ((next & 0xC0) != 0x80)
                {
                    throw new InvalidDataException("Invalid UTF-8 continuation byte");
                }

                codePoint = (codePoint << 6) | (next & 0x3F);
            }

            if (codePoint <= 0xFFFF)
            {
                low = (char)codePoint;
            }
            else
            {
                codePoint -= 0x10000;
                high = (char)((codePoint >> 10) + 0xD800);
                low = (char)((codePoint & 0x3FF) + 0xDC00);
            }

            additional++;
            return additional;
        }

        /// <summary>
        /// Peeks the next UTF-8 character in the stream.
        /// </summary>
        /// <returns>Amount of bytes read.</returns>
        public readonly byte PeekUTF8(out char low, out char high)
        {
            return PeekUTF8(Position, out low, out high);
        }

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
            uint start = position;
            int t = 0;
            for (int i = 0; i < length; i++)
            {
                uint cLength = PeekUTF8(position, out char low, out char high);
                if (low == default)
                {
                    break;
                }

                if (high != default)
                {
                    buffer[t++] = high;
                    buffer[t++] = low;
                }
                else
                {
                    buffer[t++] = low;
                }

                position += cLength;
            }

            return t;
        }

        public readonly byte ReadUTF8(out char low, out char high)
        {
            byte length = PeekUTF8(out low, out high);
            Position += length;
            return length;
        }

        public readonly int ReadUTF8Span(Span<char> buffer)
        {
            uint start = Position;
            int read = PeekUTF8Span(start, (uint)buffer.Length, buffer);
            Advance((uint)read);
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
            using BinaryWriter writer = new();
            writer.WriteUTF8Span(text);
            return new BinaryReader(writer.AsSpan());
        }
    }
}