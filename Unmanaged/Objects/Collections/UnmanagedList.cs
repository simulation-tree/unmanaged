using System;
using System.Collections;
using System.Collections.Generic;

namespace Unmanaged.Collections
{
    public readonly unsafe struct UnmanagedList<T> : IDisposable, IReadOnlyList<T> where T : unmanaged
    {
        private readonly UnsafeList* value;

        public readonly bool IsDisposed => UnsafeList.IsDisposed(value);
        public readonly uint Count => UnsafeList.GetCount(value);
        public readonly uint Capacity => UnsafeList.GetCapacity(value);

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

        internal UnmanagedList(UnsafeList* list)
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

        public readonly void Dispose()
        {
            UnsafeList.Free(value);
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

        public readonly bool AddIfUnique<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.AddIfUnique(value, item);
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

        public readonly void RemoveAt(uint index)
        {
            UnsafeList.RemoveAt(value, index);
        }

        /// <summary>
        /// Clears the list of all elements, making count 0.
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
            return UnsafeList.GetHashCode(value);
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

        public struct Enumerator(UnsafeList* list) : IEnumerator<T>
        {
            private UnsafeList* list = list;
            private int index = -1;

            public T Current => UnsafeList.Get<T>(list, (uint)index);

            object IEnumerator.Current => UnsafeList.Get<T>(list, (uint)index);

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
    }
}
