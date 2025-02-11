using System;
using System.Runtime.CompilerServices;

namespace Unmanaged
{
    /// <summary>
    /// Represents a writer that can write binary data.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct BinaryWriter : IDisposable, IEquatable<BinaryWriter>
    {
        private Implementation* value;

        /// <summary>
        /// Indicates whether the writer has been disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// Read position of the writer.
        /// </summary>
        public readonly uint Position
        {
            get => Implementation.GetPosition(value);
            set => Implementation.SetPosition(this.value, value);
        }

        /// <summary>
        /// Native address of the writer.
        /// </summary>
        public readonly nint Address => Implementation.GetStartAddress(value);

        /// <summary>
        /// Creates a new binary writer with the specified capacity.
        /// </summary>
        public BinaryWriter(uint capacity = 4)
        {
            value = Implementation.Allocate(capacity);
        }

        /// <summary>
        /// Creates a new binary writer with the given <paramref name="span"/>
        /// already contained.
        /// <para>
        /// Position of the writer will be at the end of the span.
        /// </para>
        /// </summary>
        public BinaryWriter(USpan<byte> span)
        {
            value = Implementation.Allocate(span);
        }
#if NET
        /// <summary>
        /// Creates an empty binary writer.
        /// </summary>
        public BinaryWriter()
        {
            value = Implementation.Allocate(4);
        }
#endif
        private BinaryWriter(Implementation* value)
        {
            this.value = value;
        }

        /// <summary>
        /// Writes the given value to the writer.
        /// </summary>
        public void WriteValue<T>(T value) where T : unmanaged
        {
            Allocation valueAllocation = Allocation.Get(ref value);
            Implementation.Write(ref this.value, valueAllocation, (uint)sizeof(T));
        }

        /// <summary>
        /// Writes the given span of values to the writer.
        /// </summary>
        public void WriteSpan<T>(USpan<T> span) where T : unmanaged
        {
            Allocation spanAllocation = new(span);
            Implementation.Write(ref value, spanAllocation, span.Length * (uint)sizeof(T));
        }

        /// <summary>
        /// Writes memory from the given pointer with the specified <paramref name="byteLength"/>.
        /// </summary>
        public void Write(Allocation pointer, uint byteLength)
        {
            Implementation.Write(ref value, pointer, byteLength);
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
        public void WriteUTF8(USpan<char> text)
        {
            for (uint i = 0; i < text.Length; i++)
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
        public void WriteUTF8(FixedString text)
        {
            USpan<char> textSpan = stackalloc char[text.Length];
            text.CopyTo(textSpan);
            WriteUTF8(textSpan);
        }

        /// <summary>
        /// Writes only the content of this text, without a terminator.
        /// </summary>
        public void WriteUTF8(string text)
        {
            USpan<char> textSpan = stackalloc char[text.Length];
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
            Implementation.Free(ref value);
        }

        /// <summary>
        /// Resets the position of the reader back to start.
        /// </summary>
        public readonly void Reset()
        {
            Implementation.SetPosition(value, 0);
        }

        /// <summary>
        /// All bytes written into the writer.
        /// </summary>
        public readonly USpan<byte> AsSpan()
        {
            return new((void*)Address, Position);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is BinaryWriter writer && Equals(writer);
        }

        /// <inheritdoc/>
        public readonly bool Equals(BinaryWriter other)
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
            private Allocation items;
            private uint position;
            private uint capacity;

            private Implementation(Allocation items, uint length, uint capacity)
            {
                this.items = items;
                this.position = length;
                this.capacity = capacity;
            }

            public static nint GetStartAddress(Implementation* writer)
            {
                Allocations.ThrowIfNull(writer);

                return writer->items.Address;
            }

            public static Implementation* Allocate(uint initialCapacity)
            {
                initialCapacity = Allocations.GetNextPowerOf2(initialCapacity);
                ref Implementation writer = ref Allocations.Allocate<Implementation>();
                writer = new(new(initialCapacity), 0, initialCapacity);
                fixed (Implementation* pointer = &writer)
                {
                    return pointer;
                }
            }

            public static Implementation* Allocate(USpan<byte> span)
            {
                ref Implementation writer = ref Allocations.Allocate<Implementation>();
                writer = new(Allocation.Create(span), span.Length, span.Length);
                writer.position = span.Length;
                fixed (Implementation* pointer = &writer)
                {
                    return pointer;
                }
            }

            public static void Free(ref Implementation* writer)
            {
                Allocations.ThrowIfNull(writer);

                writer->items.Dispose();
                Allocations.Free(ref writer);
            }

            public static uint GetPosition(Implementation* writer)
            {
                Allocations.ThrowIfNull(writer);

                return writer->position;
            }

            public static void SetPosition(Implementation* writer, uint position)
            {
                Allocations.ThrowIfNull(writer);

                if (position > writer->capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                writer->position = position;
            }

            public static void Write(ref Implementation* writer, Allocation data, uint dataByteLength)
            {
                Allocations.ThrowIfNull(writer);

                uint endPosition = writer->position + dataByteLength;
                uint capacity = writer->capacity;
                if (capacity < endPosition)
                {
                    writer->capacity = Allocations.GetNextPowerOf2(endPosition);
                    Allocation.Resize(ref writer->items, writer->capacity);
                }

                writer->items.Write(writer->position, dataByteLength, data);
                writer->position = endPosition;
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(BinaryWriter left, BinaryWriter right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(BinaryWriter left, BinaryWriter right)
        {
            return !(left == right);
        }
    }
}