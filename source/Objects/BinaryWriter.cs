using System;

namespace Unmanaged
{
    /// <summary>
    /// Represents a writer that can write binary data.
    /// </summary>
    public unsafe struct BinaryWriter : IDisposable, IEquatable<BinaryWriter>
    {
        private UnsafeBinaryWriter* value;

        /// <summary>
        /// Indicates whether the writer has been disposed.
        /// </summary>
        public readonly bool IsDisposed => UnsafeBinaryWriter.IsDisposed(value);

        /// <summary>
        /// Read position of the writer.
        /// </summary>
        public readonly uint Position
        {
            get => UnsafeBinaryWriter.GetPosition(value);
            set => UnsafeBinaryWriter.SetPosition(this.value, value);
        }

        /// <summary>
        /// Native address of the writer.
        /// </summary>
        public readonly nint Address => UnsafeBinaryWriter.GetStartAddress(value);

        /// <summary>
        /// Creates a new binary writer with the specified capacity.
        /// </summary>
        public BinaryWriter(uint capacity = 4)
        {
            value = UnsafeBinaryWriter.Allocate(capacity);
        }

        /// <summary>
        /// Creates a new binary writer with the specified span of bytes.
        /// </summary>
        public BinaryWriter(USpan<byte> span)
        {
            value = UnsafeBinaryWriter.Allocate(span);
        }
#if NET
        /// <summary>
        /// Creates an empty binary writer.
        /// </summary>
        public BinaryWriter()
        {
            value = UnsafeBinaryWriter.Allocate(4);
        }
#endif
        private BinaryWriter(UnsafeBinaryWriter* value)
        {
            this.value = value;
        }

        /// <summary>
        /// Writes the given value to the writer.
        /// </summary>
        public void WriteValue<T>(T value) where T : unmanaged
        {
            T* ptr = &value;
            UnsafeBinaryWriter.Write(ref this.value, ptr, TypeInfo<T>.size);
        }

        /// <summary>
        /// Writes the given span of values to the writer.
        /// </summary>
        public void WriteSpan<T>(USpan<T> span) where T : unmanaged
        {
            UnsafeBinaryWriter.Write(ref value, (void*)span.Address, span.Length * TypeInfo<T>.size);
        }

        /// <summary>
        /// Writes memory from the given pointer with the specified <paramref name="byteLength"/>.
        /// </summary>
        public void Write(void* pointer, uint byteLength)
        {
            UnsafeBinaryWriter.Write(ref value, pointer, byteLength);
        }

        /// <summary>
        /// Writes the given character as a UTF-8 character.
        /// </summary>
        public void WriteUTF8Character(char value)
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
        public void WriteUTF8Text(USpan<char> text)
        {
            foreach (char c in text)
            {
                WriteUTF8Character(c);
            }
        }

        /// <summary>
        /// Writes only the content of this text, without a terminator.
        /// </summary>
        public void WriteUTF8Text(FixedString text)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.Capacity];
            uint length = text.CopyTo(buffer);
            WriteUTF8Text(buffer.Slice(0, length));
        }

        /// <summary>
        /// Writes only the content of this text, without a terminator.
        /// </summary>
        public void WriteUTF8Text(string text)
        {
            USpan<char> buffer = stackalloc char[text.Length];
            text.AsUSpan().CopyTo(buffer);
            WriteUTF8Text(buffer);
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
            UnsafeBinaryWriter.Free(ref value);
        }

        /// <summary>
        /// Resets the position of the reader back to start.
        /// </summary>
        public readonly void Reset()
        {
            UnsafeBinaryWriter.SetPosition(value, 0);
        }

        /// <summary>
        /// All bytes written into the writer.
        /// </summary>
        public readonly USpan<byte> GetBytes()
        {
            return new((void*)Address, Position);
        }

        /// <summary>
        /// Retrieves the writer as a span of the specified type.
        /// </summary>
        public readonly USpan<T> AsSpan<T>() where T : unmanaged
        {
            return new((void*)Address, Position / TypeInfo<T>.size);
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

        internal unsafe struct UnsafeBinaryWriter
        {
            private Allocation items;
            private uint position;
            private uint capacity;

            private UnsafeBinaryWriter(Allocation items, uint length, uint capacity)
            {
                this.items = items;
                this.position = length;
                this.capacity = capacity;
            }

            public static nint GetStartAddress(UnsafeBinaryWriter* writer)
            {
                Allocations.ThrowIfNull(writer);

                return writer->items.Address;
            }

            public static UnsafeBinaryWriter* Allocate(uint initialCapacity)
            {
                initialCapacity = Allocations.GetNextPowerOf2(initialCapacity);
                UnsafeBinaryWriter* ptr = Allocations.Allocate<UnsafeBinaryWriter>();
                ptr[0] = new(new(initialCapacity), 0, initialCapacity);
                return ptr;
            }

            public static UnsafeBinaryWriter* Allocate(USpan<byte> span)
            {
                UnsafeBinaryWriter* ptr = Allocations.Allocate<UnsafeBinaryWriter>();
                ptr[0] = new(Allocation.Create(span), span.Length, span.Length);
                return ptr;
            }

            public static bool IsDisposed(UnsafeBinaryWriter* writer)
            {
                return writer is null;
            }

            public static void Free(ref UnsafeBinaryWriter* writer)
            {
                Allocations.ThrowIfNull(writer);

                writer->items.Dispose();
                Allocations.Free(ref writer);
            }

            public static uint GetPosition(UnsafeBinaryWriter* writer)
            {
                Allocations.ThrowIfNull(writer);

                return writer->position;
            }

            public static void SetPosition(UnsafeBinaryWriter* writer, uint position)
            {
                Allocations.ThrowIfNull(writer);

                if (position > writer->capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                writer->position = position;
            }

            public static void Write(ref UnsafeBinaryWriter* writer, void* data, uint length)
            {
                Allocations.ThrowIfNull(writer);

                uint endPosition = writer->position + length;
                uint capacity = writer->capacity;
                if (capacity < endPosition)
                {
                    while (capacity < endPosition)
                    {
                        capacity *= 2;
                        Allocation.Resize(ref writer->items, capacity);
                    }

                    writer->capacity = capacity;
                }

                writer->items.Write(writer->position, length, data);
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