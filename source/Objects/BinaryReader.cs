using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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

        public readonly int ReadUTF8Span(uint length, Span<char> buffer)
        {
            uint start = Position;
            int read = PeekUTF8Span(start, length, buffer);
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
            Span<byte> buffer = stackalloc byte[text.Length * sizeof(char)];
            int written = Encoding.UTF8.GetBytes(text, buffer);
            return new BinaryReader(buffer[..written]);
        }
    }
}