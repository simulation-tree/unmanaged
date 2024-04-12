using System;
using System.Collections;
using System.Collections.Generic;

namespace Unmanaged.Collections
{
    public readonly unsafe struct UnmanagedList<T> : IDisposable, IReadOnlyList<T> where T : unmanaged
    {
        private readonly UnsafeList* list;

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
            list = UnsafeList.Create<T>();
        }

        public UnmanagedList(uint initialCapacity)
        {
            list = UnsafeList.Create<T>(initialCapacity);
        }

        public UnmanagedList(Span<T> span)
        {
            list = UnsafeList.Create(span);
        }

        public UnmanagedList(ReadOnlySpan<T> span)
        {
            list = UnsafeList.Create(span);
        }

        public UnmanagedList(IEnumerable<T> list)
        {
            this.list = UnsafeList.Create<T>();
            foreach (T item in list)
            {
                Add(item);
            }
        }

        internal UnmanagedList(UnsafeList* list)
        {
            this.list = list;
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

        public readonly Span<T> AsSpan(uint start, uint length)
        {
            return UnsafeList.AsSpan<T>(list, start, length);
        }

        public readonly void Add(T item)
        {
            UnsafeList.Add(list, item);
        }

        public readonly void AddRange(ReadOnlySpan<T> items)
        {
            UnsafeList.AddRange(list, items);
        }

        public readonly uint IndexOf<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.IndexOf(list, item);
        }

        public readonly bool Contains<V>(V item) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.Contains(list, item);
        }

        public readonly void Remove<V>(V item) where V : unmanaged, IEquatable<V>
        {
            UnsafeList.Remove(list, item);
        }

        public readonly void RemoveAt(uint index)
        {
            UnsafeList.RemoveAt(list, index);
        }

        public readonly void Clear()
        {
            UnsafeList.Clear(list);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(list);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(list);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private UnsafeList* list;
            private int index;

            public Enumerator(UnsafeList* list)
            {
                this.list = list;
                index = -1;
            }

            public T Current => UnsafeList.Get<T>(list, (uint)index);

            object IEnumerator.Current => UnsafeList.Get<T>(list, (uint)index);

            public bool MoveNext()
            {
                index++;
                return index < list->Count;
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
