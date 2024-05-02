using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    public unsafe struct Allocation : IDisposable, IEquatable<Allocation>
    {
        private uint length;
        private void* value;

        public readonly uint Length => length;
        public readonly bool IsDisposed => value is null || Allocations.IsNull((nint)value);

        public Allocation()
        {
            throw new InvalidOperationException("Sizeless allocation not allowed");
        }

        public Allocation(uint length, uint alignment = 8)
        {
            ThrowIfLengthIsZero(length);
            value = NativeMemory.AlignedAlloc(length, alignment);
            Allocations.Register((nint)value);
            this.length = length;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfLengthIsZero(uint value)
        {
            if (value == 0)
            {
                throw new InvalidOperationException("Allocation length cannot be zero.");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index > length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRangeForCasting(uint size)
        {
            if (size > length)
            {
                throw new InvalidCastException("Cannot cast to type, size is too small.");
            }
        }

        public readonly void Dispose()
        {
            Allocations.ThrowIfNull((nint)value);
            NativeMemory.AlignedFree(value);
            Allocations.Unregister((nint)value);
        }

        public void Resize(uint length, uint alignment = 8)
        {
            Allocations.ThrowIfNull((nint)value);
            Allocations.Unregister((nint)value);
            value = NativeMemory.AlignedRealloc(value, length, alignment);
            this.length = length;
            Allocations.Register((nint)value);
        }

        public readonly T* AsPointer<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)value);
            if (length < sizeof(T))
            {
                throw new InvalidOperationException("Allocation is too small to be casted to the specified type.");
            }

            return (T*)value;
        }

        public readonly void Write<T>(uint start, T value) where T : unmanaged
        {
            //todo: its a bit uncomfortable to assume non byte index for a low level concept, if they
            //become byte indices then id like to remove length field too
            Allocations.ThrowIfNull((nint)this.value);
            uint elementSize = (uint)sizeof(T);
            uint byteStart = start * elementSize;
            ThrowIfOutOfRange(byteStart + elementSize);
            Unsafe.Write((void*)((nint)this.value + byteStart), value);
        }

        public readonly Span<T> AsSpan<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)this.value);
            T* items = (T*)this.value;
            return new Span<T>(items, (int)(length / sizeof(T)));
        }

        public readonly Span<T> AsSpan<T>(uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)this.value);
            uint endIndex = (uint)((start + length) * sizeof(T));
            ThrowIfOutOfRange(endIndex);
            T* items = (T*)this.value;
            return new Span<T>(items + start, (int)length);
        }

        public readonly ref T As<T>() where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)this.value);
            ThrowIfOutOfRangeForCasting((uint)sizeof(T));
            return ref Unsafe.AsRef<T>((void*)this.value);
        }

        /// <summary>
        /// Copies contents of this allocation into the destination.
        /// </summary>
        public readonly void CopyTo(uint sourceIndex, uint sourceLength, Allocation destination, uint destinationIndex, uint destinationLength)
        {
            Allocations.ThrowIfNull((nint)value);
            Allocations.ThrowIfNull((nint)destination.value);
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

        public readonly void Clear()
        {
            NativeMemory.Clear((void*)value, length);
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

            return value == other.value;
        }

        public override int GetHashCode()
        {
            nint ptr = (nint)value;
            return HashCode.Combine(ptr, 7);
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
