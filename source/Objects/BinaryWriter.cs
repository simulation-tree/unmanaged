using System;
using Unmanaged.Collections;

namespace Unmanaged
{
    public readonly unsafe struct BinaryWriter : IDisposable
    {
        private readonly UnmanagedList<byte> data;

        public readonly bool IsDisposed => data.IsDisposed;
        public readonly uint Length => data.Count;
        public readonly nint Address => data.Address;

        public BinaryWriter()
        {
            data = new();
        }

        public BinaryWriter(UnmanagedList<byte> data)
        {
            this.data = data;
        }

        public readonly void WriteValue<T>(T value) where T : unmanaged
        {
            byte* ptr = (byte*)&value;
            int size = sizeof(T);
            for (int i = 0; i < size; i++)
            {
                data.Add(ptr[i]);
            }
        }

        public readonly void WriteSpan<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            fixed (T* ptr = span)
            {
                byte* bytePtr = (byte*)ptr;
                int size = sizeof(T) * span.Length;
                for (int i = 0; i < size; i++)
                {
                    data.Add(bytePtr[i]);
                }
            }
        }

        public readonly void WriteObject<T>(T value) where T : unmanaged, IBinaryObject
        {
            value.Write(this);
        }

        public readonly void Dispose()
        {
            data.Dispose();
        }

        public readonly Span<byte> AsSpan()
        {
            return data.AsSpan();
        }

        public readonly ReadOnlySpan<T> AsSpan<T>(uint position, uint length) where T : unmanaged
        {
            return new((void*)(data.Address + position), (int)length);
        }
    }
}