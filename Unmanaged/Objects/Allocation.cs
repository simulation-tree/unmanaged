using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// An unmanaged allocation.
    /// </summary>
    public unsafe struct Allocation : IDisposable, IEquatable<Allocation>
    {
        private uint length;
        private void* pointer;

        public readonly uint Length => length;

        /// <summary>
        /// Has this allocation been disposed? Also counts for instances that weren't allocated.
        /// </summary>
        public readonly bool IsDisposed => Allocations.IsNull(pointer);

        public Allocation()
        {
            this.length = 0;
            pointer = Allocations.Allocate(0);
        }

        /// <summary>
        /// Creates a new uninitialized allocation.
        /// </summary>
        public Allocation(uint length)
        {
            this.length = length;
            pointer = Allocations.Allocate(length);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index > Length)
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
            Allocations.Free(pointer);
        }

        public readonly void Write<T>(uint start, T value) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            uint elementSize = (uint)sizeof(T);
            uint byteStart = start * elementSize;
            ThrowIfOutOfRange(byteStart + elementSize);
            Unsafe.Write((void*)((nint)pointer + byteStart), value);
        }

        public readonly Span<T> AsSpan<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            return new Span<T>(pointer, (int)(Length / sizeof(T)));
        }

        public readonly Span<T> AsSpan<T>(uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            uint endIndex = (uint)((start + length) * sizeof(T));
            ThrowIfOutOfRange(endIndex);
            return new Span<T>((void*)((nint)pointer + start), (int)length);
        }

        public readonly ref T AsRef<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
#if DEBUG
            if (Length < sizeof(T))
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
            Allocations.ThrowIfNull(pointer);
            NativeMemory.Clear(pointer, Length);
        }

        /// <summary>
        /// Resizes the allocation, and leaves new bytes uninitialized.
        /// </summary>
        public void Resize(uint newLength)
        {
            pointer = Allocations.Reallocate(pointer, newLength);
            length = newLength;
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
            CopyTo(0, Math.Min(Length, destination.Length), destination, 0, destination.Length);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Allocation allocation && Equals(allocation);
        }

        public readonly bool Equals(Allocation other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return pointer == other.pointer;
        }

        public readonly override int GetHashCode()
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
