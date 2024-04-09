using System;
using System.Collections;
using System.Collections.Generic;

namespace Unmanaged.Collections
{
    public readonly unsafe struct UnmanagedArray<T> : IDisposable, IReadOnlyList<T> where T : unmanaged
    {
        private readonly UnsafeArray* array;

        public readonly int Length => array->Length;
        public readonly T this[uint index]
        {
            get => UnsafeArray.Get<T>(array, index);
            set => UnsafeArray.Set<T>(array, index, value);
        }

        int IReadOnlyCollection<T>.Count => array->Length;
        T IReadOnlyList<T>.this[int index] => UnsafeArray.GetRef<T>(array, (uint)index);

        public UnmanagedArray()
        {
            throw new InvalidOperationException("UnmanagedArray must be initialized with a length.");
        }

        public UnmanagedArray(uint length)
        {
            array = UnsafeArray.Create<T>(length);
        }

        public readonly void Dispose()
        {
            UnsafeArray.Dispose(array);
        }

        public readonly Span<T> AsSpan()
        {
            return UnsafeArray.AsSpan<T>(array);
        }

        public readonly uint IndexOf<V>(V value) where V : unmanaged, IEquatable<V>
        {
            return UnsafeArray.IndexOf(array, value);
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

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(array);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(array);
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
    }
}
