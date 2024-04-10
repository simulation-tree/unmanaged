using System;
using System.Runtime.InteropServices;

namespace Unmanaged.Collections
{
    public unsafe struct UnsafeArray
    {
        private RuntimeType type;
        private UnmanagedBuffer items;

        public readonly int Length => (int)items.length;

        public UnsafeArray()
        {
            throw new InvalidOperationException("Use UnsafeArray.Create() instead.");
        }

        public static void Dispose(UnsafeArray* array)
        {
            array->items.Dispose();
            Marshal.FreeHGlobal((nint)array);
            array->type = default;
            array->items = default;
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
            return ref array->items.GetRef<T>(index);
        }

        public static T Get<T>(UnsafeArray* array, uint index) where T : unmanaged
        {
            return array->items.Get<T>(index);
        }

        public static void Set<T>(UnsafeArray* array, uint index, T value) where T : unmanaged
        {
            array->items.Set(index, value);
        }

        public static Span<T> AsSpan<T>(UnsafeArray* array) where T : unmanaged
        {
            return array->items.AsSpan<T>();
        }

        public static Span<T> AsSpan<T>(UnsafeArray* array, uint length) where T : unmanaged
        {
            return array->items.AsSpan<T>(length);
        }

        public static Span<T> AsSpan<T>(UnsafeArray* array, uint start, uint length) where T : unmanaged
        {
            return array->items.AsSpan<T>(start, length);
        }

        public static uint IndexOf<T>(UnsafeArray* array, T value) where T : unmanaged, IEquatable<T>
        {
            return array->items.IndexOf(value);
        }

        public static bool Contains<T>(UnsafeArray* array, T value) where T : unmanaged, IEquatable<T>
        {
            return array->items.Contains(value);
        }

        public static void CopyTo(UnsafeArray* source, uint sourceIndex, UnsafeArray* destination, uint destinationIndex)
        {
            source->items.CopyTo(sourceIndex, destination->items, destinationIndex);
        }
    }
}
