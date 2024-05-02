using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmanaged.Collections
{
    public unsafe struct UnsafeArray
    {
        private RuntimeType type;
        private Allocation items;

        public UnsafeArray()
        {
            throw new InvalidOperationException("Use UnsafeArray.Allocate() instead.");
        }

        [Conditional("DEBUG")]
        private static void ThrowIfLengthIsZero(uint value)
        {
            if (value == 0)
            {
                throw new InvalidOperationException("Array allocations cannot be zero in length.");
            }
        }

        public static void Free(UnsafeArray* array)
        {
            array->items.Dispose();
            NativeMemory.Free(array);
            Allocations.Unregister((nint)array);
        }

        public static bool IsDisposed(UnsafeArray* array)
        {
            return array->items.IsDisposed;
        }

        public static uint GetLength(UnsafeArray* array)
        {
            return array->items.Length / array->type.size;
        }

        public static UnsafeArray* Allocate<T>(uint length) where T : unmanaged
        {
            return Allocate(RuntimeType.Get<T>(), length);
        }

        public static UnsafeArray* Allocate(RuntimeType type, uint length)
        {
            ThrowIfLengthIsZero(length);
            UnsafeArray* array = (UnsafeArray*)NativeMemory.AllocZeroed((uint)sizeof(UnsafeArray));
            array->type = type;
            array->items = new(type.size * length, type.Alignment);
            Allocations.Register((nint)array);
            return array;
        }

        public static UnsafeArray* Allocate<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            UnsafeArray* array = Allocate<T>((uint)span.Length);
            span.CopyTo(array->items.AsSpan<T>());
            return array;
        }

        public static UnsafeArray* Allocate<T>(IReadOnlyCollection<T> values) where T : unmanaged
        {
            UnsafeArray* array = Allocate<T>((uint)values.Count);
            Span<T> span = AsSpan<T>(array);
            int i = 0;
            foreach (T value in values)
            {
                span[i++] = value;
            }

            return array;
        }

        public static ref T GetRef<T>(UnsafeArray* array, uint index) where T : unmanaged
        {
            Span<T> span = AsSpan<T>(array);
            return ref span[(int)index];
        }

        public static T Get<T>(UnsafeArray* array, uint index) where T : unmanaged
        {
            Span<T> span = AsSpan<T>(array);
            return span[(int)index];
        }

        public static void Set<T>(UnsafeArray* array, uint index, T value) where T : unmanaged
        {
            Span<T> span = AsSpan<T>(array);
            span[(int)index] = value;
        }

        public static Span<T> AsSpan<T>(UnsafeArray* array) where T : unmanaged
        {
            return array->items.AsSpan<T>();
        }

        public static Span<T> AsSpan<T>(UnsafeArray* array, uint start, uint length) where T : unmanaged
        {
            return array->items.AsSpan<T>(start, length);
        }

        public static uint IndexOf<T>(UnsafeArray* array, T value) where T : unmanaged, IEquatable<T>
        {
            Span<T> span = AsSpan<T>(array);
            int i = span.IndexOf(value);
            if (i == -1)
            {
                throw new NullReferenceException("Item not found.");
            }

            return (uint)i;
        }

        public static bool TryIndexOf<T>(UnsafeArray* array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            Span<T> span = AsSpan<T>(array);
            int i = span.IndexOf(value);
            if (i == -1)
            {
                index = 0;
                return false;
            }
            else
            {
                index = (uint)i;
                return true;
            }
        }

        public static bool Contains<T>(UnsafeArray* array, T value) where T : unmanaged, IEquatable<T>
        {
            Span<T> span = AsSpan<T>(array);
            return span.Contains(value);
        }

        public static void CopyTo(UnsafeArray* source, uint sourceIndex, UnsafeArray* destination, uint destinationIndex)
        {
            uint elementSize = source->type.size;
            Span<byte> sourceSpan = source->items.AsSpan<byte>(sourceIndex * elementSize, elementSize);
            Span<byte> destinationSpan = destination->items.AsSpan<byte>(destinationIndex * elementSize, elementSize);
            sourceSpan.CopyTo(destinationSpan);
        }

        public static void Resize(UnsafeArray* array, uint length)
        {
            Allocation oldItems = array->items;
            RuntimeType type = array->type;
            array->items = new(type.size * length, type.Alignment);
            oldItems.CopyTo(0, Math.Min(oldItems.Length, array->items.Length), array->items, 0, array->items.Length);
            oldItems.Dispose();
            //todo: use allocation.Resize() function here instead
        }
    }
}
