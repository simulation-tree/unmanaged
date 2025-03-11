using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Pointer = Unmanaged.Pointers.ByteWriter;

namespace Unmanaged
{
    /// <summary>
    /// Represents a writer that can write binary data.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct ByteWriter : IDisposable, IEquatable<ByteWriter>
    {
        private Pointer* writer;

        /// <summary>
        /// Indicates whether the writer has been disposed.
        /// </summary>
        public readonly bool IsDisposed => writer is null;

        /// <summary>
        /// The current position of the writer in bytes.
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
            ref Pointer writer = ref MemoryAddress.Allocate<Pointer>();
            writer = new(MemoryAddress.Allocate(initialCapacity), 0, initialCapacity);
            fixed (Pointer* pointer = &writer)
            {
                this.writer = pointer;
            }
        }

        /// <summary>
        /// Creates a new binary writer with the given <paramref name="span"/>
        /// already contained.
        /// <para>
        /// Position of the writer will be at the end of the span.
        /// </para>
        /// </summary>
        public ByteWriter(Span<byte> span)
        {
            ref Pointer writer = ref MemoryAddress.Allocate<Pointer>();
            writer = new(MemoryAddress.Allocate(span), span.Length, span.Length);
            writer.bytePosition = span.Length;
            fixed (Pointer* pointer = &writer)
            {
                this.writer = pointer;
            }
        }
#if NET
        /// <summary>
        /// Creates an empty binary writer.
        /// </summary>
        public ByteWriter()
        {
            ref Pointer writer = ref MemoryAddress.Allocate<Pointer>();
            writer = new(MemoryAddress.AllocateEmpty(), 0, 0);
            fixed (Pointer* pointer = &writer)
            {
                this.writer = pointer;
            }
        }
#endif
        private ByteWriter(void* value)
        {
            writer = (Pointer*)value;
        }

        /// <summary>
        /// Writes the given <paramref name="value"/> to the writer.
        /// </summary>
        public readonly void WriteValue<T>(T value) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(writer);

            int endPosition = writer->bytePosition + sizeof(T);
            int capacity = writer->capacity;
            if (capacity < endPosition)
            {
                writer->capacity = endPosition.GetNextPowerOf2();
                MemoryAddress.Resize(ref writer->data, writer->capacity);
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
            int capacity = writer->capacity;
            if (capacity < endPosition)
            {
                writer->capacity = endPosition.GetNextPowerOf2();
                MemoryAddress.Resize(ref writer->data, writer->capacity);
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
            int capacity = writer->capacity;
            if (capacity < endPosition)
            {
                writer->capacity = endPosition.GetNextPowerOf2();
                MemoryAddress.Resize(ref writer->data, writer->capacity);
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
            int capacity = writer->capacity;
            if (capacity < endPosition)
            {
                writer->capacity = endPosition.GetNextPowerOf2();
                MemoryAddress.Resize(ref writer->data, writer->capacity);
            }

            writer->data.Write(writer->bytePosition, byteLength, data);
            writer->bytePosition = endPosition;
        }

        /// <summary>
        /// Writes the given character as a UTF-8 character.
        /// </summary>
        public void WriteUTF8(char value)
        {
            if (value < 0x7F)
            {
                WriteValue((byte)value);
            }
            else if (value < 0x7FF)
            {
                WriteValue((byte)(0xC0 | (value >> 6)));
                WriteValue((byte)(0x80 | (value & 0x3F)));
            }
            else if (value < 0xFFFF)
            {
                WriteValue((byte)(0xE0 | (value >> 12)));
                WriteValue((byte)(0x80 | ((value >> 6) & 0x3F)));
                WriteValue((byte)(0x80 | (value & 0x3F)));
            }
            else
            {
                WriteValue((byte)(0xF0 | (value >> 18)));
                WriteValue((byte)(0x80 | ((value >> 12) & 0x3F)));
                WriteValue((byte)(0x80 | ((value >> 6) & 0x3F)));
                WriteValue((byte)(0x80 | (value & 0x3F)));
            }
        }

        /// <summary>
        /// Writes only the content of this text, without a terminator.
        /// </summary>
        public void WriteUTF8(ReadOnlySpan<char> text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 0x7F)
                {
                    WriteValue((byte)c);
                }
                else if (c < 0x7FF)
                {
                    WriteValue((byte)(0xC0 | (c >> 6)));
                    WriteValue((byte)(0x80 | (c & 0x3F)));
                }
                else if (c < 0xFFFF)
                {
                    WriteValue((byte)(0xE0 | (c >> 12)));
                    WriteValue((byte)(0x80 | ((c >> 6) & 0x3F)));
                    WriteValue((byte)(0x80 | (c & 0x3F)));
                }
                else
                {
                    WriteValue((byte)(0xF0 | (c >> 18)));
                    WriteValue((byte)(0x80 | ((c >> 12) & 0x3F)));
                    WriteValue((byte)(0x80 | ((c >> 6) & 0x3F)));
                    WriteValue((byte)(0x80 | (c & 0x3F)));
                }
            }
        }

        /// <summary>
        /// Writes only the content of this text, without a terminator.
        /// </summary>
        public void WriteUTF8(ASCIIText256 text)
        {
            Span<char> textSpan = stackalloc char[text.Length];
            text.CopyTo(textSpan);
            WriteUTF8(textSpan);
        }

        /// <summary>
        /// Writes only the content of this text, without a terminator.
        /// </summary>
        public void WriteUTF8(string text)
        {
            Span<char> textSpan = stackalloc char[text.Length];
            text.AsSpan().CopyTo(textSpan);
            WriteUTF8(textSpan);
        }

        /// <summary>
        /// Writes the given <see cref="ISerializable"/> object to the writer.
        /// </summary>
        public readonly void WriteObject<T>(T value) where T : unmanaged, ISerializable
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
            if (newPosition > writer->capacity)
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