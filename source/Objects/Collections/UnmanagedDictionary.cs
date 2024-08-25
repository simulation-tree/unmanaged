using System;
using System.Runtime.CompilerServices;

namespace Unmanaged.Collections
{
    public unsafe struct UnmanagedDictionary<K, V> : IDisposable, IEquatable<UnmanagedDictionary<K, V>> where K : unmanaged, IEquatable<K> where V : unmanaged
    {
        private UnsafeDictionary* value;

        public readonly uint Count => UnsafeDictionary.GetCount(value);
        public readonly bool IsDisposed => UnsafeDictionary.IsDisposed(value);

        public readonly ref V this[K key]
        {
            get
            {
                if (ContainsKey(key))
                {
                    return ref UnsafeDictionary.GetValueRef<K, V>(value, key);
                }
                else
                {
                    throw new NullReferenceException($"The key `{key}` was not found in the dictionary.");
                }
            }
        }

        public readonly ReadOnlySpan<K> Keys => UnsafeDictionary.GetKeys<K>(value);

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
            return UnsafeDictionary.ContainsKey(value, key);
        }

        public readonly K GetKeyAtIndex(uint index)
        {
            if (index >= Count)
            {
                throw new IndexOutOfRangeException($"The index `{index}` was out of range.");
            }

            return UnsafeDictionary.GetKeyRef<K>(value, index);
        }

        public readonly bool TryGetValue(K key, out V value)
        {
            if (ContainsKey(key))
            {
                value = UnsafeDictionary.GetValueRef<K, V>(this.value, key);
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
                return ref UnsafeDictionary.GetValueRef<K, V>(value, key);
            }
            else
            {
                found = false;
                return ref Unsafe.AsRef<V>(null);
            }
        }

        public readonly void Add(K key, V value)
        {
            UnsafeDictionary.Add(this.value, key, value);
        }

        public readonly void AddOrSet(K key, V value)
        {
            if (ContainsKey(key))
            {
                ref V existingValue = ref UnsafeDictionary.GetValueRef<K, V>(this.value, key);
                existingValue = value;
            }
            else
            {
                Add(key, value);
            }
        }

        public readonly ref V AddRef(K key)
        {
            UnsafeDictionary.Add<K, V>(value, key, default);
            return ref UnsafeDictionary.GetValueRef<K, V>(value, key);
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
            V existingValue = UnsafeDictionary.GetValueRef<K, V>(value, key);
            UnsafeDictionary.Remove(value, key);
            return existingValue;
        }

        public readonly bool TryRemove(K key, out V removed)
        {
            if (ContainsKey(key))
            {
                removed = Remove(key);
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
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            int hash = GetHashCode();
            int otherHash = other.GetHashCode();
            return hash == otherHash;
        }

        public readonly override int GetHashCode()
        {
            unchecked
            {
                return (int)value;
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
