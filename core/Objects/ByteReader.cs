using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Unmanaged
{
    /// <summary>
    /// Reads binary data from a byte stream.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct ByteReader : IDisposable, IEquatable<ByteReader>
    {
        private Implementation* reader;

        /// <summary>
        /// Position of the reader in the byte stream.
        /// </summary>
        public readonly ref uint Position
        {
            get
            {
                Allocations.ThrowIfNull(reader);

                return ref reader->bytePosition;
            }
        }

        /// <summary>
        /// Length of the byte stream.
        /// </summary>
        public readonly uint Length
        {
            get
            {
                Allocations.ThrowIfNull(reader);

                return reader->length;
            }
        }

        /// <summary>
        /// Has this reader been disposed?
        /// </summary>
        public readonly bool IsDisposed => reader is null;

        /// <summary>
        /// Creates a new binary reader from the given <paramref name="data"/>.
        /// </summary>
        public ByteReader(USpan<byte> data, uint position = 0)
        {
            reader = Implementation.Allocate(data, position);
        }

        /// <summary>
        /// Creates a new binary reader from the data in the <paramref name="writer"/>.
        /// </summary>
        public ByteReader(ByteWriter writer)
        {
            reader = Implementation.Allocate(writer.AsSpan());
        }

        /// <summary>
        /// Creates a new binary reader using the data inside the stream.
        /// </summary>
        public ByteReader(Stream stream, uint position = 0)
        {
            reader = Implementation.Allocate(stream, position);
        }

        private ByteReader(Implementation* value)
        {
            this.reader = value;
        }

#if NET
        /// <summary>
        /// Creates an empty binary reader.
        /// </summary>
        public ByteReader()
        {
            this.reader = Implementation.Allocate(Array.Empty<byte>());
        }
#endif

        /// <summary>
        /// Disposes the reader and frees the data.
        /// </summary>
        public void Dispose()
        {
            Implementation.Free(ref reader);
        }

        /// <summary>
        /// Retrieves the string representation of the reader.
        /// </summary>
        public readonly override string ToString()
        {
            return $"Position: {Position}, Length: {Length}";
        }

        /// <summary>
        /// Clones this reader.
        /// </summary>
        public readonly ByteReader Clone()
        {
            Allocations.ThrowIfNull(reader);

            return new(Implementation.Allocate(reader));
        }

        /// <summary>
        /// Returns all bytes in the reader.
        /// </summary>
        public readonly USpan<byte> GetBytes()
        {
            Allocations.ThrowIfNull(reader);

            return reader->data.AsSpan(0, Length);
        }

        /// <summary>
        /// Resets this reader and loads data from the given <paramref name="data"/>.
        /// </summary>
        public readonly void CopyFrom(USpan<byte> data)
        {
            Allocations.ThrowIfNull(reader);

            if (reader->length < data.Length)
            {
                Allocation.Resize(ref reader->data, data.Length);
            }

            reader->bytePosition = 0;
            reader->length = data.Length;
            reader->data.Write(0, data);
        }

        /// <summary>
        /// Returns the remaining bytes.
        /// </summary>
        public readonly USpan<byte> GetRemainingBytes()
        {
            Allocations.ThrowIfNull(reader);

            return GetBytes().Slice(reader->bytePosition);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfReadingPastLength(uint position)
        {
            if (position > Length)
            {
                throw new InvalidOperationException($"Position {position} is out of range {Length}");
            }
        }

        /// <summary>
        /// Peeks the next UTF-8 character in the stream.
        /// </summary>
        /// <returns>Amount of bytes read.</returns>
        public readonly byte PeekUTF8(uint position, out char low, out char high)
        {
            USpan<byte> bytes = GetBytes();
            return bytes.GetUTF8Character(position, out low, out high);
        }

        /// <summary>
        /// Peeks the next UTF-8 character in the stream.
        /// </summary>
        /// <returns>Amount of bytes read.</returns>
        public readonly byte PeekUTF8(out char low, out char high)
        {
            Allocations.ThrowIfNull(reader);

            return PeekUTF8(reader->bytePosition, out low, out high);
        }

        /// <summary>
        /// Peeks the next <typeparamref name="T"/> value.
        /// </summary>
        public readonly T PeekValue<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(reader);

            return PeekValue<T>(reader->bytePosition);
        }

        /// <summary>
        /// Peeks a <typeparamref name="T"/> value at the specified <paramref name="bytePosition"/>.
        /// </summary>
        public readonly T PeekValue<T>(uint bytePosition) where T : unmanaged
        {
            Allocations.ThrowIfNull(reader);

            if (bytePosition + (uint)sizeof(T) > reader->length)
            {
                return default;
            }

            return reader->data.Read<T>(bytePosition);
        }

        /// <summary>
        /// Reads a <typeparamref name="T"/> value at the current position and
        /// advances forward.
        /// </summary>
        public readonly T ReadValue<T>() where T : unmanaged
        {
            T value = PeekValue<T>();
            Advance<T>();
            return value;
        }

        /// <summary>
        /// Advances the reader by the specified amount of <paramref name="byteLength"/>.
        /// </summary>
        public readonly void Advance(uint byteLength)
        {
            Allocations.ThrowIfNull(reader);

            uint newPosition = reader->bytePosition + byteLength;
            ThrowIfReadingPastLength(newPosition);

            reader->bytePosition = newPosition;
        }

        /// <summary>
        /// Advances the reader by the size of <typeparamref name="T"/> elements.
        /// </summary>
        public readonly void Advance<T>(uint length = 1) where T : unmanaged
        {
            Advance((uint)sizeof(T) * length);
        }

        /// <summary>
        /// Reads a span of values from the reader with the specified length
        /// in <typeparamref name="T"/> elements.
        /// </summary>
        public readonly USpan<T> ReadSpan<T>(uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(reader);

            USpan<T> span = PeekSpan<T>(reader->bytePosition, length);
            reader->bytePosition += (uint)sizeof(T) * length;
            return span;
        }

        /// <summary>
        /// Peeks a span of values with <paramref name="length"/>.
        /// </summary>
        public readonly USpan<T> PeekSpan<T>(uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(reader);

            return PeekSpan<T>(reader->bytePosition, length);
        }

        /// <summary>
        /// Reads a span with <paramref name="length"/> starting at the given <paramref name="bytePosition"/>.
        /// </summary>
        public readonly USpan<T> PeekSpan<T>(uint bytePosition, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(reader);

            uint byteLength = (uint)sizeof(T) * length;
            ThrowIfReadingPastLength(bytePosition + byteLength);

            nint address = reader->data.Address + (nint)bytePosition;
            return new(address, length);
        }

        /// <summary>
        /// Peeks UTF8 bytes as characters into the given <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values read.</returns>
        public readonly uint PeekUTF8(uint bytePosition, uint length, USpan<char> destination)
        {
            USpan<byte> bytes = GetBytes();
            return bytes.GetUTF8Characters(bytePosition, length, destination);
        }

        /// <summary>
        /// Peeks a UTF8 character from the stream.
        /// </summary>
        public readonly byte ReadUTF8(out char low, out char high)
        {
            byte length = PeekUTF8(out low, out high);
            Advance(length);
            return length;
        }

        /// <summary>
        /// Reads UTF8 bytes as characters into the given <paramref name="destination"/>
        /// until a terminator is found, or no bytes are left.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values read.</returns>
        public readonly uint ReadUTF8(USpan<char> destination)
        {
            Allocations.ThrowIfNull(reader);

            uint start = reader->bytePosition;
            uint read = PeekUTF8(start, destination.Length, destination);
            reader->bytePosition += read;
            return read;
        }

        /// <summary>
        /// Reads a <see cref="ISerializable"/> object and advances the reader forward.
        /// </summary>
        public readonly T ReadObject<T>() where T : unmanaged, ISerializable
        {
            T value = default;
            value.Read(this);
            return value;
        }

        /// <summary>
        /// Creates a new binary reader from the given text.
        /// </summary>
        public static ByteReader CreateFromUTF8(USpan<char> text)
        {
            using ByteWriter writer = new(text.Length);
            writer.WriteUTF8(text);
            return new ByteReader(writer.AsSpan());
        }

        /// <summary>
        /// Creates a new binary reader from the given text.
        /// </summary>
        public static ByteReader CreateFromUTF8(FixedString text)
        {
            using ByteWriter writer = new(text.Length);
            writer.WriteUTF8(text);
            return new ByteReader(writer.AsSpan());
        }

        /// <summary>
        /// Creates a new binary reader from the given text.
        /// </summary>
        public static ByteReader CreateFromUTF8(string text)
        {
            using ByteWriter writer = new((uint)text.Length);
            writer.WriteUTF8(text);
            return new ByteReader(writer.AsSpan());
        }

        /// <summary>
        /// Creates an empty binary reader.
        /// </summary>
        public static ByteReader Create()
        {
            USpan<byte> emptyBytes = stackalloc byte[0];
            return new ByteReader(Implementation.Allocate(emptyBytes));
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is ByteReader reader && Equals(reader);
        }

        /// <inheritdoc/>
        public readonly bool Equals(ByteReader other)
        {
            return reader == other.reader;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)reader).GetHashCode();
        }

        internal unsafe struct Implementation
        {
            public readonly bool dataIsOwned;

            internal uint bytePosition;
            internal Allocation data;
            internal uint length;

            private Implementation(uint position, Allocation data, uint length, bool dataIsOwned)
            {
                this.bytePosition = position;
                this.data = data;
                this.length = length;
                this.dataIsOwned = dataIsOwned;
            }

            public static Implementation* Allocate(Implementation* reader, uint position = 0)
            {
                ref Implementation copy = ref Allocations.Allocate<Implementation>();
                copy = new(position, reader->data, reader->length, false);
                fixed (Implementation* pointer = &copy)
                {
                    return pointer;
                }
            }

            public static Implementation* Allocate(ByteWriter.Implementation* writer, uint position = 0)
            {
                ref Implementation copy = ref Allocations.Allocate<Implementation>();
                copy = new(position, writer->data, writer->bytePosition, false);
                fixed (Implementation* pointer = &copy)
                {
                    return pointer;
                }
            }

            public static Implementation* Allocate(USpan<byte> bytes, uint position = 0)
            {
                ref Implementation reader = ref Allocations.Allocate<Implementation>();
                reader = new(position, Allocation.Create(bytes), bytes.Length, true);
                fixed (Implementation* pointer = &reader)
                {
                    return pointer;
                }
            }

            public static Implementation* Allocate(Stream stream, uint position = 0)
            {
                uint bufferLength = (uint)stream.Length + 4;
                using Allocation buffer = new(bufferLength);
                USpan<byte> span = buffer.AsSpan(0, bufferLength);
                uint length = (uint)stream.Read(span);
                USpan<byte> bytes = span.Slice(0, length);
                return Allocate(bytes, position);
            }

            public static void Free(ref Implementation* reader)
            {
                Allocations.ThrowIfNull(reader);

                if (reader->dataIsOwned)
                {
                    reader->data.Dispose();
                }

                Allocations.Free(ref reader);
            }

            public static void CopyFrom(Implementation* reader, USpan<byte> data)
            {
                Allocations.ThrowIfNull(reader);

                if (reader->length < data.Length)
                {
                    Allocation.Resize(ref reader->data, data.Length);
                }

                reader->length = data.Length;
                reader->data.Write(0, data);
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(ByteReader left, ByteReader right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ByteReader left, ByteReader right)
        {
            return !(left == right);
        }
    }
}