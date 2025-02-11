﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Unmanaged
{
    /// <summary>
    /// Reads binary data from a byte stream.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct BinaryReader : IDisposable, IEquatable<BinaryReader>
    {
        private Implementation* value;

        /// <summary>
        /// Position of the reader in the byte stream.
        /// </summary>
        public readonly ref uint Position => ref Implementation.GetPositionRef(value);

        /// <summary>
        /// Length of the byte stream.
        /// </summary>
        public readonly uint Length => Implementation.GetLength(value);

        /// <summary>
        /// Has this reader been disposed?
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// Creates a new binary reader from the data in the span.
        /// </summary>
        public BinaryReader(USpan<byte> data, uint position = 0)
        {
            value = Implementation.Allocate(data, position);
        }

        /// <summary>
        /// Creates a new binary reader using/sharing the data from the writer.
        /// <para>Disposal of the reader instance is still required.</para>
        /// </summary>
        public BinaryReader(BinaryWriter writer)
        {
            value = Implementation.Allocate(writer.AsSpan());
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
            value = Implementation.Allocate(reader.value);
        }

        /// <summary>
        /// Creates a new binary reader using the data inside the stream.
        /// </summary>
        public BinaryReader(Stream stream, uint position = 0)
        {
            value = Implementation.Allocate(stream, position);
        }

        private BinaryReader(Implementation* value)
        {
            this.value = value;
        }

#if NET
        /// <summary>
        /// Creates an empty binary reader.
        /// </summary>
        public BinaryReader()
        {
            this.value = Implementation.Allocate(Array.Empty<byte>());
        }
#endif

        /// <summary>
        /// Disposes the reader and frees the data.
        /// </summary>
        public void Dispose()
        {
            ThrowIfDisposed();
            Implementation.Free(ref value);
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
            Allocation allocation = Implementation.GetData(value);
            return allocation.AsSpan(0, Length);
        }

        /// <summary>
        /// Resets this reader and loads data from the given <paramref name="data"/>.
        /// </summary>
        public readonly void CopyFrom(USpan<byte> data)
        {
            Position = 0;
            Implementation.CopyFrom(value, data);
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
            if (position + (uint)sizeof(T) > Length)
            {
                return default;
            }

            nint address = Implementation.GetData(value).Address + (nint)position;
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
            ref uint position = ref Implementation.GetPositionRef(value);
            ThrowIfReadingPastLength(position + size);
            position += size;
        }

        /// <summary>
        /// Advances the reader by the size of the specified type.
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
            ref uint position = ref Implementation.GetPositionRef(value);
            USpan<T> span = PeekSpan<T>(Position, length);
            position += (uint)sizeof(T) * length;
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
            ThrowIfReadingPastLength(position + (uint)sizeof(T) * length);
            nint address = Implementation.GetData(value).Address + (nint)position;
            return new(address, length);
        }

        /// <summary>
        /// Peeks UTF8 bytes as characters into the given buffer
        /// </summary>
        public readonly uint PeekUTF8(uint position, uint length, USpan<char> buffer)
        {
            USpan<byte> bytes = GetBytes();
            return bytes.PeekUTF8(position, length, buffer);
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
        public readonly uint ReadUTF8(USpan<char> buffer)
        {
            ref uint position = ref Implementation.GetPositionRef(value);
            uint start = position;
            uint read = PeekUTF8(start, buffer.Length, buffer);
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
            using BinaryWriter writer = new(text.Length);
            writer.WriteUTF8(text);
            return new BinaryReader(writer.AsSpan());
        }

        /// <summary>
        /// Creates a new binary reader from the given text.
        /// </summary>
        public static BinaryReader CreateFromUTF8(FixedString text)
        {
            using BinaryWriter writer = new(text.Length);
            writer.WriteUTF8(text);
            return new BinaryReader(writer.AsSpan());
        }

        /// <summary>
        /// Creates a new binary reader from the given text.
        /// </summary>
        public static BinaryReader CreateFromUTF8(string text)
        {
            using BinaryWriter writer = new((uint)text.Length);
            writer.WriteUTF8(text);
            return new BinaryReader(writer.AsSpan());
        }

        /// <summary>
        /// Creates an empty binary reader.
        /// </summary>
        public static BinaryReader Create()
        {
            USpan<byte> emptyBytes = stackalloc byte[0];
            return new BinaryReader(Implementation.Allocate(emptyBytes));
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is BinaryReader reader && Equals(reader);
        }

        /// <inheritdoc/>
        public readonly bool Equals(BinaryReader other)
        {
            return value == other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        internal unsafe struct Implementation
        {
            private uint position;
            private Allocation data;
            private uint length;
            private readonly bool clone;

            private Implementation(uint position, Allocation data, uint length, bool clone)
            {
                this.position = position;
                this.data = data;
                this.length = length;
                this.clone = clone;
            }

            public static Allocation GetData(Implementation* reader)
            {
                Allocations.ThrowIfNull(reader);

                return reader->data;
            }

            public static Implementation* Allocate(Implementation* reader, uint position = 0)
            {
                ref Implementation copy = ref Allocations.Allocate<Implementation>();
                copy = new(position, reader->data, reader->length, true);
                fixed (Implementation* pointer = &copy)
                {
                    return pointer;
                }
            }

            public static Implementation* Allocate(BinaryWriter.Implementation* writer, uint position = 0)
            {
                ref Implementation copy = ref Allocations.Allocate<Implementation>();
                Allocation data = new((Allocation*)BinaryWriter.Implementation.GetStartAddress(writer));
                copy = new(position, data, BinaryWriter.Implementation.GetPosition(writer), true);
                fixed (Implementation* pointer = &copy)
                {
                    return pointer;
                }
            }

            public static Implementation* Allocate(USpan<byte> bytes, uint position = 0)
            {
                ref Implementation reader = ref Allocations.Allocate<Implementation>();
                reader = new(position, Allocation.Create(bytes), bytes.Length, false);
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

            public static ref uint GetPositionRef(Implementation* reader)
            {
                Allocations.ThrowIfNull(reader);

                return ref reader->position;
            }

            public static uint GetLength(Implementation* reader)
            {
                Allocations.ThrowIfNull(reader);

                return reader->length;
            }

            public static void Free(ref Implementation* reader)
            {
                Allocations.ThrowIfNull(reader);

                if (!reader->clone)
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
        public static bool operator ==(BinaryReader left, BinaryReader right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(BinaryReader left, BinaryReader right)
        {
            return !(left == right);
        }
    }
}