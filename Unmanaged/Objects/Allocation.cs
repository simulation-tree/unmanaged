using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// An unmanaged allocation.
    /// </summary>
    public readonly unsafe struct Allocation : IDisposable, IEquatable<Allocation>
    {
        /// <summary>
        /// Size of the allocation in bytes.
        /// </summary>
        public readonly uint length;

        private readonly void* pointer;

        /// <summary>
        /// Has this allocation been disposed? Also counts for instances that weren't allocated.
        /// </summary>
        public readonly bool IsDisposed => Allocations.IsNull((nint)pointer);

        public Allocation()
        {
            this.length = 0;
            pointer = NativeMemory.Alloc(0);
            Allocations.Register((nint)pointer);
        }

        /// <summary>
        /// Creates a new uninitialized allocation.
        /// </summary>
        public Allocation(uint length)
        {
            this.length = length;
            pointer = NativeMemory.Alloc(length);
            Allocations.Register((nint)pointer);
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
            Allocations.ThrowIfNull((nint)pointer);
            NativeMemory.Free(pointer);
            Allocations.Unregister((nint)pointer);
        }

        public readonly void Write<T>(uint start, T value) where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)pointer);
            uint elementSize = (uint)sizeof(T);
            uint byteStart = start * elementSize;
            ThrowIfOutOfRange(byteStart + elementSize);
            Unsafe.Write((void*)((nint)pointer + byteStart), value);
        }

        public readonly Span<T> AsSpan<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)pointer);
            ///T* items = (T*)pointer;
            return new Span<T>(pointer, (int)(length / sizeof(T)));
        }

        public readonly Span<T> AsSpan<T>(uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)pointer);
            uint endIndex = (uint)((start + length) * sizeof(T));
            ThrowIfOutOfRange(endIndex);
            return new Span<T>((void*)((nint)pointer + start), (int)length);
        }

        public readonly ref T AsRef<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)pointer);
#if DEBUG
            if (length < sizeof(T))
            {
                throw new InvalidCastException("Expected type isn't large enough to contain the bytes in the allocation");
            }
#endif

            return ref Unsafe.AsRef<T>(pointer);
        }

        /// <summary>
        /// Resets the memory to zero.
        /// </summary>
        public readonly void Clear()
        {
            Allocations.ThrowIfNull((nint)pointer);
            NativeMemory.Clear(pointer, length);
        }

        /// <summary>
        /// Copies contents of this allocation into the destination.
        /// </summary>
        public readonly void CopyTo(uint sourceIndex, uint sourceLength, Allocation destination, uint destinationIndex, uint destinationLength)
        {
            Allocations.ThrowIfNull((nint)pointer);
            Allocations.ThrowIfNull((nint)destination.pointer);
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

        public override bool Equals(object? obj)
        {
            return obj is Allocation allocation && Equals(allocation);
        }

        public bool Equals(Allocation other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return pointer == other.pointer;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((nint)pointer);
        }

        public static Allocation Create<T>(T value) where T : unmanaged
        {
            Allocation allocation = new((uint)sizeof(T));
            allocation.Write(0, value);
            return allocation;
        }

        public static Allocation Create<T>(Span<T> span) where T : unmanaged
        {
            Allocation allocation = new((uint)(span.Length * sizeof(T)));
            span.CopyTo(allocation.AsSpan<T>());
            return allocation;
        }

        public static Allocation Create<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            Allocation allocation = new((uint)(span.Length * sizeof(T)));
            span.CopyTo(allocation.AsSpan<T>());
            return allocation;
        }

        public static bool operator ==(Allocation left, Allocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Allocation left, Allocation right)
        {
            return !(left == right);
        }
    }
}
