using System;
using System.Collections;
using System.Collections.Generic;

namespace Unmanaged.Collections
{
    public readonly unsafe struct UnmanagedArray<T> : IDisposable, IReadOnlyList<T>, IEquatable<UnmanagedArray<T>> where T : unmanaged
    {
        private readonly UnsafeArray* array;

        public readonly bool IsDisposed => UnsafeArray.IsDisposed(array);
        public readonly uint Length => array->Length;

        public readonly T this[uint index]
        {
            get => UnsafeArray.Get<T>(array, index);
            set => UnsafeArray.Set<T>(array, index, value);
        }

        int IReadOnlyCollection<T>.Count => (int)array->Length;
        T IReadOnlyList<T>.this[int index] => UnsafeArray.GetRef<T>(array, (uint)index);

        public UnmanagedArray()
        {
            throw new InvalidOperationException("UnmanagedArray cannot be created without a length.");
        }

        internal UnmanagedArray(UnsafeArray* array)
        {
            this.array = array;
        }

        /// <summary>
        /// Creates a new blank array with the specified length.
        /// </summary>
        public UnmanagedArray(uint length)
        {
            array = UnsafeArray.Allocate<T>(length);
        }

        public UnmanagedArray(Span<T> span)
        {
            array = UnsafeArray.Allocate<T>((uint)span.Length);
            span.CopyTo(AsSpan());
        }

        public UnmanagedArray(ReadOnlySpan<T> span)
        {
            array = UnsafeArray.Allocate<T>((uint)span.Length);
            span.CopyTo(AsSpan());
        }

        public readonly void Dispose()
        {
            UnsafeArray.Dispose(array);
        }

        /// <summary>
        /// Returns the span for the array.
        /// </summary>
        public readonly Span<T> AsSpan()
        {
            return UnsafeArray.AsSpan<T>(array);
        }

        /// <summary>
        /// Returns the array as a span of a different type, length may
        /// be different.
        /// </summary>
        public readonly Span<V> AsSpan<V>() where V : unmanaged
        {
            uint length = (uint)(array->Length * sizeof(T) / sizeof(V));
            return UnsafeArray.AsSpan<V>(array, length);
        }

        public readonly uint IndexOf<V>(V value) where V : unmanaged, IEquatable<V>
        {
            return UnsafeArray.IndexOf(array, value);
        }

        public readonly bool TryIndexOf<V>(V value, out uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeArray.TryIndexOf(array, value, out index);
        }

        public readonly bool Contains<V>(V value) where V : unmanaged, IEquatable<V>
        {
            return UnsafeArray.Contains(array, value);
        }

        public readonly ref T GetRef(uint index)
        {
            return ref UnsafeArray.GetRef<T>(array, index);
        }

        public readonly T Get(uint index)
        {
            return UnsafeArray.Get<T>(array, index);
        }

        public readonly void Set(uint index, T value)
        {
            UnsafeArray.Set<T>(array, index, value);
        }

        public readonly void CopyTo(Span<T> span)
        {
            AsSpan().CopyTo(span);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(array);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(array);
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

            return array == other.array;
        }

        public override int GetHashCode()
        {
            nint ptr = (nint)array;
            return HashCode.Combine(ptr, 7);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private UnsafeArray* array;
            private int index;

            public T Current => UnsafeArray.Get<T>(array, (uint)index);

            object IEnumerator.Current => Current;

            public Enumerator(UnsafeArray* array)
            {
                this.array = array;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < array->Length;
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
