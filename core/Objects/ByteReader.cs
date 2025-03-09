using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Pointer = Unmanaged.Pointers.ByteReader;

namespace Unmanaged
{
    /// <summary>
    /// Reads binary data from a byte stream.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct ByteReader : IDisposable, IEquatable<ByteReader>
    {
        private Pointer* reader;

        /// <summary>
        /// Position of the reader in the byte stream.
        /// </summary>
        public readonly ref int Position
        {
            get
            {
                MemoryAddress.ThrowIfDefault(reader);

                return ref reader->bytePosition;
            }
        }

        /// <summary>
        /// Length of the byte stream.
        /// </summary>
        public readonly int Length
        {
            get
            {
                MemoryAddress.ThrowIfDefault(reader);

                return reader->byteLength;
            }
        }

        /// <summary>
        /// Has this reader been disposed?
        /// </summary>
        public readonly bool IsDisposed => reader is null;

        /// <summary>
        /// Creates a new binary reader from the given <paramref name="bytes"/>.
        /// </summary>
        public ByteReader(Span<byte> bytes, int position = 0)
        {
            ref Pointer reader = ref MemoryAddress.Allocate<Pointer>();
            reader = new(position, MemoryAddress.Allocate(bytes), bytes.Length, true);
            fixed (Pointer* pointer = &reader)
            {
                this.reader = pointer;
            }
        }

        /// <summary>
        /// Creates a new binary reader from the data in the <paramref name="writer"/>.
        /// </summary>
        public ByteReader(ByteWriter writer, int position = 0)
        {
            ref Pointer reader = ref MemoryAddress.Allocate<Pointer>();
            reader = new(position, writer.Items, writer.Position, false);
            fixed (Pointer* pointer = &reader)
            {
                this.reader = pointer;
            }
        }

        /// <summary>
        /// Creates a new binary reader using the data inside the stream.
        /// </summary>
        public ByteReader(Stream stream, int position = 0)
        {
            int byteLength = (int)stream.Length;
            MemoryAddress streamData = MemoryAddress.Allocate(byteLength);
            Span<byte> span = new(streamData.Pointer, byteLength);
            byteLength = stream.Read(span);
            ref Pointer reader = ref MemoryAddress.Allocate<Pointer>();
            reader = new(position, streamData, span.Length, true);
            fixed (Pointer* pointer = &reader)
            {
                this.reader = pointer;
            }
        }

        private ByteReader(void* value)
        {
            reader = (Pointer*)value;
        }

#if NET
        /// <summary>
        /// Creates an empty binary reader.
        /// </summary>
        public ByteReader()
        {
            ref Pointer reader = ref MemoryAddress.Allocate<Pointer>();
            reader = new(0, MemoryAddress.AllocateEmpty(), 0, true);
            fixed (Pointer* pointer = &reader)
            {
                this.reader = pointer;
            }
        }
#endif

        /// <summary>
        /// Disposes the reader and frees the data.
        /// </summary>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(reader);

            if (reader->isOriginal)
            {
                reader->data.Dispose();
            }

            MemoryAddress.Free(ref reader);
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
            MemoryAddress.ThrowIfDefault(reader);

            ref Pointer clone = ref MemoryAddress.Allocate<Pointer>();
            clone = new(reader->bytePosition, MemoryAddress.Allocate(GetBytes()), reader->byteLength, true);
            fixed (Pointer* pointer = &clone)
            {
                return new(pointer);
            }
        }

        /// <summary>
        /// Returns all bytes in the reader.
        /// </summary>
        public readonly Span<byte> GetBytes()
        {
            MemoryAddress.ThrowIfDefault(reader);

            return new(reader->data.Pointer, reader->byteLength);
        }

        /// <summary>
        /// Resets this reader and loads data from the given <paramref name="data"/>.
        /// </summary>
        public readonly void CopyFrom(Span<byte> data)
        {
            MemoryAddress.ThrowIfDefault(reader);

            if (reader->byteLength < data.Length)
            {
                MemoryAddress.Resize(ref reader->data, data.Length);
            }

            reader->bytePosition = 0;
            reader->byteLength = data.Length;
            reader->data.Write(0, data);
        }

