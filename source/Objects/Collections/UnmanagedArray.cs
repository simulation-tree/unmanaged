using System;
using System.Collections;
using System.Collections.Generic;

namespace Unmanaged.Collections
{
    public unsafe struct UnmanagedArray<T> : IDisposable, IReadOnlyList<T>, IEquatable<UnmanagedArray<T>> where T : unmanaged
    {
        private UnsafeArray* value;

        public readonly bool IsDisposed => UnsafeArray.IsDisposed(value);
        public readonly uint Length => UnsafeArray.GetLength(value);
        public readonly ref T this[uint index] => ref UnsafeArray.GetRef<T>(value, index);

        readonly int IReadOnlyCollection<T>.Count => (int)Length;
        readonly T IReadOnlyList<T>.this[int index] => UnsafeArray.GetRef<T>(value, (uint)index);

        public UnmanagedArray(UnsafeArray* array)
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

        /// <summary>
        /// Creates a new array containing the given span.
        /// </summary>
        public UnmanagedArray(USpan<T> span)
        {
            value = UnsafeArray.Allocate<T>(span);
        }

        /// <summary>
        /// Creates a new array containing elements from the given list.
        /// </summary>
        public UnmanagedArray(UnmanagedList<T> items)
        {
            value = UnsafeArray.Allocate<T>(items.AsSpan());
        }

#if NET
        /// <summary>
        /// Creates an empty array.
        /// </summary>
        public UnmanagedArray()
        {
            this = Create();
        }
#endif

        public void Dispose()
        {
            UnsafeArray.Free(ref value);
        }

        /// <summary>
        /// Resets all elements in the array back to <c>default</c> state.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeArray.Clear(value);
        }

        /// <summary>
        /// Clears the array from the specified start index to the end.
        /// </summary>
        public readonly void Clear(uint start, uint length)
        {
            UnsafeArray.Clear(value, start, length);
        }

        public readonly void Fill(T defaultValue)
        {
            AsSpan().Fill(defaultValue);
        }

        /// <summary>
        /// Returns the array as a span.
        /// </summary>
        public readonly USpan<T> AsSpan()
        {
            return UnsafeArray.AsSpan<T>(value);
        }

        public readonly USpan<T> AsSpan(uint start, uint length)
        {
            return AsSpan().Slice(start, length);
        }

        public readonly bool TryIndexOf<V>(V value, out uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeArray.TryIndexOf(this.value, value, out index);
        }

        public readonly uint IndexOf<V>(V value) where V : unmanaged, IEquatable<V>
        {
            if (!TryIndexOf(value, out uint index))
            {
                throw new NullReferenceException($"The value {value} was not found in the array.");
            }

            return index;
        }

        public readonly bool Contains<V>(V value) where V : unmanaged, IEquatable<V>
        {
            return TryIndexOf(value, out _);
        }

        /// <summary>
        /// Resizes the array to match the given length and
        /// optionally initializes new elements to <c>default</c> state.
        /// </summary>
        //todo: remove this Resize and use the setter in length?
        public readonly void Resize(uint length, bool initialize = false)
        {
            UnsafeArray.Resize(value, length, initialize);
        }

        public readonly void CopyTo(USpan<T> span)
        {
            AsSpan().CopyTo(span);
        }

        public readonly void CopyFrom(USpan<T> span)
        {
            span.CopyTo(AsSpan());
        }

        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(value);
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(value);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(value);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is UnmanagedArray<T> array && Equals(array);
        }

        public readonly bool Equals(UnmanagedArray<T> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return value == other.value;
        }

        public override readonly int GetHashCode()
        {
            nint ptr = (nint)value;
            return HashCode.Combine(ptr, 7);
        }

        public static UnmanagedArray<T> Create(uint length = 0)
        {
            return new UnmanagedArray<T>(length);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly UnsafeArray* array;
            private int index;

            public readonly T Current => UnsafeArray.GetRef<T>(array, (uint)index);

            readonly object IEnumerator.Current => Current;

            public Enumerator(UnsafeArray* array)
            {
                this.array = array;
                index = -1;
            }

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
