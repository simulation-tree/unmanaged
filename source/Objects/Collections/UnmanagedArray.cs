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
        public readonly ReadOnlySpan<T> this[Range range] => AsSpan()[range];

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
        public UnmanagedArray(Span<T> span)
        {
            value = UnsafeArray.Allocate<T>(span);
        }

        /// <summary>
        /// Creates a new array containing the given span.
        /// </summary>
        public UnmanagedArray(ReadOnlySpan<T> span)
        {
            value = UnsafeArray.Allocate(span);
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

        /// <summary>
        /// Returns the array as a span.
        /// </summary>
        public readonly Span<T> AsSpan()
        {
            return UnsafeArray.AsSpan<T>(value);
        }

        public readonly Span<T> AsSpan(uint start, uint length)
        {
            return AsSpan().Slice((int)start, (int)length);
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

        /// <summary>
        /// Retrieves a reference to the value at the specified index.
        /// </summary>
        public readonly ref T GetRef(uint index)
        {
            return ref UnsafeArray.GetRef<T>(value, index);
        }

        /// <summary>
        /// Retrieves the value at the specified index.
        /// </summary>
        public readonly T Get(uint index)
        {
            return UnsafeArray.GetRef<T>(value, index);
        }

        /// <summary>
        /// Assigns a value to the specified index.
        /// </summary>
        public readonly void Set(uint index, T value)
        {
            UnsafeArray.GetRef<T>(this.value, index) = value;
        }

        public readonly void CopyTo(Span<T> span)
        {
            AsSpan().CopyTo(span);
        }

        public readonly void CopyFrom(Span<T> span)
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
