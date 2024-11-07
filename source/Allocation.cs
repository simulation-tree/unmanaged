﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// An unmanaged allocation of memory.
    /// </summary>
    public unsafe struct Allocation : IDisposable, IEquatable<Allocation>
    {
        private void* pointer;

        /// <summary>
        /// Has this allocation been disposed? Also counts for instances that weren't allocated.
        /// </summary>
        public readonly bool IsDisposed => pointer is null;

        public readonly nint Address => (nint)pointer;

        public readonly ref byte this[uint index]
        {
            get
            {
                Allocations.ThrowIfNull(pointer);
#if NET
                return ref Unsafe.Add(ref Unsafe.AsRef<byte>(pointer), index);
#else
                return ref Unsafe.Add(ref Unsafe.AsRef<byte>(pointer), (int)index);
#endif
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

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[16];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly uint ToString(USpan<char> buffer)
        {
            Allocations.ThrowIfNull(pointer);
            return Address.ToString(buffer);
        }

        /// <summary>
        /// Writes a single given value into the memory into this byte position.
        /// </summary>
        public readonly void Write<T>(uint bytePosition, T value) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            void* ptr = &value;
            Write(bytePosition, TypeInfo<T>.size, ptr);
        }

        /// <summary>
        /// Writes a single given value into the memory starting at the beginning.
        /// </summary>
        public readonly void Write<T>(T value) where T : unmanaged
        {
            Write(0, value);
        }

        /// <summary>
        /// Writes the given span into the memory starting at this position in bytes.
        /// </summary>
        public readonly void Write<T>(uint bytePosition, USpan<T> span) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            Write(bytePosition, span.Length * TypeInfo<T>.size, (void*)span.Address);
        }

        public readonly void Write(uint bytePosition, uint byteLength, void* data)
        {
            Allocations.ThrowIfNull(pointer);
            Unsafe.CopyBlock((void*)((nint)pointer + bytePosition), data, byteLength);
        }

        /// <summary>
        /// Retrieves a span slice of the bytes in this allocation.
        /// </summary>
        public readonly USpan<byte> AsSpan(uint bytePosition, uint byteLength)
        {
            Allocations.ThrowIfNull(pointer);
            return new USpan<byte>((void*)((nint)pointer + bytePosition), byteLength);
        }

        /// <summary>
        /// Gets a span of elements from the memory.
        /// <para>Both <paramref name="start"/> and <paramref name="length"/> are expected
        /// to be in <typeparamref name="T"/> elements.</para>
        /// </summary>
        public readonly USpan<T> AsSpan<T>(uint start, uint length) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            uint position = start * TypeInfo<T>.size;
            return new USpan<T>((void*)((nint)pointer + position), length);
        }

        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the memory starting from the given byte position.
        /// </summary>
        public readonly ref T Read<T>(uint bytePosition = 0) where T : unmanaged
        {
            Allocations.ThrowIfNull(pointer);
            return ref Unsafe.AsRef<T>((void*)((nint)pointer + bytePosition));
        }

        public readonly void* Read(uint bytePosition, uint byteLength)
        {
            Allocations.ThrowIfNull(pointer);
            return (void*)((nint)pointer + bytePosition);
        }

        /// <summary>
        /// Resets the memory to <c>default</c> state.
        /// </summary>
        public readonly void Clear(uint byteLength)
        {
            Allocations.ThrowIfNull(pointer);
            NativeMemory.Clear(pointer, byteLength);
        }

        /// <summary>
        /// Resets a range of memory to <c>default</c> state.
        /// </summary>
        public readonly void Clear(uint bytePosition, uint byteLength)
        {
            Allocations.ThrowIfNull(pointer);
            nint address = (nint)((nint)pointer + bytePosition);
            NativeMemory.Clear((void*)address, byteLength);
        }

        /// <summary>
        /// Fills the memory with the given byte value.
        /// </summary>
        public readonly void Fill(uint byteLength, byte value)
        {
            Allocations.ThrowIfNull(pointer);
            NativeMemory.Fill(pointer, byteLength, value);
        }

        /// <summary>
        /// Fills the memory with the given byte value.
        /// </summary>
        public readonly void Fill(uint bytePosition, uint byteLength, byte value)
        {
            Allocations.ThrowIfNull(pointer);
            nint address = (nint)((nint)pointer + bytePosition);
            NativeMemory.Fill((void*)address, byteLength, value);
        }

        /// <summary>
        /// Copies bytes of this allocation into the destination.
        /// </summary>
        public readonly void CopyTo(Allocation destination, uint sourceIndex, uint destinationIndex, uint byteLength)
        {
            Allocations.ThrowIfNull(pointer);
            Allocations.ThrowIfNull(destination.pointer);
            USpan<byte> sourceSpan = AsSpan<byte>(sourceIndex, byteLength);
            USpan<byte> destinationSpan = destination.AsSpan<byte>(destinationIndex, byteLength);
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
        /// Moves existing memory into a new allocation of the given size.
        /// </summary>
        public static void Resize(ref Allocation allocation, uint newLength)
        {
            allocation = new(Allocations.Reallocate(allocation.pointer, newLength));
        }

        /// <summary>
        /// Moves existing memory into a new allocation that is able to
        /// fit the given type.
        /// </summary>
        public static void Resize<T>(ref Allocation allocation) where T : unmanaged
        {
            Resize(ref allocation, TypeInfo<T>.size);
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
            Allocation allocation = new(TypeInfo<T>.size);
            allocation.Write(0, value);
            return allocation;
        }

        /// <summary>
        /// Creates an uninitialized allocation that can contain a(n) <typeparamref name="T"/>
        /// </summary>
        public static Allocation Create<T>() where T : unmanaged
        {
            Allocation allocation = new(TypeInfo<T>.size);
            return allocation;
        }

        /// <summary>
        /// Creates an allocation containg the given span.
        /// </summary>
        public static Allocation Create<T>(USpan<T> span) where T : unmanaged
        {
            uint length = span.Length * TypeInfo<T>.size;
            Allocation allocation = new(length);
            span.CopyTo(allocation.AsSpan<T>(0, span.Length));
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

        public static implicit operator void*(Allocation allocation)
        {
            return allocation.pointer;
        }

        public static implicit operator Allocation*(Allocation allocation)
        {
            return (Allocation*)allocation.pointer;
        }

        public static implicit operator nint(Allocation allocation)
        {
            return allocation.Address;
        }
    }
}
