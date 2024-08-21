using System;
using System.Collections;
using System.Collections.Generic;

namespace Unmanaged.Collections
{
    public unsafe struct UnmanagedList<T> : IDisposable, IReadOnlyList<T>, IList<T>, IEquatable<UnmanagedList<T>> where T : unmanaged
    {
        private UnsafeList* value;

        public readonly bool IsDisposed => UnsafeList.IsDisposed(value);

        /// <summary>
        /// Amount of elements in the list.
        /// </summary>
        public readonly uint Count => UnsafeList.GetCountRef(value);

        public readonly uint Capacity
        {
            get => UnsafeList.GetCapacity(value);
            set => UnsafeList.SetCapacity(this.value, value);
        }

        public readonly ref T this[uint index] => ref UnsafeList.GetRef<T>(value, index);

        readonly T IReadOnlyList<T>.this[int index] => UnsafeList.Get<T>(value, (uint)index);
        readonly int IReadOnlyCollection<T>.Count => (int)Count;
        readonly int ICollection<T>.Count => (int)Count;
        readonly bool ICollection<T>.IsReadOnly => false;
        readonly T IList<T>.this[int index]
        {
            get => UnsafeList.Get<T>(value, (uint)index);
            set => UnsafeList.Set(this.value, (uint)index, value);
        }

        public UnmanagedList(UnsafeList* list)
        {
            this.value = list;
        }

        /// <summary>
        /// Creates a new list with the given initial capacity.
        /// </summary>
        public UnmanagedList(uint initialCapacity = 1)
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

#if NET5_0_OR_GREATER
        /// <summary>
        /// Creates a new empty list.
        /// </summary>
        public UnmanagedList()
        {
            this = Create();
        }
#endif

        public void Dispose()
        {
            UnsafeList.Free(ref value);
        }

        public readonly void* AsPointer()
        {
            return value;
        }

        /// <summary>
        /// Returns a span containing elements in the list.
        /// </summary>
        public readonly Span<T> AsSpan()
        {
            return UnsafeList.AsSpan<T>(value);
        }

        /// <summary>
        /// Returns the list as a span of a different type <typeparamref name="V"/>.
        /// </summary>
        public readonly Span<V> AsSpan<V>() where V : unmanaged
        {
            return UnsafeList.AsSpan<V>(value);
        }

        /// <summary>
        /// Returns the remaining span starting from the given index.
        /// </summary>
        public readonly Span<T> AsSpan(uint start)
        {
            return UnsafeList.AsSpan<T>(value, start);
        }

        public readonly Span<T> AsSpan(uint start, uint length)
        {
            return UnsafeList.AsSpan<T>(value, start, length);
        }

        /// <summary>
        /// Inserts the given item at the specified index by shifting 
        /// succeeding elements over.
        /// </summary>
        public readonly void Insert(uint index, T item)
        {
            UnsafeList.Insert(value, index, item);
        }

        public readonly void Add(T item)
        {
            UnsafeList.Add(value, item);
        }

        /// <summary>
        /// Attempts to add the given item if its unique.
        /// </summary>
        /// <returns><c>true</c> if item was added.</returns>
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

        /// <summary>
        /// Adds a range of the specified default value to the list.
        /// </summary>
        public readonly void AddRepeat(T defaultValue, uint count)
        {
            uint start = Count;
            AddDefault(count);
            Span<T> span = AsSpan(start);
            for (int i = 0; i < count; i++)
            {
                span[i] = defaultValue;
            }
        }

        /// <summary>
        /// Adds the given span to the list.
        /// </summary>
        public readonly void AddRange(ReadOnlySpan<T> items)
        {
            UnsafeList.AddRange(value, items);
        }

        public readonly void AddRange(void* pointer, uint count)
        {
            UnsafeList.AddRange(value, pointer, count);
        }

        public readonly void InsertRange(uint index, ReadOnlySpan<T> items)
        {
            uint count = Count;
            if (index > count)
            {
                throw new IndexOutOfRangeException();
            }

            uint length = (uint)items.Length;
            if (index == count)
            {
                AddRange(items);
            }
            else
            {
                Capacity = count + length;
                foreach (T item in items)
                {
                    Insert(index++, item);
                }
            }
        }

