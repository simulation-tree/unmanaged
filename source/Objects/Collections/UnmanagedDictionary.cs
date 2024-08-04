using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Unmanaged.Collections
{
    public unsafe struct UnmanagedDictionary<K, V> : IDisposable, IEquatable<UnmanagedDictionary<K, V>> where K : unmanaged, IEquatable<K> where V : unmanaged
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

        public UnmanagedDictionary(UnsafeDictionary* dictionary)
        {
            value = dictionary;
        }

        public UnmanagedDictionary(uint initialCapacity)
        {
            value = UnsafeDictionary.Allocate<K, V>(initialCapacity);
        }

#if NET5_0_OR_GREATER
        public UnmanagedDictionary()
        {
            value = UnsafeDictionary.Allocate<K, V>();
        }
#endif
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

        public readonly ref V AddRef(K key)
        {
            UnsafeDictionary.Add<K, V>(this.value, key, default);
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
            UnsafeDictionary.Clear(value);
        }

        public static UnmanagedDictionary<K, V> Create(uint capacity = 1)
        {
            UnsafeDictionary* value = UnsafeDictionary.Allocate<K, V>(capacity);
            return new UnmanagedDictionary<K, V>(value);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is UnmanagedDictionary<K, V> dictionary && Equals(dictionary);
        }

        public readonly bool Equals(UnmanagedDictionary<K, V> other)
        {
            int hash = GetHashCode();
            int otherHash = other.GetHashCode();
            return hash == otherHash;
        }

        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                ReadOnlySpan<K> keys = Keys;
                foreach (K key in keys)
                {
                    hash = hash * 31 + key.GetHashCode();
                }

                ReadOnlySpan<V> values = Values;
                foreach (V value in values)
                {
                    hash = hash * 31 + value.GetHashCode();
                }

                return hash;
            }
        }

        public static bool operator ==(UnmanagedDictionary<K, V> left, UnmanagedDictionary<K, V> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnmanagedDictionary<K, V> left, UnmanagedDictionary<K, V> right)
        {
            return !(left == right);
        }
    }
}
