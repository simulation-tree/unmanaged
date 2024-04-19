using System;

namespace Unmanaged.Collections
{
    public readonly unsafe struct UnmanagedDictionary<K, V> : IDisposable where K : unmanaged, IEquatable<K> where V : unmanaged
    {
        private readonly UnsafeDictionary* value;

        public readonly uint Count => UnsafeDictionary.GetCount(value);
        public readonly bool IsDisposed => UnsafeDictionary.IsDisposed(value);

        public UnmanagedDictionary()
        {
            value = UnsafeDictionary.Allocate<K, V>();
        }

        public UnmanagedDictionary(uint initialCapacity)
        {
            value = UnsafeDictionary.Allocate<K, V>(initialCapacity);
        }

        public readonly void Dispose()
        {
            UnsafeDictionary.Free(value);
        }

        public readonly bool ContainsKey(K key)
        {
            return UnsafeDictionary.ContainsKey<K, V>(value, key);
        }

        public readonly ref V GetRef(K key)
        {
            return ref UnsafeDictionary.GetValueRef<K, V>(value, key);
        }

        public readonly bool TryGetValue(K key, out V value)
        {
            if (ContainsKey(key))
            {
                value = GetRef(key);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public readonly void Remove(K key)
        {
            UnsafeDictionary.Remove<K, V>(value, key);
        }

        public readonly void Clear()
        {
            UnsafeDictionary.Clear<K, V>(value);
        }
    }
}
