using System;
using System.IO;
using Unmanaged.Serialization.Unsafe;

namespace Unmanaged
{
    public unsafe struct BinaryWriter : IDisposable
    {
        private UnsafeBinaryWriter* value;

        public readonly bool IsDisposed => UnsafeBinaryWriter.IsDisposed(value);
        public readonly uint Length
        {
            get => UnsafeBinaryWriter.SetPosition(value);
            set => UnsafeBinaryWriter.SetPosition(this.value, value);
        }

        public readonly nint Address => UnsafeBinaryWriter.GetAddress(value);

        public BinaryWriter()
        {
            value = UnsafeBinaryWriter.Allocate();
        }

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

        public void WriteUTF8Span(ReadOnlySpan<char> span)
        {
            foreach (char c in span)
            {
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

        public readonly void WriteObject<T>(T value) where T : unmanaged, ISerializable
        {
            value.Write(this);
        }

        public void Dispose()
        {
            UnsafeBinaryWriter.Free(ref value);
        }

        /// <summary>
        /// All bytes written into the writer.
        /// </summary>
        public readonly Span<byte> AsSpan()
        {
            return new((void*)Address, (int)Length);
        }

        public readonly Span<byte> AsSpan(uint position, uint length)
        {
            return AsSpan().Slice((int)position, (int)length);
        }

        public readonly ReadOnlySpan<T> AsSpan<T>() where T : unmanaged
        {
            return new((void*)Address, (int)Length / sizeof(T));
        }

        public readonly ReadOnlySpan<T> AsSpan<T>(uint position, uint length) where T : unmanaged
        {
            return AsSpan<T>().Slice((int)position, (int)length);
        }
    }
}