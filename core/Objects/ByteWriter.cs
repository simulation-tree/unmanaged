using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged.Pointers;

namespace Unmanaged
{
    /// <summary>
    /// Represents a writer that can write binary data.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct ByteWriter : IDisposable, IEquatable<ByteWriter>
    {
        private ByteWriterPointer* writer;

        /// <summary>
        /// Indicates whether the writer has been disposed.
        /// </summary>
        public readonly bool IsDisposed => writer is null;

        /// <summary>
        /// The current position in the writer in <see cref="byte"/>s.
        /// </summary>
        public readonly int Position
        {
            get
            {
                MemoryAddress.ThrowIfDefault(writer);

                return writer->bytePosition;
            }
            set
            {
                MemoryAddress.ThrowIfDefault(writer);
                ThrowIfPositionPastCapacity(value);

                writer->bytePosition = value;
            }
        }

        /// <summary>
        /// The underlying memory allocation of the writer containg all of the bytes.
        /// </summary>
        public readonly MemoryAddress Items
        {
            get
            {
                MemoryAddress.ThrowIfDefault(writer);

                return writer->data;
            }
        }

        /// <summary>
        /// Creates a new binary writer with the specified <paramref name="initialCapacity"/>.
        /// </summary>
        public ByteWriter(int initialCapacity = 4)
        {
            initialCapacity = initialCapacity.GetNextPowerOf2();
            writer = MemoryAddress.AllocatePointer<ByteWriterPointer>();
            writer->bytePosition = 0;
            writer->byteCapacity = initialCapacity;
            writer->data = MemoryAddress.Allocate(initialCapacity);
        }

        /// <summary>
        /// Creates a new binary writer with the given <paramref name="span"/>
        /// already contained.
        /// <para>
        /// Position of the writer will be at the end of the span.
        /// </para>
        /// </summary>
        public ByteWriter(ReadOnlySpan<byte> span)
        {
            int initialCapacity = span.Length.GetNextPowerOf2();
            writer = MemoryAddress.AllocatePointer<ByteWriterPointer>();
            writer->bytePosition = span.Length;
            writer->byteCapacity = initialCapacity;
            writer->data = MemoryAddress.Allocate(initialCapacity);
            writer->data.Write(0, span);
        }

#if NET
        /// <summary>
        /// Creates an empty binary writer.
        /// </summary>
        public ByteWriter()
        {
            writer = MemoryAddress.AllocatePointer<ByteWriterPointer>();
            writer->bytePosition = 0;
            writer->byteCapacity = 4;
            writer->data = MemoryAddress.Allocate(4);
        }
#endif

        /// <summary>
        /// Initializes an existing writer from the given <paramref name="pointer"/>.
        /// </summary>
        public ByteWriter(void* pointer)
        {
            writer = (ByteWriterPointer*)pointer;
        }

        /// <summary>
        /// Writes the given <paramref name="value"/> to the writer.
        /// </summary>
        public readonly void WriteValue<T>(T value) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(writer);

            int endPosition = writer->bytePosition + sizeof(T);
            if (writer->byteCapacity < endPosition)
            {
                writer->byteCapacity = endPosition.GetNextPowerOf2();
                MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
            }

            writer->data.Write(writer->bytePosition, value);
            writer->bytePosition = endPosition;
        }

        /// <summary>
        /// Writes the given <paramref name="span"/> of values to the writer.
        /// </summary>
        public readonly void WriteSpan<T>(Span<T> span) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(writer);

            int endPosition = writer->bytePosition + sizeof(T) * span.Length;
            if (writer->byteCapacity < endPosition)
            {
                writer->byteCapacity = endPosition.GetNextPowerOf2();
                MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
            }

