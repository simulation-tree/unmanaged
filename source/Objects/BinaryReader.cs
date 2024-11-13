using System;
using System.Diagnostics;
using System.IO;

namespace Unmanaged
{
    /// <summary>
    /// Reads binary data from a byte stream.
    /// </summary>
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

        /// <summary>
        /// Has this reader been disposed?
        /// </summary>
        public readonly bool IsDisposed => UnsafeBinaryReader.IsDisposed(value);

        /// <summary>
        /// Creates a new binary reader from the data in the span.
        /// </summary>
        public BinaryReader(USpan<byte> data, uint position = 0)
        {
            value = UnsafeBinaryReader.Allocate(data, position);
        }

        /// <summary>
        /// Creates a new binary reader using/sharing the data from the writer.
        /// <para>Disposal of the reader instance is still required.</para>
        /// </summary>
        public BinaryReader(BinaryWriter writer)
        {
            value = UnsafeBinaryReader.Allocate(writer.GetBytes());
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

#if NET
        /// <summary>
        /// Creates an empty binary reader.
        /// </summary>
        public BinaryReader()
        {
            this.value = UnsafeBinaryReader.Allocate(Array.Empty<byte>());
        }
#endif

        /// <summary>
        /// Disposes the reader and frees the data.
        /// </summary>
        public void Dispose()
        {
            ThrowIfDisposed();
            UnsafeBinaryReader.Free(ref value);
        }

        /// <summary>
        /// Retrieves the string representation of the reader.
        /// </summary>
        public readonly override string ToString()
        {
            return $"Position: {Position}, Length: {Length}";
        }

        /// <summary>
        /// Returns all bytes in the reader.
        /// </summary>
        public readonly USpan<byte> GetBytes()
        {
            ThrowIfDisposed();
            Allocation allocation = UnsafeBinaryReader.GetData(value);
            return allocation.AsSpan(0, Length);
        }

        /// <summary>
        /// Returns the remaining bytes.
        /// </summary>
        public readonly USpan<byte> GetRemainingBytes()
        {
            return GetBytes().Slice(Position);
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
            USpan<byte> bytes = GetBytes();
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

        /// <summary>
        /// Peeks a <typeparamref name="T"/> value at the specified position.
        /// </summary>
        public readonly T PeekValue<T>(uint position) where T : unmanaged
        {
            if (position + TypeInfo<T>.size > Length)
            {
                return default;
            }

            nint address = UnsafeBinaryReader.GetData(value).Address + (nint)position;
            return *(T*)address;
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
        /// Advances the reader by the specified amount of bytes.
        /// </summary>
        public readonly void Advance(uint size)
        {
            ref uint position = ref UnsafeBinaryReader.GetPositionRef(value);
            ThrowIfReadingPastLength(position + size);
            position += size;
        }

        /// <summary>
        /// Advances the reader by the size of the specified type.
        /// </summary>
        public readonly void Advance<T>(uint length = 1) where T : unmanaged
        {
            Advance(TypeInfo<T>.size * length);
        }

        /// <summary>
        /// Reads a span of values from the reader with the specified length.
        /// </summary>
        public readonly USpan<T> ReadSpan<T>(uint length) where T : unmanaged
        {
            ref uint position = ref UnsafeBinaryReader.GetPositionRef(value);
            USpan<T> span = PeekSpan<T>(Position, length);
            position += TypeInfo<T>.size * length;
            return span;
        }

        /// <summary>
        /// Peeks a span of values from the reader with the specified length.
        /// </summary>
        public readonly USpan<T> PeekSpan<T>(uint length) where T : unmanaged
        {
            return PeekSpan<T>(Position, length);
        }

        /// <summary>
        /// Reads a span starting at the given position in bytes.
        /// </summary>
        public readonly USpan<T> PeekSpan<T>(uint position, uint length) where T : unmanaged
        {
            ThrowIfReadingPastLength(position + TypeInfo<T>.size * length);
            nint address = UnsafeBinaryReader.GetData(value).Address + (nint)position;
            return new(address, length);
        }

        /// <summary>
        /// Peeks UTF8 bytes as characters into the given buffer
        /// </summary>
        public readonly uint PeekUTF8Span(uint position, uint length, USpan<char> buffer)
        {
            USpan<byte> bytes = GetBytes();
            return bytes.PeekUTF8Span(position, length, buffer);
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
        /// Reads UTF8 bytes as characters into the given buffer
        /// until a terminator is found, or no bytes are left.
        /// </summary>
        public readonly uint ReadUTF8Span(USpan<char> buffer)
        {
            ref uint position = ref UnsafeBinaryReader.GetPositionRef(value);
            uint start = position;
            uint read = PeekUTF8Span(start, buffer.Length, buffer);
            position += read;
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
        public static BinaryReader CreateFromUTF8(USpan<char> text)
        {
            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteUTF8Text(text);
            return new BinaryReader(writer.GetBytes());
        }

        /// <summary>
        /// Creates a new binary reader from the given text.
        /// </summary>
        public static BinaryReader CreateFromUTF8(FixedString text)
        {
            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteUTF8Text(text);
            return new BinaryReader(writer.GetBytes());
        }

        /// <summary>
        /// Creates a new binary reader from the given text.
        /// </summary>
        public static BinaryReader CreateFromUTF8(string text)
        {
            using BinaryWriter writer = BinaryWriter.Create();
            writer.WriteUTF8Text(text);
            return new BinaryReader(writer.GetBytes());
        }

        /// <summary>
        /// Creates a new empty binary reader.
        /// </summary>
        public static BinaryReader Create()
        {
            UnsafeBinaryReader* value = UnsafeBinaryReader.Allocate(Array.Empty<byte>());
            return new BinaryReader(value);
        }

        internal unsafe struct UnsafeBinaryReader
        {
            private uint position;
            private readonly bool clone;
            private readonly Allocation data;
            private readonly uint length;

            private UnsafeBinaryReader(uint position, Allocation data, uint length, bool clone)
            {
                this.position = position;
                this.data = data;
                this.length = length;
                this.clone = clone;
            }

            public static Allocation GetData(UnsafeBinaryReader* reader)
            {
                return reader->data;
            }

            public static UnsafeBinaryReader* Allocate(UnsafeBinaryReader* reader, uint position = 0)
            {
                UnsafeBinaryReader* copy = Allocations.Allocate<UnsafeBinaryReader>();
                copy[0] = new(position, reader->data, reader->length, true);
                return copy;
            }

            public static UnsafeBinaryReader* Allocate(BinaryWriter.UnsafeBinaryWriter* writer, uint position = 0)
            {
                UnsafeBinaryReader* copy = Allocations.Allocate<UnsafeBinaryReader>();
                Allocation data = new((Allocation*)BinaryWriter.UnsafeBinaryWriter.GetStartAddress(writer));
                copy[0] = new(position, data, BinaryWriter.UnsafeBinaryWriter.GetPosition(writer), true);
                return copy;
            }

            public static UnsafeBinaryReader* Allocate(USpan<byte> bytes, uint position = 0)
            {
                UnsafeBinaryReader* reader = Allocations.Allocate<UnsafeBinaryReader>();
                reader[0] = new(position, Allocation.Create(bytes), bytes.Length, false);
                return reader;
            }

            public static UnsafeBinaryReader* Allocate(Stream stream, uint position = 0)
            {
                uint bufferLength = (uint)stream.Length + 4;
                using Allocation buffer = new(bufferLength);
                USpan<byte> span = buffer.AsSpan(0, bufferLength);
                uint length = (uint)stream.Read(span.AsSystemSpan());
                USpan<byte> bytes = span.Slice(0, length);
                return Allocate(bytes, position);
            }

            public static bool IsDisposed(UnsafeBinaryReader* reader)
            {
                return reader is null;
            }

            public static ref uint GetPositionRef(UnsafeBinaryReader* reader)
            {
                return ref reader->position;
            }

            public static uint GetLength(UnsafeBinaryReader* reader)
            {
                return reader->length;
            }

            public static void Free(ref UnsafeBinaryReader* reader)
            {
                Allocations.ThrowIfNull(reader);
                if (!reader->clone)
                {
                    reader->data.Dispose();
                }

                Allocations.Free(ref reader);
            }
        }
    }
}