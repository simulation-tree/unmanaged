using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// Represents an allocated region of memory that isn't managed by the garbage collector.
    /// <para>
    /// Must be disposed.
    /// </para>
    /// </summary>
    public readonly unsafe struct UnmanagedBuffer : IDisposable
    {
        /// <summary>
        /// Size of each element in the buffer.
        /// </summary>
        public readonly uint size;

        /// <summary>
        /// Fixed length of the buffer.
        /// </summary>
        public readonly uint length;

        private readonly nint pointer;

        public readonly bool IsDisposed => Allocations.IsNull(pointer);

        public UnmanagedBuffer(uint size, uint length, bool clear = true)
        {
            this.size = size;
            this.length = length;
            pointer = Marshal.AllocHGlobal((int)(length * size));
            Allocations.Register(pointer);

            if (clear)
            {
                Span<byte> span = AsSpan();
                span.Clear();
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfSizeMismatch(uint size)
        {
            if (this.size != size)
            {
                throw new ArgumentException("Size mismatch.");
            }
        }

        public readonly void Dispose()
        {
            Allocations.ThrowIfNull(pointer);

            Marshal.FreeHGlobal(pointer);
            Allocations.Unregister(pointer);
        }

        public readonly void Clear()
        {
            Allocations.ThrowIfNull(pointer);

            Span<byte> span = AsSpan();
            span.Clear();
        }

        public readonly unsafe Span<byte> AsSpan()
        {
            Allocations.ThrowIfNull(pointer);

            byte* bytes = (byte*)pointer;
            return new Span<byte>(bytes, (int)(size * length));
        }

        /// <summary>
        /// Returns a span into the entire memory allocated by this buffer.
        /// </summary>
        public readonly unsafe Span<T> AsSpan<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfSizeMismatch((uint)sizeof(T));

            T* items = (T*)pointer;
            return new Span<T>(items, (int)length);
        }

        public readonly unsafe Span<T> AsSpan<T>(uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfSizeMismatch((uint)sizeof(T));

            if (length > this.length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            T* items = (T*)pointer;
            return new Span<T>(items, (int)length);
        }

        public readonly unsafe Span<T> AsSpan<T>(uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfSizeMismatch((uint)sizeof(T));

            if (start + length > this.length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            T* items = (T*)pointer;
            return new Span<T>(items + start, (int)length);
        }

        public readonly unsafe void* AsPointer()
        {
            Allocations.ThrowIfNull(pointer);

            return (void*)pointer;
        }

        public readonly ref T GetRef<T>(uint index) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfSizeMismatch((uint)sizeof(T));

            if (index >= length)
            {
                throw new IndexOutOfRangeException();
            }

            T* items = (T*)pointer;
            return ref Unsafe.Add(ref items[0], (int)index);
        }

        public readonly T Get<T>(uint index) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfSizeMismatch((uint)sizeof(T));

            if (index >= length)
            {
                throw new IndexOutOfRangeException();
            }

            T* items = (T*)pointer;
            return items[index];
        }

        public readonly void Set<T>(uint index, T value) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfSizeMismatch((uint)sizeof(T));

            if (index >= length)
            {
                throw new IndexOutOfRangeException();
            }

            T* items = (T*)pointer;
            items[index] = value;
        }

        /// <summary>
        /// Returns all bytes for the element at the given index.
        /// <para>
        /// May throw <see cref="IndexOutOfRangeException"/>.
        /// </para>
        /// </summary>
        public readonly Span<byte> Get(uint index)
        {
            Allocations.ThrowIfNull(pointer);

            if (index >= length)
            {
                throw new IndexOutOfRangeException();
            }

            return new Span<byte>((void*)(pointer + (index * size)), (int)size);
        }

        public readonly void CopyTo(UnmanagedBuffer destination)
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.ThrowIfNull(destination.pointer);

            Span<byte> sourceSpan = AsSpan();
            Span<byte> destinationSpan = destination.AsSpan();
            sourceSpan.CopyTo(destinationSpan);
        }

        public readonly void CopyTo(uint sourceIndex, UnmanagedBuffer destination, uint destinationIndex)
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.ThrowIfNull(destination.pointer);

            if (sourceIndex >= length)
            {
                throw new IndexOutOfRangeException(nameof(sourceIndex));
            }

            if (destinationIndex >= destination.length)
            {
                throw new IndexOutOfRangeException(nameof(destinationIndex));
            }

            Span<byte> sourceSpan = Get(sourceIndex);
            Span<byte> destinationSpan = destination.Get(destinationIndex);
            sourceSpan.CopyTo(destinationSpan);
        }

        public readonly uint IndexOf<T>(T value) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfSizeMismatch((uint)sizeof(T));

            Span<T> span = AsSpan<T>();
            int result = span.IndexOf(value);
            if (result == -1)
            {
                throw new ArgumentException("The value was not found in the buffer.");
            }
            else
            {
                return (uint)result;
            }
        }

        public readonly bool Contains<T>(T value) where T : unmanaged, IEquatable<T>
        {
            Allocations.ThrowIfNull(pointer);
            ThrowIfSizeMismatch((uint)sizeof(T));

            Span<T> span = AsSpan<T>();
            return span.Contains(value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return HashCode.Combine(size, length, pointer);
            }
        }

        public readonly int GetContentHashCode()
        {
            Allocations.ThrowIfNull(pointer);
            unchecked
            {
                int hash = 17;
                for (uint i = 0; i < length; i++)
                {
                    Span<byte> span = Get(i);
                    int djb2hash = Djb2.GetDjb2HashCode(span);
                    hash = hash * 23 + djb2hash;
                }

                return hash;
            }
        }
    }
}
