using System;
using System.Collections;
using System.Collections.Generic;

namespace Unmanaged.Collections
{
    public readonly unsafe struct UnmanagedList<T> : IDisposable, IReadOnlyList<T> where T : unmanaged
    {
        private readonly UnsafeList* list;

        public readonly bool IsDisposed => UnsafeList.IsDisposed(list);
        public readonly uint Count => UnsafeList.GetCount(list);
        public readonly uint Capacity => UnsafeList.GetCapacity(list);

        public readonly T this[uint index]
        {
            get => UnsafeList.Get<T>(list, index);
            set => UnsafeList.Set<T>(list, index, value);
        }

        T IReadOnlyList<T>.this[int index] => UnsafeList.Get<T>(list, (uint)index);
        int IReadOnlyCollection<T>.Count => (int)Count;

        public UnmanagedList()
        {
            list = UnsafeList.Allocate<T>();
        }

        internal UnmanagedList(UnsafeList* list)
        {
            this.list = list;
        }

        public UnmanagedList(uint initialCapacity)
        {
            list = UnsafeList.Allocate<T>(initialCapacity);
        }

        public UnmanagedList(Span<T> span)
        {
            list = UnsafeList.Allocate(span);
        }

        public UnmanagedList(ReadOnlySpan<T> span)
        {
            list = UnsafeList.Allocate(span);
        }

        public UnmanagedList(IEnumerable<T> list)
        {
            this.list = UnsafeList.Allocate<T>();
            foreach (T item in list)
            {
                Add(item);
            }
        }

        public readonly void Dispose()
        {
            UnsafeList.Dispose(list);
        }

        /// <summary>
        /// Returns the list contents as a span.
        /// </summary>
        public readonly Span<T> AsSpan()
        {
            return UnsafeList.AsSpan<T>(list);
        }

        public readonly Span<T> AsSpan(uint start)
        {
            return UnsafeList.AsSpan<T>(list, start);
        }

        public readonly Span<T> AsSpan(uint start, uint length)
        {
            return UnsafeList.AsSpan<T>(list, start, length);
        }

        public readonly void Add(T item)
        {
            UnsafeList.Add(list, item);
        }

        public readonly bool AddIfUnique<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.AddIfUnique(list, item);
        }

        public readonly void AddDefault(uint count = 1)
        {
            UnsafeList.AddDefault(list, count);
        }

        public readonly void AddRange(ReadOnlySpan<T> items)
        {
            UnsafeList.AddRange(list, items);
        }

        public readonly void AddRange(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                Add(item);
            }
        }

        public readonly uint IndexOf<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.IndexOf(list, item);
        }

        public readonly bool TryIndexOf<V>(V item, out uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.TryIndexOf(list, item, out index);
        }

        public readonly bool Contains<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.Contains(list, item);
        }

        public readonly uint Remove<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.Remove(list, item);
        }

        public readonly void RemoveAt(uint index)
        {
            UnsafeList.RemoveAt(list, index);
        }

        /// <summary>
        /// Clears the list of all elements, making count 0.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeList.Clear(list);
        }

        /// <summary>
        /// Clears the list all elements and ensures the capacity is at least the minimum capacity.
        /// </summary>
        public readonly void Clear(uint minimumCapacity)
        {
            UnsafeList.Clear(list);
            uint capacity = UnsafeList.GetCapacity(list);
            if (capacity < minimumCapacity)
            {
                UnsafeList.AddDefault(list, minimumCapacity - capacity);
            }
        }

        public readonly ref T GetRef(uint index)
        {
            return ref UnsafeList.GetRef<T>(list, index);
        }

        public readonly override int GetHashCode()
        {
            return UnsafeList.GetHashCode(list);
        }

        public readonly int GetContentHashCode()
        {
            return UnsafeList.GetContentHashCode(list);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(list);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(list);
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
