using System;
using System.Diagnostics;

namespace Unmanaged.Collections
{
    public unsafe struct UnsafeDictionary
    {
        private RuntimeType keyType;
        private RuntimeType valueType;
        private uint count;
        private UnsafeList* keys;
        private UnsafeList* values;

        public UnsafeDictionary()
        {
            throw new InvalidOperationException("Use UnsafeDictionary.Allocate() to create an UnsafeDictionary.");
        }

        [Conditional("DEBUG")]
        private static void ThrowIfSizeMismatch<K, V>(UnsafeDictionary* dictionary) where K : unmanaged where V : unmanaged
        {
            if (dictionary->keyType.size != sizeof(K))
            {
                throw new InvalidOperationException("Key size mismatch.");
            }

            if (dictionary->valueType.size != sizeof(V))
            {
                throw new InvalidOperationException("Value size mismatch.");
            }
        }

        public static bool IsDisposed(UnsafeDictionary* dictionary)
        {
            return Allocations.IsNull(dictionary) || UnsafeList.IsDisposed(dictionary->keys);
        }

        public static uint GetCount(UnsafeDictionary* dictionary)
        {
            Allocations.ThrowIfNull(dictionary);
            return dictionary->count;
        }

        public static UnsafeDictionary* Allocate<K, V>(uint initialCapacity = 1) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            RuntimeType type = RuntimeType.Get<K>();
            UnsafeDictionary* dictionary = Allocations.Allocate<UnsafeDictionary>();
            dictionary->keyType = type;
            dictionary->valueType = RuntimeType.Get<V>();
            dictionary->count = 0;
            dictionary->keys = UnsafeList.Allocate<K>(initialCapacity);
            dictionary->values = UnsafeList.Allocate<V>(initialCapacity);
            return dictionary;
        }

        public static UnsafeDictionary* Allocate(RuntimeType keyType, RuntimeType valueType, uint initialCapacity = 1)
        {
            UnsafeDictionary* dictionary = Allocations.Allocate<UnsafeDictionary>();
            dictionary->keyType = keyType;
            dictionary->valueType = valueType;
            dictionary->count = 0;
            dictionary->keys = UnsafeList.Allocate(keyType, initialCapacity);
            dictionary->values = UnsafeList.Allocate(valueType, initialCapacity);
            return dictionary;
        }

        public static void Free(ref UnsafeDictionary* dictionary)
        {
            Allocations.ThrowIfNull(dictionary);
            UnsafeList.Free(ref dictionary->keys);
            UnsafeList.Free(ref dictionary->values);
            Allocations.Free(ref dictionary);
        }

        public static ref V GetValueRef<K, V>(UnsafeDictionary* dictionary, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfSizeMismatch<K, V>(dictionary);
            uint index = UnsafeList.IndexOf<K>(dictionary->keys, key);
            return ref UnsafeList.GetRef<V>(dictionary->values, index);
        }

        public static ref K GetKeyRef<K, V>(UnsafeDictionary* dictionary, uint index) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfSizeMismatch<K, V>(dictionary);
            if (index >= dictionary->count)
            {
                throw new IndexOutOfRangeException();
            }

            return ref UnsafeList.GetRef<K>(dictionary->keys, index);
        }

        public static bool ContainsKey<K, V>(UnsafeDictionary* dictionary, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfSizeMismatch<K, V>(dictionary);
            return UnsafeList.Contains(dictionary->keys, key);
        }

        public static ReadOnlySpan<K> GetKeys<K>(UnsafeDictionary* dictionary) where K : unmanaged, IEquatable<K>
        {
            Allocations.ThrowIfNull(dictionary);
            return UnsafeList.AsSpan<K>(dictionary->keys);
        }

        public static ReadOnlySpan<V> GetValues<V>(UnsafeDictionary* dictionary) where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            return UnsafeList.AsSpan<V>(dictionary->values);
        }

        public static void Add<K, V>(UnsafeDictionary* dictionary, K key, V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfSizeMismatch<K, V>(dictionary);
            if (UnsafeList.Contains(dictionary->keys, key))
            {
                throw new ArgumentException("An element with the same key already exists.");
            }

            UnsafeList.Add(dictionary->keys, key);
            UnsafeList.Add(dictionary->values, value);
            dictionary->count++;
        }

        public static void Remove<K, V>(UnsafeDictionary* dictionary, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfSizeMismatch<K, V>(dictionary);
            uint index = UnsafeList.IndexOf(dictionary->keys, key);
            UnsafeList.RemoveAtBySwapping<K>(dictionary->keys, index, out _);
            UnsafeList.RemoveAtBySwapping(dictionary->values, index);
            dictionary->count--;
        }

        public static void Clear(UnsafeDictionary* dictionary)
        {
            Allocations.ThrowIfNull(dictionary);
            UnsafeList.Clear(dictionary->keys);
            UnsafeList.Clear(dictionary->values);
            dictionary->count = 0;
        }
    }
}
