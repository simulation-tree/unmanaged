﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Unmanaged.Collections
{
    public unsafe struct UnsafeArray
    {
        private RuntimeType type;
        private Allocation items;

        public UnsafeArray()
        {
            throw new InvalidOperationException("Use UnsafeArray.Create() instead.");
        }

        public static void Free(UnsafeArray* array)
        {
            array->items.Dispose();
            Marshal.FreeHGlobal((nint)array);
        }

        public static bool IsDisposed(UnsafeArray* array)
        {
            return array->items.IsDisposed;
        }

        public static uint GetLength(UnsafeArray* array)
        {
            return array->items.length / array->type.size;
        }

        public static UnsafeArray* Allocate<T>(uint length) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            nint arrayPointer = Marshal.AllocHGlobal(sizeof(UnsafeArray));
            UnsafeArray* array = (UnsafeArray*)arrayPointer;
            array->type = type;
            array->items = new(type.size * length);
            array->items.Clear();
            return array;
        }

        public static UnsafeArray* Allocate(RuntimeType type, uint length)
        {
            nint arrayPointer = Marshal.AllocHGlobal(sizeof(UnsafeArray));
            UnsafeArray* array = (UnsafeArray*)arrayPointer;
            array->type = type;
            array->items = new(type.size * length);
            array->items.Clear();
            return array;
        }

        public static UnsafeArray* Allocate<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            nint arrayPointer = Marshal.AllocHGlobal(sizeof(UnsafeArray));
            UnsafeArray* array = (UnsafeArray*)arrayPointer;
            array->type = type;
            array->items = new(type.size * (uint)span.Length);
            span.CopyTo(array->items.AsSpan<T>());
            return array;
        }

        public static UnsafeArray* Allocate<T>(IReadOnlyCollection<T> values) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            nint arrayPointer = Marshal.AllocHGlobal(sizeof(UnsafeArray));
            UnsafeArray* array = (UnsafeArray*)arrayPointer;
            array->type = type;
            array->items = new(type.size * (uint)values.Count);
            Span<T> span = array->items.AsSpan<T>();
            int i = 0;
            foreach (T value in values)
            {
                span[i++] = value;
            }

            return array;
        }

        public static ref T GetRef<T>(UnsafeArray* array, uint index) where T : unmanaged
        {
            Span<T> span = array->items.AsSpan<T>();
            return ref span[(int)index];
        }

        public static T Get<T>(UnsafeArray* array, uint index) where T : unmanaged
        {
            Span<T> span = array->items.AsSpan<T>();
            return span[(int)index];
        }

        public static void Set<T>(UnsafeArray* array, uint index, T value) where T : unmanaged
        {
            Span<T> span = array->items.AsSpan<T>();
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
            Span<T> span = array->items.AsSpan<T>();
            int i = span.IndexOf(value);
            if (i == -1)
            {
                throw new NullReferenceException("Item not found.");
            }

            return (uint)i;
        }

        public static bool TryIndexOf<T>(UnsafeArray* array, T value, out uint index) where T : unmanaged, IEquatable<T>
        {
            Span<T> span = array->items.AsSpan<T>();
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
            Span<T> span = array->items.AsSpan<T>();
            return span.Contains(value);
        }

        public static void CopyTo(UnsafeArray* source, uint sourceIndex, UnsafeArray* destination, uint destinationIndex)
        {
            uint elementSize = source->type.size;
            Span<byte> sourceSpan = source->items.AsSpan<byte>(sourceIndex * elementSize, elementSize);
            Span<byte> destinationSpan = destination->items.AsSpan<byte>(destinationIndex * elementSize, elementSize);
            sourceSpan.CopyTo(destinationSpan);
        }
    }
}