        /// <summary>
        /// Returns the remaining bytes.
        /// </summary>
        public readonly Span<byte> GetRemainingBytes()
        {
            MemoryAddress.ThrowIfDefault(reader);

            return GetBytes().Slice(reader->bytePosition);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfReadingPastLength(int position)
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
        public readonly byte PeekUTF8(int bytePosition, out char low, out char high)
        {
            Span<byte> bytes = GetBytes();
            return bytes.GetUTF8Character(bytePosition, out low, out high);
        }

        /// <summary>
        /// Peeks the next UTF-8 character in the stream.
        /// </summary>
        /// <returns>Amount of bytes read.</returns>
        public readonly byte PeekUTF8(out char low, out char high)
        {
            MemoryAddress.ThrowIfDefault(reader);

            return PeekUTF8(reader->bytePosition, out low, out high);
        }

        /// <summary>
        /// Peeks the next <typeparamref name="T"/> value.
        /// </summary>
        public readonly T PeekValue<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(reader);

            return PeekValue<T>(reader->bytePosition);
        }

        /// <summary>
        /// Peeks a <typeparamref name="T"/> value at the specified <paramref name="bytePosition"/>.
        /// </summary>
        public readonly T PeekValue<T>(int bytePosition) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(reader);

            if (bytePosition + sizeof(T) > reader->byteLength)
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
        public readonly void Advance(int byteLength)
        {
            MemoryAddress.ThrowIfDefault(reader);

            int newPosition = reader->bytePosition + byteLength;
            ThrowIfReadingPastLength(newPosition);

            reader->bytePosition = newPosition;
        }

        /// <summary>
        /// Advances the reader by the size of <typeparamref name="T"/> elements.
        /// </summary>
        public readonly void Advance<T>(int elementCount = 1) where T : unmanaged
        {
            Advance(sizeof(T) * elementCount);
        }

        /// <summary>
        /// Reads a span of values from the reader with the specified length
        /// in <typeparamref name="T"/> elements.
        /// </summary>
        public readonly Span<T> ReadSpan<T>(int length) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(reader);

            Span<T> span = PeekSpan<T>(reader->bytePosition, length);
            reader->bytePosition += sizeof(T) * length;
            return span;
        }

        /// <summary>
        /// Peeks a span of values with <paramref name="length"/>.
        /// </summary>
        public readonly Span<T> PeekSpan<T>(int length) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(reader);

            return PeekSpan<T>(reader->bytePosition, length);
        }

        /// <summary>
        /// Reads a span with <paramref name="length"/> starting at the given <paramref name="bytePosition"/>.
        /// </summary>
        public readonly Span<T> PeekSpan<T>(int bytePosition, int length) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(reader);

            int byteLength = sizeof(T) * length;
            ThrowIfReadingPastLength(bytePosition + byteLength);

            void* pointer = reader->data.Pointer + bytePosition;
            return new(pointer, length);
        }

        /// <summary>
        /// Peeks UTF8 bytes as characters into the given <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values read.</returns>
        public readonly int PeekUTF8(int bytePosition, int length, Span<char> destination)
        {
            Span<byte> bytes = GetBytes();
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
        public readonly int ReadUTF8(Span<char> destination)
        {
            MemoryAddress.ThrowIfDefault(reader);

            int start = reader->bytePosition;
            int read = PeekUTF8(start, destination.Length, destination);
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
        public static ByteReader CreateFromUTF8(Span<char> text)
        {
            using ByteWriter writer = new(text.Length);
            writer.WriteUTF8(text);
            return new ByteReader(writer.AsSpan());
        }

        /// <summary>
        /// Creates a new binary reader from the given text.
        /// </summary>
        public static ByteReader CreateFromUTF8(ASCIIText256 text)
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
            using ByteWriter writer = new(text.Length);
            writer.WriteUTF8(text);
            return new ByteReader(writer.AsSpan());
        }

        /// <summary>
        /// Creates an empty binary reader.
        /// </summary>
        public static ByteReader Create()
        {
            ref Pointer reader = ref MemoryAddress.Allocate<Pointer>();
            reader = new(0, MemoryAddress.AllocateEmpty(), 0, true);
            fixed (Pointer* pointer = &reader)
            {
                return new(pointer);
            }
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