using System;
using System.Diagnostics;

namespace Unmanaged.Collections
{
    public unsafe struct UnsafeDictionary
    {
        private RuntimeType keyType;
        private RuntimeType valueType;
        private uint count;
        private uint capacity;
        private Allocation keys;
        private Allocation values;

        [Conditional("DEBUG")]
        private static void ThrowIfKeySizeMismatches<K>(UnsafeDictionary* dictionary) where K : unmanaged
        {
            if (dictionary->keyType.Size != sizeof(K))
            {
                throw new ArgumentException("Key size doesn't match the expected size.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfValueSizeMismatches<V>(UnsafeDictionary* dictionary) where V : unmanaged
        {
            if (dictionary->valueType.Size != sizeof(V))
            {
                throw new ArgumentException("Value size doesn't match the expected size.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfRange(UnsafeDictionary* dictionary, uint index)
        {
            if (index > dictionary->count)
            {
                throw new ArgumentException($"Index {index} is out of range for dictionary of length {dictionary->count}.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfCapacityIsZero(uint capacity)
        {
            if (capacity == 0)
            {
                throw new InvalidOperationException("Dictionary capacity cannot be zero.");
            }
        }

        public static bool IsDisposed(UnsafeDictionary* dictionary)
        {
            return Allocations.IsNull(dictionary);
        }

        public static uint GetCount(UnsafeDictionary* dictionary)
        {
            Allocations.ThrowIfNull(dictionary);
            return dictionary->count;
        }

        public static UnsafeDictionary* Allocate<K, V>(uint initialCapacity = 1) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            return Allocate(RuntimeType.Get<K>(), RuntimeType.Get<V>(), initialCapacity);
        }

        public static UnsafeDictionary* Allocate(RuntimeType keyType, RuntimeType valueType, uint initialCapacity = 1)
        {
            ThrowIfCapacityIsZero(initialCapacity);
            UnsafeDictionary* dictionary = (UnsafeDictionary*)Allocation.Create<UnsafeDictionary>();
            dictionary->keyType = keyType;
            dictionary->valueType = valueType;
            dictionary->count = 0;
            dictionary->capacity = initialCapacity;
            dictionary->keys = new Allocation(initialCapacity * keyType.Size);
            dictionary->values = new Allocation(initialCapacity * valueType.Size);
            return dictionary;
        }

        public static void Free(ref UnsafeDictionary* dictionary)
        {
            Allocations.ThrowIfNull(dictionary);
            dictionary->keys.Dispose();
            dictionary->values.Dispose();
            Allocations.Free(ref dictionary);
        }

        private static bool TryIndexOf<K>(UnsafeDictionary* dictionary, K key, out uint index) where K : unmanaged, IEquatable<K>
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeySizeMismatches<K>(dictionary);
            uint count = GetCount(dictionary);
            uint keySize = (uint)sizeof(K);
            for (uint i = 0; i < count; i++)
            {
                if (dictionary->keys.Read<K>(i * keySize).Equals(key))
                {
                    index = i;
                    return true;
                }
            }

            index = default;
            return false;
        }

        public static Span<K> GetKeys<K>(UnsafeDictionary* dictionary) where K : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeySizeMismatches<K>(dictionary);
            return dictionary->keys.AsSpan<K>(0, dictionary->count);
        }

        public static ref V GetValueRef<K, V>(UnsafeDictionary* dictionary, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeySizeMismatches<K>(dictionary);
            if (!TryIndexOf(dictionary, key, out uint index))
            {
                throw new NullReferenceException($"The key '{key}' was not found in the dictionary to retrieve.");
            }

            return ref dictionary->values.Read<V>(index * dictionary->valueType.Size);
        }

        public static ref K GetKeyRef<K>(UnsafeDictionary* dictionary, uint index) where K : unmanaged, IEquatable<K>
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeySizeMismatches<K>(dictionary);
            ThrowIfOutOfRange(dictionary, index);
            return ref dictionary->keys.Read<K>(index * (uint)sizeof(K));
        }

        public static bool ContainsKey<K>(UnsafeDictionary* dictionary, K key) where K : unmanaged, IEquatable<K>
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeySizeMismatches<K>(dictionary);
            return TryIndexOf(dictionary, key, out _);
        }

        public static void Add<K, V>(UnsafeDictionary* dictionary, K key, V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeySizeMismatches<K>(dictionary);
            ThrowIfValueSizeMismatches<V>(dictionary);
            if (ContainsKey(dictionary, key))
            {
                throw new ArgumentException($"The key '{key}' already exists in the dictionary.");
            }

            uint keySize = (uint)sizeof(K);
            uint valueSize = (uint)sizeof(V);
            dictionary->keys.Write(dictionary->count * keySize, key);
            dictionary->values.Write(dictionary->count * valueSize, value);
            dictionary->count++;

            ref uint capacity = ref dictionary->capacity;
            if (dictionary->count == capacity)
            {
                capacity *= 2;
                Allocation.Resize(ref dictionary->keys, capacity * keySize);
                Allocation.Resize(ref dictionary->values, capacity * valueSize);
            }
        }

        public static void Remove<K>(UnsafeDictionary* dictionary, K key) where K : unmanaged, IEquatable<K>
        {
            Allocations.ThrowIfNull(dictionary);
            ThrowIfKeySizeMismatches<K>(dictionary);
            if (!TryIndexOf(dictionary, key, out uint index))
            {
                throw new NullReferenceException($"The key '{key}' was not found in the dictionary to remove.");
            }

            //move last element into slot
            ref uint count = ref dictionary->count;
            count--;
            uint keySize = (uint)sizeof(K);
            uint valueSize = dictionary->valueType.Size;
            K lastKey = dictionary->keys.Read<K>(count * keySize);
            dictionary->keys.Write(index * keySize, lastKey);
            Span<byte> lastValue = dictionary->values.AsSpan(count * valueSize, valueSize);
            dictionary->values.Write(index * valueSize, lastValue);
        }

        public static void Clear(UnsafeDictionary* dictionary)
        {
            Allocations.ThrowIfNull(dictionary);
            dictionary->count = 0;
        }
    }
}
