using System;
using System.Collections;
using System.Collections.Generic;

namespace Unmanaged.Collections
{
    public unsafe struct UnmanagedList<T> : IDisposable, IReadOnlyList<T>, IEquatable<UnmanagedList<T>> where T : unmanaged
    {
        private UnsafeList* value;

        public readonly bool IsDisposed => UnsafeList.IsDisposed(value);
        public readonly uint Count => UnsafeList.GetCount(value);
        public readonly uint Capacity => UnsafeList.GetCapacity(value);
        public readonly nint Address => UnsafeList.GetAddress(value);

        public readonly T this[uint index]
        {
            get => UnsafeList.Get<T>(value, index);
            set => UnsafeList.Set<T>(this.value, index, value);
        }

        T IReadOnlyList<T>.this[int index] => UnsafeList.Get<T>(value, (uint)index);
        int IReadOnlyCollection<T>.Count => (int)Count;

        public UnmanagedList()
        {
            value = UnsafeList.Allocate<T>();
        }

        public UnmanagedList(UnsafeList* list)
        {
            this.value = list;
        }

        public UnmanagedList(uint initialCapacity)
        {
            value = UnsafeList.Allocate<T>(initialCapacity);
        }

        public UnmanagedList(Span<T> span)
        {
            value = UnsafeList.Allocate(span);
        }

        public UnmanagedList(ReadOnlySpan<T> span)
        {
            value = UnsafeList.Allocate(span);
        }

        public UnmanagedList(IEnumerable<T> list)
        {
            this.value = UnsafeList.Allocate<T>();
            foreach (T item in list)
            {
                Add(item);
            }
        }

        public void Dispose()
        {
            UnsafeList.Free(ref value);
        }

        /// <summary>
        /// Returns a span for the contents of the list.
        /// </summary>
        public readonly Span<T> AsSpan()
        {
            return UnsafeList.AsSpan<T>(value);
        }

        public readonly Span<T> AsSpan(uint start)
        {
            return UnsafeList.AsSpan<T>(value, start);
        }

        public readonly Span<T> AsSpan(uint start, uint length)
        {
            return UnsafeList.AsSpan<T>(value, start, length);
        }

        public readonly void Insert(uint index, T item)
        {
            UnsafeList.Insert(value, index, item);
        }

        public readonly void Add(T item)
        {
            UnsafeList.Add(value, item);
        }

        public readonly bool TryAdd<V>(V item) where V : unmanaged, IEquatable<V>
        {
            Span<V> span = UnsafeList.AsSpan<V>(value);
            if (span.Contains(item))
            {
                return false;
            }

            UnsafeList.Add(value, item);
            return true;
        }

        /// <summary>
        /// Adds a range of default values to the list.
        /// </summary>
        public readonly void AddDefault(uint count = 1)
        {
            UnsafeList.AddDefault(value, count);
        }

        public readonly void AddRange(ReadOnlySpan<T> items)
        {
            UnsafeList.AddRange(value, items);
        }

        public readonly uint IndexOf<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.IndexOf(value, item);
        }

        public readonly bool TryIndexOf<V>(V item, out uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.TryIndexOf(value, item, out index);
        }

        /// <summary>
        /// Checks whether the list contains the given item.
        /// </summary>
        public readonly bool Contains<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.Contains(value, item);
        }

        public readonly uint Remove<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.Remove(value, item);
        }

        public readonly bool TryRemove<V>(V item) where V : unmanaged, IEquatable<V>
        {
            if (UnsafeList.TryIndexOf(value, item, out uint index))
            {
                UnsafeList.RemoveAt(value, index);
                return true;
            }
            else return false;
        }

        public readonly void RemoveAt(uint index)
        {
            UnsafeList.RemoveAt(value, index);
        }

        public readonly void RemoveAtBySwapping(uint index)
        {
            UnsafeList.RemoveAtBySwapping(value, index);
        }

        /// <summary>
        /// Clears the list so that it's count becomes 0.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeList.Clear(value);
        }

        /// <summary>
        /// Clears the list all elements and ensures the capacity is at least the minimum capacity.
        /// </summary>
        public readonly void Clear(uint minimumCapacity)
        {
            UnsafeList.Clear(value);
            uint capacity = UnsafeList.GetCapacity(value);
            if (capacity < minimumCapacity)
            {
                UnsafeList.AddDefault(value, minimumCapacity - capacity);
            }
        }

        public readonly ref T GetRef(uint index)
        {
            return ref UnsafeList.GetRef<T>(value, index);
        }

        public readonly override int GetHashCode()
        {
            nint ptr = (nint)value;
            return HashCode.Combine(ptr, 7);
        }

        public readonly int GetContentHashCode()
        {
            return UnsafeList.GetContentHashCode(value);
        }

        public readonly void CopyTo(Span<T> destination)
        {
            AsSpan().CopyTo(destination);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(value);
        }

        public bool Equals(UnmanagedList<T> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return value == other.value;
        }

        public override bool Equals(object? obj)
        {
            return obj is UnmanagedList<T> list && Equals(list);
        }

        public struct Enumerator(UnsafeList* list) : IEnumerator<T>
        {
            private readonly UnsafeList* list = list;
            private int index = -1;

            public readonly T Current => UnsafeList.Get<T>(list, (uint)index);

            readonly object IEnumerator.Current => UnsafeList.Get<T>(list, (uint)index);

            public bool MoveNext()
            {
                index++;
                return index < UnsafeList.GetCount(list);
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose()
            {
            }
        }

        public static bool operator ==(UnmanagedList<T> left, UnmanagedList<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnmanagedList<T> left, UnmanagedList<T> right)
        {
            return !left.Equals(right);
        }
    }
}
