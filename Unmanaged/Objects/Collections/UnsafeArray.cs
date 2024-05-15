using System;
using System.Collections.Generic;

namespace Unmanaged.Collections
{
    public unsafe struct UnsafeArray
    {
        private RuntimeType type;
        private uint length;
        private Allocation items;

        public UnsafeArray()
        {
            throw new InvalidOperationException("Use UnsafeArray.Create() instead.");
        }

        public static void Free(UnsafeArray* array)
        {
            array->items.Dispose();
            Allocations.Free(array);
        }

        public static bool IsDisposed(UnsafeArray* array)
        {
            return Allocations.IsNull(array);
        }

        public static uint GetLength(UnsafeArray* array)
        {
            return array->length;
        }

        public static nint GetAddress(UnsafeArray* array)
        {
            return array->items.Address;
        }

        public static UnsafeArray* Allocate<T>(uint length) where T : unmanaged
        {
            return Allocate(RuntimeType.Get<T>(), length);
        }

        public static UnsafeArray* Allocate(RuntimeType type, uint length)
        {
            UnsafeArray* array = Allocations.Allocate<UnsafeArray>();
            array->type = type;
            array->length = length;
            array->items = new(type.size * length);
            array->items.Clear();
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

        public static Span<T> AsSpan<T>(UnsafeArray* array) where T : unmanaged
        {
            return array->items.AsSpan<T>();
        }

        public static Span<T> AsSpan<T>(UnsafeArray* array, uint start, uint length) where T : unmanaged
        {
            return array->items.AsSpan<T>(start, length);
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

        public static void CopyTo(UnsafeArray* source, uint sourceIndex, UnsafeArray* destination, uint destinationIndex)
        {
            uint elementSize = source->type.size;
            Span<byte> sourceSpan = source->items.AsSpan<byte>(sourceIndex * elementSize, elementSize);
            Span<byte> destinationSpan = destination->items.AsSpan<byte>(destinationIndex * elementSize, elementSize);
            sourceSpan.CopyTo(destinationSpan);
        }

        public static void Resize(UnsafeArray* array, uint newLength)
        {
            Allocation oldItems = array->items;
            array->items = new(array->type.size * newLength);
            array->length = newLength;
            oldItems.CopyTo(0, Math.Min(oldItems.Length, array->length), array->items, 0, array->length);
            oldItems.Dispose();
        }

        public static void Clear(UnsafeArray* array)
        {
            array->items.Clear();
        }
    }
}
