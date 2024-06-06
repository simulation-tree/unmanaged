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

        public static void Free(ref UnsafeArray* array)
        {
            array->items.Dispose();
            Allocations.Free(ref array);
            array = null;
        }

        public static bool IsDisposed(UnsafeArray* array)
        {
            return Allocations.IsNull(array) || array->items.IsDisposed;
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
            uint size = type.Size;
            UnsafeArray* array = Allocations.Allocate<UnsafeArray>();
            array->type = type;
            array->length = length;
            array->items = new(size * length);
            array->items.Clear(size * length);
            return array;
        }

        public static UnsafeArray* Allocate<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            UnsafeArray* array = Allocate<T>((uint)span.Length);
            span.CopyTo(array->items.AsSpan<T>(0, array->length));
            return array;
        }

        public static ref T GetRef<T>(UnsafeArray* array, uint index) where T : unmanaged
        {
            T* ptr = (T*)GetAddress(array);
            return ref ptr[index];
        }

        public static Span<T> AsSpan<T>(UnsafeArray* array) where T : unmanaged
        {
            return array->items.AsSpan<T>(0, array->length);
        }

        public static bool TryIndexOf<T>(UnsafeArray* array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            Span<T> span = AsSpan<T>(array);
            int i = span.IndexOf(value);
            if (i == -1)
            {
                index = uint.MaxValue;
                return false;
            }
            else
            {
                index = (uint)i;
                return true;
            }
        }

        /// <summary>
        /// Resizes the array and optionally initializes new elements.
        /// </summary>
        public static void Resize(UnsafeArray* array, uint newLength, bool initialize = false)
        {
            uint size = array->type.Size;
            uint oldLength = array->length;
            array->items.Resize(size * newLength);
            array->length = newLength;

            if (initialize && newLength > oldLength)
            {
                array->items.Clear(size * oldLength, size * (newLength - oldLength));
            }
        }

        /// <summary>
        /// Clears the entire array to <c>default</c> state.
        /// </summary>
        public static void Clear(UnsafeArray* array)
        {
            array->items.Clear(array->length * array->type.Size);
        }

        /// <summary>
        /// Clears a range of elements in the array to <c>default</c> state.
        /// </summary>
        public static void Clear(UnsafeArray* array, uint start, uint length)
        {
            uint size = array->type.Size;
            array->items.Clear(start * size, length * size);
        }
    }
}