            writer->data.Write(writer->bytePosition, span);
            writer->bytePosition = endPosition;
        }

        /// <summary>
        /// Writes the given <paramref name="span"/> of values to the writer.
        /// </summary>
        public readonly void WriteSpan<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(writer);

            int endPosition = writer->bytePosition + sizeof(T) * span.Length;
            if (writer->byteCapacity < endPosition)
            {
                writer->byteCapacity = endPosition.GetNextPowerOf2();
                MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
            }

            writer->data.Write(writer->bytePosition, span);
            writer->bytePosition = endPosition;
        }

        /// <summary>
        /// Writes memory from the given <paramref name="data"/> with a specified <paramref name="byteLength"/>.
        /// </summary>
        public readonly void Write(MemoryAddress data, int byteLength)
        {
            MemoryAddress.ThrowIfDefault(writer);

            int endPosition = writer->bytePosition + byteLength;
            if (writer->byteCapacity < endPosition)
            {
                writer->byteCapacity = endPosition.GetNextPowerOf2();
                MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
            }

            writer->data.Write(writer->bytePosition, byteLength, data);
            writer->bytePosition = endPosition;
        }

        /// <summary>
        /// Writes the given <paramref name="character"/> as a UTF-8 character.
        /// </summary>
        public readonly void WriteUTF8(char character)
        {
            MemoryAddress.ThrowIfDefault(writer);

            int bytePosition = writer->bytePosition;
            int endPosition;
            if (character < 0x7F)
            {
                endPosition = bytePosition + 1;
                if (writer->byteCapacity < endPosition)
                {
                    writer->byteCapacity *= 2;
                    MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
                }

                writer->data.Write(bytePosition, (byte)character);
            }
            else if (character < 0x7FF)
            {
                endPosition = bytePosition + 2;
                if (writer->byteCapacity < endPosition)
                {
                    writer->byteCapacity = endPosition.GetNextPowerOf2();
                    MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
                }

                Span<byte> buffer = writer->data.GetSpan(endPosition);
                buffer[bytePosition + 0] = (byte)(0xC0 | (character >> 6));
                buffer[bytePosition + 1] = (byte)(0x80 | (character & 0x3F));
            }
            else if (character < 0xFFFF)
            {
                endPosition = bytePosition + 3;
                if (writer->byteCapacity < endPosition)
                {
                    writer->byteCapacity = endPosition.GetNextPowerOf2();
                    MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
                }

                Span<byte> buffer = writer->data.GetSpan(endPosition);
                buffer[bytePosition + 0] = (byte)(0xE0 | (character >> 12));
                buffer[bytePosition + 1] = (byte)(0x80 | ((character >> 6) & 0x3F));
                buffer[bytePosition + 2] = (byte)(0x80 | (character & 0x3F));
            }
            else
            {
                endPosition = bytePosition + 4;
                if (writer->byteCapacity < endPosition)
                {
                    writer->byteCapacity = endPosition.GetNextPowerOf2();
                    MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
                }

                Span<byte> buffer = writer->data.GetSpan(endPosition);
                buffer[bytePosition + 0] = (byte)(0xF0 | (character >> 18));
                buffer[bytePosition + 1] = (byte)(0x80 | ((character >> 12) & 0x3F));
                buffer[bytePosition + 2] = (byte)(0x80 | ((character >> 6) & 0x3F));
                buffer[bytePosition + 3] = (byte)(0x80 | (character & 0x3F));
            }

            writer->bytePosition = endPosition;
        }

        /// <summary>
        /// Writes the given <paramref name="character"/> as a UTF-8 character,
        /// <paramref name="repeat"/> amount of times.
        /// </summary>
        public readonly void WriteUTF8(char character, int repeat)
        {
            MemoryAddress.ThrowIfDefault(writer);

            int bytePosition = writer->bytePosition;
            if (character < 0x7F)
            {
                int endPosition = bytePosition + repeat;
                if (writer->byteCapacity < endPosition)
                {
                    writer->byteCapacity = endPosition.GetNextPowerOf2();
                    MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
                }

                Span<byte> buffer = writer->data.GetSpan(endPosition);
                for (int i = 0; i < repeat; i++)
                {
                    buffer[bytePosition++] = (byte)character;
                }
            }
            else if (character < 0x7FF)
            {
                int endPosition = bytePosition + 2 * repeat;
                if (writer->byteCapacity < endPosition)
                {
                    writer->byteCapacity = endPosition.GetNextPowerOf2();
                    MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
                }

                Span<byte> buffer = writer->data.GetSpan(endPosition);
                for (int i = 0; i < repeat; i++)
                {
                    buffer[bytePosition + 0] = (byte)(0xC0 | (character >> 6));
                    buffer[bytePosition + 1] = (byte)(0x80 | (character & 0x3F));
                    bytePosition += 2;
                }
            }
            else if (character < 0xFFFF)
            {
                int endPosition = bytePosition + 3 * repeat;
                if (writer->byteCapacity < endPosition)
                {
                    writer->byteCapacity = endPosition.GetNextPowerOf2();
                    MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
                }

                Span<byte> buffer = writer->data.GetSpan(endPosition);
                for (int i = 0; i < repeat; i++)
                {
                    buffer[bytePosition + 0] = (byte)(0xE0 | (character >> 12));
                    buffer[bytePosition + 1] = (byte)(0x80 | ((character >> 6) & 0x3F));
                    buffer[bytePosition + 2] = (byte)(0x80 | (character & 0x3F));
                    bytePosition += 3;
                }
            }
            else
            {
                int endPosition = bytePosition + 4 * repeat;
                if (writer->byteCapacity < endPosition)
                {
                    writer->byteCapacity = endPosition.GetNextPowerOf2();
                    MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
                }

                Span<byte> buffer = writer->data.GetSpan(endPosition);
                for (int i = 0; i < repeat; i++)
                {
                    buffer[bytePosition + 0] = (byte)(0xF0 | (character >> 18));
                    buffer[bytePosition + 1] = (byte)(0x80 | ((character >> 12) & 0x3F));
                    buffer[bytePosition + 2] = (byte)(0x80 | ((character >> 6) & 0x3F));
                    buffer[bytePosition + 3] = (byte)(0x80 | (character & 0x3F));
                    bytePosition += 4;
                }
            }

            writer->bytePosition = bytePosition;
        }

        /// <summary>
        /// Writes only the content of this <paramref name="text"/>, without a terminator.
        /// </summary>
        public readonly void WriteUTF8(ReadOnlySpan<char> text)
        {
            MemoryAddress.ThrowIfDefault(writer);

            int bytePosition = writer->bytePosition;
            int endPosition = bytePosition + (4 * text.Length);
            if (writer->byteCapacity < endPosition)
            {
                writer->byteCapacity = endPosition.GetNextPowerOf2();
                MemoryAddress.Resize(ref writer->data, writer->byteCapacity);
            }

            Span<byte> buffer = writer->data.GetSpan(endPosition);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 0x7F)
                {
                    buffer[bytePosition++] = (byte)c;
                }
                else if (c < 0x7FF)
                {
                    buffer[bytePosition + 0] = (byte)(0xC0 | (c >> 6));
                    buffer[bytePosition + 1] = (byte)(0x80 | (c & 0x3F));
                    bytePosition += 2;
                }
                else if (c < 0xFFFF)
                {
                    buffer[bytePosition + 0] = (byte)(0xE0 | (c >> 12));
                    buffer[bytePosition + 1] = (byte)(0x80 | ((c >> 6) & 0x3F));
                    buffer[bytePosition + 2] = (byte)(0x80 | (c & 0x3F));
                    bytePosition += 3;
                }
                else
                {
                    buffer[bytePosition + 0] = (byte)(0xF0 | (c >> 18));
                    buffer[bytePosition + 1] = (byte)(0x80 | ((c >> 12) & 0x3F));
                    buffer[bytePosition + 2] = (byte)(0x80 | ((c >> 6) & 0x3F));
                    buffer[bytePosition + 3] = (byte)(0x80 | (c & 0x3F));
                    bytePosition += 4;
                }
            }

            writer->bytePosition = bytePosition;
        }

        /// <summary>
        /// Writes only the content of this <paramref name="text"/>, without a terminator.
        /// </summary>
        public readonly void WriteUTF8(ASCIIText256 text)
        {
            Span<char> textSpan = stackalloc char[text.Length];
            text.CopyTo(textSpan);
            WriteUTF8(textSpan);
        }

        /// <summary>
        /// Writes the given <see cref="ISerializable"/> <paramref name="value"/> to the writer.
        /// </summary>
        public readonly void WriteObject<T>(T value) where T : ISerializable
        {
            value.Write(this);
        }

        /// <summary>
        /// Writes the given <see cref="ISerializable"/> <paramref name="value"/> to the writer.
        /// </summary>
        public readonly void WriteObject(ISerializable value)
        {
            value.Write(this);
        }

        /// <summary>
        /// Disposes the writer and frees all resources.
        /// </summary>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(writer);

            writer->data.Dispose();
            MemoryAddress.Free(ref writer);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfPositionPastCapacity(int newPosition)
        {
            if (newPosition > writer->byteCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(newPosition));
            }
        }

        /// <summary>
        /// Resets the position of the reader back to start.
        /// </summary>
        public readonly void Reset()
        {
            MemoryAddress.ThrowIfDefault(writer);

            writer->bytePosition = 0;
        }

        /// <summary>
        /// All bytes written into the writer.
        /// </summary>
        public readonly Span<byte> AsSpan()
        {
            MemoryAddress.ThrowIfDefault(writer);

            return new(writer->data.Pointer, writer->bytePosition);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is ByteWriter writer && Equals(writer);
        }

        /// <inheritdoc/>
        public readonly bool Equals(ByteWriter other)
        {
            return writer == other.writer;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)writer).GetHashCode();
        }

        /// <summary>
        /// Creates a new empty writer.
        /// </summary>
        /// <returns></returns>
        public static ByteWriter Create()
        {
            ByteWriterPointer* writer = MemoryAddress.AllocatePointer<ByteWriterPointer>();
            writer->bytePosition = 0;
            writer->byteCapacity = 4;
            writer->data = MemoryAddress.Allocate(4);
            return new(writer);
        }

        /// <inheritdoc/>
        public static bool operator ==(ByteWriter left, ByteWriter right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ByteWriter left, ByteWriter right)
        {
            return !(left == right);
        }
    }
}