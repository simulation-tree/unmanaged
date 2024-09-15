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
            UnsafeBinaryWriter.Write(ref this.value, ptr, USpan<T>.ElementSize);
        }

        public void WriteSpan<T>(USpan<T> span) where T : unmanaged
        {
            UnsafeBinaryWriter.Write(ref value, span.pointer, span.Length * USpan<T>.ElementSize);
        }

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
            USpan<char> buffer = stackalloc char[(int)FixedString.MaxLength];
            uint length = text.CopyTo(buffer);
            WriteUTF8Text(buffer.Slice(0, length));
        }

        public void WriteUTF8Text(string text)
        {
            USpan<char> buffer = stackalloc char[text.Length];
            text.AsUSpan().CopyTo(buffer);
            WriteUTF8Text(buffer);
        }

        public readonly void WriteObject<T>(T value) where T : unmanaged, ISerializable
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
        public readonly USpan<byte> GetBytes()
        {
            return new((void*)Address, Position);
        }

        public readonly USpan<T> AsSpan<T>() where T : unmanaged
        {
            return new((void*)Address, Position / USpan<T>.ElementSize);
        }

        public static BinaryWriter Create()
        {
            UnsafeBinaryWriter* value = UnsafeBinaryWriter.Allocate();
            return new BinaryWriter(value);
        }
    }
}