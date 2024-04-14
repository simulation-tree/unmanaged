using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmanaged.Collections
{
    public unsafe struct UnsafeArray
    {
        private RuntimeType type;
        private UnmanagedBuffer items;

        public readonly uint Length => items.length;

        public UnsafeArray()
        {
            throw new InvalidOperationException("Use UnsafeArray.Create() instead.");
        }

        public static void Dispose(UnsafeArray* array)
        {
            ThrowIfNull(array);

            array->items.Dispose();
            Marshal.FreeHGlobal((nint)array);
            array->type = default;
            array->items = default;
        }

        public static bool IsDisposed(UnsafeArray* array)
        {
            ThrowIfNull(array);

            return array->items.IsDisposed;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNull(UnsafeArray* array)
        {
            if (array is null)
            {
                throw new InvalidOperationException("UnsafeArray is null.");
            }
        }

        public static UnsafeArray* Create<T>(uint length) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            nint arrayPointer = Marshal.AllocHGlobal(sizeof(UnsafeArray));
            UnsafeArray* array = (UnsafeArray*)arrayPointer;
            array->type = type;
            array->items = new(type.size, length);
            return array;
        }

        public static UnsafeArray* Create(RuntimeType type, uint length)
        {
            nint arrayPointer = Marshal.AllocHGlobal(sizeof(UnsafeArray));
            UnsafeArray* array = (UnsafeArray*)arrayPointer;
            array->type = type;
            array->items = new(type.size, length);
            return array;
        }

        public static ref T GetRef<T>(UnsafeArray* array, uint index) where T : unmanaged
        {
            ThrowIfNull(array);

            return ref array->items.GetRef<T>(index);
        }

        public static T Get<T>(UnsafeArray* array, uint index) where T : unmanaged
        {
            ThrowIfNull(array);

            return array->items.Get<T>(index);
        }

        public static void Set<T>(UnsafeArray* array, uint index, T value) where T : unmanaged
        {
            ThrowIfNull(array);

            array->items.Set(index, value);
        }

        public static Span<T> AsSpan<T>(UnsafeArray* array) where T : unmanaged
        {
            ThrowIfNull(array);

            return array->items.AsSpan<T>();
        }

        public static Span<T> AsSpan<T>(UnsafeArray* array, uint length) where T : unmanaged
        {
            ThrowIfNull(array);

            return array->items.AsSpan<T>(length);
        }

        public static Span<T> AsSpan<T>(UnsafeArray* array, uint start, uint length) where T : unmanaged
        {
            ThrowIfNull(array);

            return array->items.AsSpan<T>(start, length);
        }

        public static uint IndexOf<T>(UnsafeArray* array, T value) where T : unmanaged, IEquatable<T>
        {
            ThrowIfNull(array);

            return array->items.IndexOf(value);
        }

        public static bool Contains<T>(UnsafeArray* array, T value) where T : unmanaged, IEquatable<T>
        {
            ThrowIfNull(array);

            return array->items.Contains(value);
        }

        public static void CopyTo(UnsafeArray* source, uint sourceIndex, UnsafeArray* destination, uint destinationIndex)
        {
            ThrowIfNull(source);
            ThrowIfNull(destination);

            source->items.CopyTo(sourceIndex, destination->items, destinationIndex);
        }
    }
}
