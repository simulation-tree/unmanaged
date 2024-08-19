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

        /// <summary>
        /// Creates an existing allocation from the given pointer.
        /// </summary>
        public Allocation(void* pointer)
        {
            this.pointer = pointer;
        }

        /// <summary>
        /// Creates a new uninitialized allocation with the given size.
        /// </summary>
        public Allocation(uint size, bool clear = false)
        {
            pointer = Allocations.Allocate(size);
            if (clear)
            {
                Clear(size);
            }
        }

#if NET
        /// <summary>
        /// Creates a new empty allocation.
        /// </summary>
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

        /// <summary>
        /// Writes the given value into the memory of this allocation.
        /// <para>Position is in bytes.</para>
        /// </summary>
        public readonly void Write<T>(T value, uint start = 0) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            uint length = (uint)sizeof(T);
            void* ptr = &value;
            Write(ptr, start, length);
        }

        public readonly void Write<T>(Span<T> span) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            fixed (T* ptr = span)
            {
                Write(ptr, 0, (uint)(span.Length * sizeof(T)));
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
                Write(ptr, 0, (uint)(span.Length * sizeof(T)));
            }
        }

        public readonly void Write(void* data, uint start, uint length)
        {
            Allocations.ThrowIfNull(pointer);
            Unsafe.CopyBlock((void*)((nint)pointer + start), data, length);
        }

        public readonly Span<byte> AsSpan(uint start, uint length)
        {
            Allocations.ThrowIfNull(pointer);
            return new Span<byte>((void*)((nint)pointer + start), (int)length);
        }

        /// <summary>
        /// Gets a span of elements from the memory.
        /// <para>Both <paramref name="start"/> and <paramref name="length"/> are expected
        /// to be in elements, not bytes.</para>
        /// </summary>
        public readonly Span<T> AsSpan<T>(uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            uint byteLength = (uint)sizeof(T);
            uint position = start * byteLength;
            return new Span<T>((void*)((nint)pointer + position), (int)length);
        }

        /// <summary>
        /// Reads a value of <typeparamref name="T"/> from the memory at the given position.
        /// <para>Position is in bytes.</para>
        /// </summary>
        public readonly ref T Read<T>(uint start = 0) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            return ref Unsafe.AsRef<T>((void*)((nint)pointer + start));
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
