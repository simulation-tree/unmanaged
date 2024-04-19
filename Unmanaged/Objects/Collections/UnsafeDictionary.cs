﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

        public static void Free(UnsafeDictionary* dictionary)
        {
            UnsafeList.Free(dictionary->keys);
            UnsafeList.Free(dictionary->values);
            Marshal.FreeHGlobal((nint)dictionary);
            dictionary->keyType = default;
            dictionary->valueType = default;
            dictionary->count = 0;
            dictionary->keys = default;
            dictionary->values = default;
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
            return UnsafeList.IsDisposed(dictionary->keys);
        }

        public static uint GetCount(UnsafeDictionary* dictionary)
        {
            return dictionary->count;
        }

        public static UnsafeDictionary* Allocate<K, V>(uint initialCapacity = 0) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            RuntimeType type = RuntimeType.Get<K>();
            nint dictionaryPointer = Marshal.AllocHGlobal(sizeof(UnsafeDictionary));
            UnsafeDictionary* dictionary = (UnsafeDictionary*)dictionaryPointer;
            dictionary->keyType = type;
            dictionary->valueType = RuntimeType.Get<V>();
            dictionary->count = 0;
            dictionary->keys = UnsafeList.Allocate<K>(initialCapacity);
            dictionary->values = UnsafeList.Allocate<V>(initialCapacity);
            return dictionary;
        }

        public static UnsafeDictionary* Allocate(RuntimeType keyType, RuntimeType valueType, uint initialCapacity = 0)
        {
            nint dictionaryPointer = Marshal.AllocHGlobal(sizeof(UnsafeDictionary));
            UnsafeDictionary* dictionary = (UnsafeDictionary*)dictionaryPointer;
            dictionary->keyType = keyType;
            dictionary->valueType = valueType;
            dictionary->count = 0;
            dictionary->keys = UnsafeList.Allocate(keyType, initialCapacity);
            dictionary->values = UnsafeList.Allocate(valueType, initialCapacity);
            return dictionary;
        }

        public static ref V GetValueRef<K, V>(UnsafeDictionary* dictionary, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            ThrowIfSizeMismatch<K, V>(dictionary);
            uint index = UnsafeList.IndexOf<K>(dictionary->keys, key);
            return ref UnsafeList.GetRef<V>(dictionary->values, index);
        }

        public static ref K GetKeyRef<K, V>(UnsafeDictionary* dictionary, uint index) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            ThrowIfSizeMismatch<K, V>(dictionary);
            if (index >= dictionary->count)
            {
                throw new IndexOutOfRangeException();
            }

            return ref UnsafeList.GetRef<K>(dictionary->keys, index);
        }

        public static bool ContainsKey<K, V>(UnsafeDictionary* dictionary, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            ThrowIfSizeMismatch<K, V>(dictionary);
            return UnsafeList.Contains(dictionary->keys, key);
        }

        public static void Add<K, V>(UnsafeDictionary* dictionary, K key, V value) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            ThrowIfSizeMismatch<K, V>(dictionary);
            UnsafeList.Add(dictionary->keys, key);
            UnsafeList.Add(dictionary->values, value);
            dictionary->count++;
        }

        public static void Remove<K, V>(UnsafeDictionary* dictionary, K key) where K : unmanaged, IEquatable<K> where V : unmanaged
        {
            ThrowIfSizeMismatch<K, V>(dictionary);
            uint index = UnsafeList.Remove<K>(dictionary->keys, key);
            UnsafeList.RemoveAt(dictionary->values, index);
            dictionary->count--;
        }

        public static void Clear<K, V>(UnsafeDictionary* dictionary) where K : unmanaged where V : unmanaged
        {
            UnsafeList.Clear(dictionary->keys);
            UnsafeList.Clear(dictionary->values);
            dictionary->count = 0;
        }
    }
}
