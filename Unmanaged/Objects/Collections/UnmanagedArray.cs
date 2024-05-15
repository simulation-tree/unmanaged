using System;
using System.Collections;
using System.Collections.Generic;

namespace Unmanaged.Collections
{
    public readonly unsafe struct UnmanagedArray<T> : IDisposable, IReadOnlyList<T>, IEquatable<UnmanagedArray<T>> where T : unmanaged
    {
        private readonly UnsafeArray* value;

        public readonly bool IsDisposed => UnsafeArray.IsDisposed(value);
        public readonly uint Length => UnsafeArray.GetLength(value);
        public readonly nint Address => UnsafeArray.GetAddress(value);

        public readonly T this[uint index]
        {
            get => UnsafeArray.GetRef<T>(value, index);
            set => UnsafeArray.GetRef<T>(this.value, index) = value;
        }

        public readonly ReadOnlySpan<T> this[Range range] => AsSpan()[range];

        int IReadOnlyCollection<T>.Count => (int)Length;
        T IReadOnlyList<T>.this[int index] => UnsafeArray.GetRef<T>(value, (uint)index);

        public UnmanagedArray()
        {
            value = UnsafeArray.Allocate<T>(0);
        }

        internal UnmanagedArray(UnsafeArray* array)
        {
            this.value = array;
        }

        /// <summary>
        /// Creates a new blank array with the specified length.
        /// </summary>
        public UnmanagedArray(uint length)
        {
            value = UnsafeArray.Allocate<T>(length);
        }

        public UnmanagedArray(Span<T> span)
        {
            value = UnsafeArray.Allocate<T>((uint)span.Length);
            span.CopyTo(AsSpan());
        }

        public UnmanagedArray(ReadOnlySpan<T> span)
        {
            value = UnsafeArray.Allocate<T>((uint)span.Length);
            span.CopyTo(AsSpan());
        }

        public readonly void Dispose()
        {
            UnsafeArray.Free(value);
        }

        /// <summary>
        /// Resets all elements in the array to 0.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeArray.Clear(value);
        }

        /// <summary>
        /// Returns the span for the array.
        /// </summary>
        public readonly Span<T> AsSpan()
        {
            return UnsafeArray.AsSpan<T>(value);
        }

        public readonly bool TryIndexOf<V>(V value, out uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeArray.TryIndexOf(this.value, value, out index);
        }

        public readonly bool Contains<V>(V value) where V : unmanaged, IEquatable<V>
        {
            return TryIndexOf(value, out _);
        }

        public readonly void Resize(uint length)
        {
            UnsafeArray.Resize(value, length);
        }

        public readonly ref T GetRef(uint index)
        {
            return ref UnsafeArray.GetRef<T>(value, index);
        }

        public readonly T Get(uint index)
        {
            return UnsafeArray.GetRef<T>(value, index);
        }

        public readonly void Set(uint index, T value)
        {
            UnsafeArray.GetRef<T>(this.value, index) = value;
        }

        public readonly void CopyTo(Span<T> span)
        {
            AsSpan().CopyTo(span);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(value);
        }

        public override bool Equals(object? obj)
        {
            return obj is UnmanagedArray<T> array && Equals(array);
        }

        public bool Equals(UnmanagedArray<T> other)
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

        public struct Enumerator(UnsafeArray* array) : IEnumerator<T>
        {
            private readonly UnsafeArray* array = array;
            private int index = -1;

            public readonly T Current => UnsafeArray.GetRef<T>(array, (uint)index);

            readonly object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                index++;
                return index < UnsafeArray.GetLength(array);
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose()
            {
            }
        }

        public static bool operator ==(UnmanagedArray<T> left, UnmanagedArray<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnmanagedArray<T> left, UnmanagedArray<T> right)
        {
            return !(left == right);
        }
    }
}
