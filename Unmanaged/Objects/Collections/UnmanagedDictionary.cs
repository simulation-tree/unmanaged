using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Unmanaged.Collections
{
    public unsafe struct UnmanagedDictionary<K, V> : IDisposable where K : unmanaged, IEquatable<K> where V : unmanaged
    {
        private UnsafeDictionary* value;

        public readonly uint Count => UnsafeDictionary.GetCount(value);
        public readonly bool IsDisposed => UnsafeDictionary.IsDisposed(value);
        public readonly ReadOnlySpan<K> Keys => UnsafeDictionary.GetKeys<K>(value);
        public readonly ReadOnlySpan<V> Values => UnsafeDictionary.GetValues<V>(value);

        public readonly V this[K key]
        {
            get
            {
                if (ContainsKey(key))
                {
                    return GetRef(key);
                }
                else
                {
                    throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary.");
                }
            }
            set
            {
                if (ContainsKey(key))
                {
                    ref var v = ref GetRef(key);
                    v = value;
                }
                else
                {
                    ref var v = ref AddRef(key, default);
                    v = value;
                }
            }
        }

        public UnmanagedDictionary()
        {
            value = UnsafeDictionary.Allocate<K, V>();
        }

        public UnmanagedDictionary(uint initialCapacity)
        {
            value = UnsafeDictionary.Allocate<K, V>(initialCapacity);
        }

        public void Dispose()
        {
            UnsafeDictionary.Free(ref value);
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

        public readonly ref V TryGetValueRef(K key, out bool found)
        {
            if (ContainsKey(key))
            {
                found = true;
                return ref GetRef(key);
            }
            else
            {
                found = false;
                return ref Unsafe.AsRef<V>(null);
            }
        }

        public readonly void Add(K key, V value)
        {
            UnsafeDictionary.Add<K, V>(this.value, key, value);
        }

        public readonly ref V AddRef(K key, V value)
        {
            UnsafeDictionary.Add<K, V>(this.value, key, value);
            return ref GetRef(key);
        }

        public readonly bool TryAdd(K key, V value)
        {
            if (ContainsKey(key))
            {
                return false;
            }
            else
            {
                Add(key, value);
                return true;
            }
        }

        public readonly V Remove(K key)
        {
            V removed = GetRef(key);
            UnsafeDictionary.Remove<K, V>(value, key);
            return removed;
        }

        public readonly bool TryRemove(K key, out V removed)
        {
            if (ContainsKey(key))
            {
                removed = GetRef(key);
                Remove(key);
                return true;
            }
            else
            {
                removed = default;
                return false;
            }
        }

        public readonly void Clear()
        {
            UnsafeDictionary.Clear<K, V>(value);
        }
    }
}
