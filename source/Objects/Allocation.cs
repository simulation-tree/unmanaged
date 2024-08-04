using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// An unmanaged allocation.
    /// </summary>
    public unsafe struct Allocation : IDisposable, IEquatable<Allocation>
    {
        private void* pointer;

        /// <summary>
        /// Has this allocation been disposed? Also counts for instances that weren't allocated.
        /// </summary>
        public readonly bool IsDisposed => Allocations.IsNull(pointer);

        public readonly nint Address => (nint)pointer;

        public readonly Span<byte> this[Range range]
        {
            get
            {
                Allocations.ThrowIfNull(pointer);
                return new Span<byte>((void*)((nint)pointer + range.Start.Value), range.End.Value - range.Start.Value);
            }
        }

        public Allocation(void* pointer)
        {
            this.pointer = pointer;
        }

        /// <summary>
        /// Creates a new uninitialized allocation.
        /// </summary>
        public Allocation(uint size)
        {
            pointer = Allocations.Allocate(size);
        }

#if NET5_0_OR_GREATER
        public Allocation()
        {
            pointer = Allocations.Allocate(0);
        }
#endif
        /// <summary>
        /// Frees the allocation.
        /// </summary>
        public void Dispose()
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.Free(ref pointer);
        }

        public readonly void Write<T>(T value) where T : unmanaged
        {
            Write(0, value);
        }

        public readonly void Write<T>(uint index, T value) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            uint length = (uint)sizeof(T);
            uint position = index * length;
            void* ptr = &value;
            Write(position, ptr, length);
        }

        public readonly void Write<T>(Span<T> span) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            fixed (T* ptr = span)
            {
                Write(0, ptr, (uint)(span.Length * sizeof(T)));
            }
        }

        /// <summary>
        /// Writes the given span into the memory.
        /// </summary>
        public readonly void Write<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            fixed (T* ptr = span)
            {
                Write(0, ptr, (uint)(span.Length * sizeof(T)));
            }
        }

        public readonly void Write(uint position, void* data, uint length)
        {
            Allocations.ThrowIfNull(pointer);
            Unsafe.CopyBlock((void*)((nint)pointer + position), data, length);
        }

        /// <returns>A span of bytes for the given slice range of memory.</returns>
        public readonly Span<byte> AsSpan(uint start, uint length)
        {
            Allocations.ThrowIfNull(pointer);
            return new Span<byte>((void*)((nint)pointer + start), (int)length);
        }

        /// <summary>
        /// Gets a span of elements from the memory, where the start and
        /// length values are normalized to type <typeparamref name="T"/>.
        /// </summary>
        public readonly Span<T> AsSpan<T>(uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            uint byteLength = (uint)sizeof(T);
            uint position = start * byteLength;
            return new Span<T>((void*)((nint)pointer + position), (int)length);
        }

        public readonly ref T AsRef<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            return ref Unsafe.AsRef<T>(pointer);
        }

        /// <summary>
        /// Resets the memory to <c>default</c> state.
        /// </summary>
        public readonly void Clear(uint length)
        {
            Allocations.ThrowIfNull(pointer);
            NativeMemory.Clear(pointer, length);
        }

        /// <summary>
        /// Resets a range of memory to <c>default</c> state.
        /// </summary>
        public readonly void Clear(uint start, uint length)
        {
            Allocations.ThrowIfNull(pointer);
            nint address = (nint)((nint)pointer + start);
            NativeMemory.Clear((void*)address, length);
        }

        /// <summary>
        /// Resizes the allocation, and leaves new bytes uninitialized.
        /// </summary>
        public void Resize(uint newLength)
        {
            pointer = Allocations.Reallocate(pointer, newLength);
        }

        /// <summary>
        /// Resizes the allocation to fit the given type.
        /// </summary>
        public void Resize<T>() where T : unmanaged
        {
            Resize((uint)sizeof(T));
        }

        /// <summary>
        /// Copies contents of this allocation into the destination.
        /// </summary>
        public readonly void CopyTo(Allocation destination, uint sourceIndex, uint destinationIndex, uint size)
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.ThrowIfNull(destination.pointer);
            Span<byte> sourceSpan = AsSpan<byte>(sourceIndex, size);
            Span<byte> destinationSpan = destination.AsSpan<byte>(destinationIndex, size);
            sourceSpan.CopyTo(destinationSpan);
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

        /// <summary>
        /// Creates an empty allocation of size 0.
        /// </summary>
        public static Allocation Create()
        {
            return new(0);
        }

        /// <summary>
        /// Creates an allocation that contains the data of the given value.
        /// </summary>
        public static Allocation Create<T>(T value) where T : unmanaged
        {
            Allocation allocation = new((uint)sizeof(T));
            allocation.Write(value);
            return allocation;
        }

        /// <summary>
        /// Creates an uninitialized allocation that fits the given type.
        /// </summary>
        public static Allocation Create<T>() where T : unmanaged
        {
            Allocation allocation = new((uint)sizeof(T));
            return allocation;
        }

        public static Allocation Create<T>(Span<T> span) where T : unmanaged
        {
            uint length = (uint)(span.Length * sizeof(T));
            Allocation allocation = new(length);
            span.CopyTo(allocation.AsSpan<T>(0, (uint)span.Length));
            return allocation;
        }

        public static Allocation Create<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            uint length = (uint)(span.Length * sizeof(T));
            Allocation allocation = new(length);
            span.CopyTo(allocation.AsSpan<T>(0, (uint)span.Length));
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
