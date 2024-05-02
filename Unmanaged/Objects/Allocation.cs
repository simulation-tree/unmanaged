using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// Unmanaged allocation.
    /// </summary>
    public readonly unsafe struct Allocation : IDisposable
    {
        /// <summary>
        /// Size of the allocation in bytes.
        /// </summary>
        public readonly uint length;

        private readonly nint pointer;

        /// <summary>
        /// Has this allocation been disposed? Also counts for instances that weren't allocated.
        /// </summary>
        public readonly bool IsDisposed => Allocations.IsNull(pointer);

        public Allocation()
        {
            throw new InvalidOperationException("Sizeless allocation is not allowed.");
        }

        /// <summary>
        /// Creates a new uninitialized allocation.
        /// </summary>
        public Allocation(uint length)
        {
            ThrowIfLengthIsZero(length);
            this.length = length;
            pointer = (nint)NativeMemory.Alloc(length);
            Allocations.Register(pointer);
        }

        [Conditional("DEBUG")]
        private void ThrowIfLengthIsZero(uint value)
        {
            if (value == 0)
            {
                //throw new InvalidOperationException("Allocation length cannot be zero.");
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfOutOfRange(uint index)
        {
            if (index > length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <summary>
        /// Frees the allocation.
        /// </summary>
        public readonly void Dispose()
        {
            Allocations.ThrowIfNull(pointer);
            NativeMemory.Free((void*)pointer);
            Allocations.Unregister(pointer);
        }

        public readonly void Write<T>(uint start, T value) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            uint elementSize = (uint)sizeof(T);
            uint byteStart = start * elementSize;
            ThrowIfOutOfRange(byteStart + elementSize);
            Unsafe.Write((void*)(pointer + byteStart), value);
        }

        public readonly Span<T> AsSpan<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            T* items = (T*)pointer;
            return new Span<T>(items, (int)(length / sizeof(T)));
        }

        public readonly Span<T> AsSpan<T>(uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            uint endIndex = (uint)((start + length) * sizeof(T));
            ThrowIfOutOfRange(endIndex);
            T* items = (T*)pointer;
            return new Span<T>(items + start, (int)length);
        }

        public readonly ref T AsRef<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
#if DEBUG
            if (length < sizeof(T))
            {
                throw new InvalidCastException("Expected type isn't large enough to contain the bytes in the allocation");
            }
#endif

            return ref Unsafe.AsRef<T>((void*)pointer);
        }

        /// <summary>
        /// Resets the memory to zero.
        /// </summary>
        public readonly void Clear()
        {
            Allocations.ThrowIfNull(pointer);
            Span<byte> span = AsSpan<byte>();
            span.Clear();
        }

        /// <summary>
        /// Copies contents of this allocation into the destination.
        /// </summary>
        public readonly void CopyTo(uint sourceIndex, uint sourceLength, Allocation destination, uint destinationIndex, uint destinationLength)
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.ThrowIfNull(destination.pointer);
            Span<byte> sourceSpan = AsSpan<byte>(sourceIndex, sourceLength);
            Span<byte> destinationSpan = destination.AsSpan<byte>(destinationIndex, destinationLength);
            sourceSpan.CopyTo(destinationSpan);
        }

        /// <summary>
        /// Copies bytes from this allocation into the destination.
        /// <para>
        /// Copy length is size of the destination.
        /// </para>
        /// </summary>
        public readonly void CopyTo(Allocation destination)
        {
            CopyTo(0, Math.Min(length, destination.length), destination, 0, destination.length);
        }
    }
}
