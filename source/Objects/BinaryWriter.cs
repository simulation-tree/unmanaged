using System;
using Unmanaged.Serialization.Unsafe;

namespace Unmanaged
{
    public unsafe struct BinaryWriter : IDisposable
    {
        private UnsafeBinaryWriter* value;

        public readonly bool IsDisposed => UnsafeBinaryWriter.IsDisposed(value);

        /// <summary>
        /// Read position of the writer.
        /// </summary>
        public readonly uint Position
        {
            get => UnsafeBinaryWriter.GetPosition(value);
            set => UnsafeBinaryWriter.SetPosition(this.value, value);
        }

        public readonly nint Address => UnsafeBinaryWriter.GetStartAddress(value);

        public BinaryWriter(uint capacity = 1)
        {
            value = UnsafeBinaryWriter.Allocate(capacity);
        }

        public BinaryWriter(Span<byte> span)
        {
            value = UnsafeBinaryWriter.Allocate(span);
        }

        private BinaryWriter(UnsafeBinaryWriter* value)
        {
            this.value = value;
        }

#if NET5_0_OR_GREATER
        public BinaryWriter()
        {
            value = UnsafeBinaryWriter.Allocate();
        }
#endif
        public void WriteValue<T>(T value) where T : unmanaged
        {
            T* ptr = &value;
            UnsafeBinaryWriter.Write(ref this.value, ptr, (uint)sizeof(T));
        }

        public void WriteSpan<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            fixed (T* ptr = span)
            {
                UnsafeBinaryWriter.Write(ref this.value, ptr, (uint)(span.Length * sizeof(T)));
            }
        }

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
        public void WriteUTF8Span(ReadOnlySpan<char> text)
        {
            foreach (char c in text)
            {
                WriteUTF8(c);
            }
        }

        /// <summary>
        /// Writes only the content of this text, without a terminator.
        /// </summary>
        public void WriteUTF8Span(FixedString text)
        {
            Span<char> buffer = stackalloc char[FixedString.MaxLength];
            int length = text.ToString(buffer);
            WriteUTF8Span(buffer[..length]);
        }

        public void WriteObject<T>(T value) where T : unmanaged, ISerializable
        {
            value.Write(this);
        }

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
        public readonly Span<byte> AsSpan()
        {
            return new((void*)Address, (int)Position);
        }

        public readonly Span<byte> AsSpan(uint position, uint length)
        {
            return AsSpan().Slice((int)position, (int)length);
        }

        public readonly ReadOnlySpan<T> AsSpan<T>() where T : unmanaged
        {
            return new((void*)Address, (int)Position / sizeof(T));
        }

        public readonly ReadOnlySpan<T> AsSpan<T>(uint length) where T : unmanaged
        {
            return AsSpan<T>()[..(int)length];
        }

        public readonly ReadOnlySpan<T> AsSpan<T>(uint position, uint length) where T : unmanaged
        {
            return AsSpan<T>().Slice((int)position, (int)length);
        }

        public static BinaryWriter Create()
        {
            UnsafeBinaryWriter* value = UnsafeBinaryWriter.Allocate();
            return new BinaryWriter(value);
        }
    }
}