        /// <summary>
        /// Adds the given span of different type <typeparamref name="V"/> into
        /// the list, assuming its size equals to <typeparamref name="T"/>.
        /// </summary>
        public readonly void AddRange<V>(ReadOnlySpan<V> items) where V : unmanaged
        {
            UnsafeList.AddRange(value, items);
        }

        public readonly void AddRange(UnmanagedList<T> list)
        {
            nint address = UnsafeList.GetAddress(list.value);
            UnsafeList.AddRange(value, (void*)address, list.Count);
        }

        /// <summary>
        /// Returns the index of the given item in the list, otherwise
        /// throws an <see cref="Exception"/> if none found.
        /// </summary>
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

        /// <summary>
        /// Removes the given item from the list by swapping it with the removed element.
        /// </summary>
        public readonly bool TryRemove<V>(V item) where V : unmanaged, IEquatable<V>
        {
            if (TryIndexOf(item, out uint index))
            {
                RemoveAtBySwapping(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        public readonly void RemoveAt(uint index)
        {
            UnsafeList.RemoveAt(value, index);
        }

        public readonly void RemoveAtBySwapping(uint index)
        {
            UnsafeList.RemoveAtBySwapping(value, index);
        }

        public readonly V RemoveAt<V>(uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.RemoveAt<V>(value, index);
        }

        public readonly V RemoveAtBySwapping<V>(uint index) where V : unmanaged, IEquatable<V>
        {
            return UnsafeList.RemoveAtBySwapping<V>(value, index);
        }

        /// <summary>
        /// Clears the list so that it's count becomes 0.
        /// </summary>
        public readonly void Clear()
        {
            UnsafeList.Clear(value);
        }

        /// <summary>
        /// Clears the list all elements and ensures that the capacity at least
        /// the given amount.
        /// </summary>
        public readonly void Clear(uint minimumCapacity)
        {
            uint capacity = Capacity;
            if (capacity < minimumCapacity)
            {
                UnsafeList.AddDefault(value, minimumCapacity - capacity);
            }

            UnsafeList.Clear(value);
        }

        /// <summary>
        /// Returns the element at the given index by reference.
        /// </summary>
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

        public readonly Enumerator GetEnumerator()
        {
            return new(value);
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public readonly bool Equals(UnmanagedList<T> other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return value == other.value;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is UnmanagedList<T> list && Equals(list);
        }

        readonly int IList<T>.IndexOf(T item)
        {
            Span<T> values = AsSpan();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        readonly void IList<T>.Insert(int index, T item)
        {
            Insert((uint)index, item);
        }

        readonly void IList<T>.RemoveAt(int index)
        {
            RemoveAt((uint)index);
        }

        readonly bool ICollection<T>.Contains(T item)
        {
            Span<T> values = AsSpan();
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < values.Length; i++)
            {
                if (comparer.Equals(values[i], item))
                {
                    return true;
                }
            }

            return false;
        }

        readonly void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            AsSpan().CopyTo(array.AsSpan(arrayIndex));
        }

        readonly bool ICollection<T>.Remove(T item)
        {
            Span<T> values = AsSpan();
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < values.Length; i++)
            {
                if (comparer.Equals(values[i], item))
                {
                    RemoveAt((uint)i);
                    return true;
                }
            }

            return false;
        }

        public static UnmanagedList<T> Create(uint initialCapacity = 1)
        {
            return new(initialCapacity);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly UnsafeList* list;
            private int index;

            public readonly T Current => UnsafeList.Get<T>(list, (uint)index);

            readonly object IEnumerator.Current => UnsafeList.Get<T>(list, (uint)index);

            public Enumerator(UnsafeList* list)
            {
                this.list = list;
                index = -1;
            }

            public bool MoveNext()
            {
                index++;
                return index < UnsafeList.GetCountRef(list);
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
