using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmanaged.Collections
{
    public unsafe struct UnsafeList
    {
        private RuntimeType type;
        private uint count;
        private Allocation items;

        public UnsafeList()
        {
            throw new InvalidOperationException("Use UnsafeList.Allocate() to create an UnsafeList.");
        }

        [Conditional("DEBUG")]
        private static void ThrowIfLengthIsZero(uint value)
        {
            if (value == 0)
            {
                throw new InvalidOperationException("Allocation capacity cannot be zero.");
            }
        }

        public static void Free(UnsafeList* list)
        {
            list->items.Dispose();
            Marshal.FreeHGlobal((nint)list);
            list->count = 0;
        }

        public static UnsafeList* Allocate<T>(uint initialCapacity = 1) where T : unmanaged
        {
            Console.WriteLine("allocating");
            ThrowIfLengthIsZero(initialCapacity);
            RuntimeType type = RuntimeType.Get<T>();
            Console.WriteLine("size:" + type.size);
            Console.WriteLine("type: " + type);
            UnsafeList* list = (UnsafeList*)Marshal.AllocHGlobal(sizeof(UnsafeList));
            Console.WriteLine((nint)list);
            list->type = type;
            list->count = 0;
            list->items = new(type.size * initialCapacity);
            return list;
        }

        public static UnsafeList* Allocate(RuntimeType type, uint initialCapacity = 1)
        {
            ThrowIfLengthIsZero(initialCapacity);
            UnsafeList* list = (UnsafeList*)Marshal.AllocHGlobal(sizeof(UnsafeList));
            list->type = type;
            list->count = 0;
            list->items = new(type.size * initialCapacity);
            return list;
        }

        public static UnsafeList* Allocate<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            UnsafeList* list = (UnsafeList*)Marshal.AllocHGlobal(sizeof(UnsafeList));
            list->type = type;
            list->count = (uint)span.Length;
            list->items = new(list->count * type.size);
            Span<T> items = list->items.AsSpan<T>();
            span.CopyTo(items);
            return list;
        }

        public static UnsafeList* Allocate<T>(Span<T> span) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            UnsafeList* list = (UnsafeList*)Marshal.AllocHGlobal(sizeof(UnsafeList));
            list->type = type;
            list->count = (uint)span.Length;
            list->items = new(list->count * type.size);
            Span<T> items = list->items.AsSpan<T>();
            span.CopyTo(items);
            return list;
        }

        public static ref T GetRef<T>(UnsafeList* list, uint index) where T : unmanaged
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException();
            }

            Span<T> span = list->items.AsSpan<T>();
            return ref span[(int)index];
        }

        public static T Get<T>(UnsafeList* list, uint index) where T : unmanaged
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} that is out of range, count: {list->count}");
            }

            Span<T> span = list->items.AsSpan<T>();
            return span[(int)index];
        }

        /// <summary>
        /// Returns the bytes for the element at the given index.
        /// </summary>
        public static Span<byte> Get(UnsafeList* list, uint index)
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException();
            }

            uint elementSize = list->type.size;
            return list->items.AsSpan<byte>(index * elementSize, elementSize);
        }

        public static void Set<T>(UnsafeList* list, uint index, T value) where T : unmanaged
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException();
            }

            Span<T> span = list->items.AsSpan<T>();
            span[(int)index] = value;
        }

        public static void Insert<T>(UnsafeList* list, uint index, T item) where T : unmanaged
        {
            if (index > list->count)
            {
                throw new IndexOutOfRangeException();
            }

            uint elementSize = list->type.size;
            uint capacity = GetCapacity(list);
            if (list->count == capacity)
            {
                uint newCapacity = capacity * 2;
                Allocation newItems = new(elementSize * newCapacity);
                list->items.CopyTo(newItems);
                list->items.Dispose();
                list->items = newItems;
            }

            Span<byte> destination = list->items.AsSpan<byte>((index + 1) * elementSize, (list->count - index) * elementSize);
            Span<byte> source = list->items.AsSpan<byte>(index * elementSize, (list->count - index) * elementSize);
            source.CopyTo(destination);
            list->items.Write(index * elementSize, item);
            list->count++;
        }

        public static void Add<T>(UnsafeList* list, T item) where T : unmanaged
        {
            uint elementSize = list->type.size;
            uint capacity = GetCapacity(list);
            if (list->count == capacity)
            {
                uint newCapacity = capacity * 2;
                Allocation newItems = new(elementSize * newCapacity);
                list->items.CopyTo(newItems);
                list->items.Dispose();
                list->items = newItems;
            }

            list->items.Write(list->count * elementSize, item);
            list->count++;
        }

        public static bool AddIfUnique<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            Span<T> span = list->items.AsSpan<T>(0, list->count);
            if (span.Contains(item))
            {
                return false;
            }

            uint elementSize = list->type.size;
            uint capacity = GetCapacity(list);
            if (list->count == capacity)
            {
                uint newCapacity = capacity * 2;
                Allocation newItems = new(elementSize * newCapacity);
                list->items.CopyTo(newItems);
                list->items.Dispose();
                list->items = newItems;
            }

            list->items.Write(list->count * elementSize, item);
            list->count++;
            return true;
        }

        public static void AddDefault(UnsafeList* list, uint count = 1)
        {
            uint elementSize = list->type.size;
            uint newCount = list->count + count;
            if (newCount >= GetCapacity(list))
            {
                Allocation newItems = new(elementSize * newCount);
                list->items.CopyTo(newItems);
                list->items.Dispose();
                list->items = newItems;
            }

            Span<byte> bytes = list->items.AsSpan<byte>(list->count * elementSize, count * elementSize);
            bytes.Clear();
            list->count = newCount;
        }

        public static void AddRange<T>(UnsafeList* list, ReadOnlySpan<T> items) where T : unmanaged
        {
            uint capacity = GetCapacity(list);
            uint addLength = (uint)items.Length;
            uint newCount = list->count + addLength;
            if (newCount >= capacity)
            {
                Allocation newItems = new(list->type.size * newCount);
                list->items.CopyTo(newItems);
                list->items.Dispose();
                list->items = newItems;
            }

            Span<T> destination = list->items.AsSpan<T>(list->count, addLength);
            items.CopyTo(destination);
            list->count = newCount;
        }

        public static uint IndexOf<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            Span<T> span = AsSpan<T>(list);
            int result = span.IndexOf(item);
            if (result == -1)
            {
                throw new NullReferenceException($"Item {item} not found in list.");
            }
            else return (uint)result;
        }

        public static bool TryIndexOf<T>(UnsafeList* list, T item, out uint index) where T : unmanaged, IEquatable<T>
        {
            Span<T> span = AsSpan<T>(list);
            int result = span.IndexOf(item);
            if (result == -1)
            {
                index = 0;
                return false;
            }
            else
            {
                index = (uint)result;
                return true;
            }
        }

        public static bool Contains<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            Span<T> span = AsSpan<T>(list);
            return span.Contains(item);
        }

        public static uint Remove<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            uint index = IndexOf(list, item);
            RemoveAt(list, index);
            return index;
        }

        public static void RemoveAt(UnsafeList* list, uint index)
        {
            uint count = list->count;
            if (index >= count)
            {
                throw new IndexOutOfRangeException();
            }

            uint elementSize = list->type.size;
            while (index < count - 1)
            {
                Span<byte> thisElement = list->items.AsSpan<byte>(index * elementSize, elementSize);
                Span<byte> nextElement = list->items.AsSpan<byte>((index + 1) * elementSize, elementSize);
                nextElement.CopyTo(thisElement);
                index++;
            }

            list->count--;
        }

        public static void RemoveAt<T>(UnsafeList* list, uint index, out T removed) where T : unmanaged, IEquatable<T>
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException();
            }

            Span<T> span = list->items.AsSpan<T>();
            removed = span[(int)index];
            RemoveAt(list, index);
        }

        public static int GetContentHashCode(UnsafeList* list)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + list->type.GetHashCode();
                hash = hash * 23 + list->count.GetHashCode();
                hash = hash * 23 + Djb2Hash.GetDjb2HashCode(list->items.AsSpan<byte>());
                return hash;
            }
        }

        public static void Clear(UnsafeList* list)
        {
            list->count = 0;
        }

        public static Span<T> AsSpan<T>(UnsafeList* list) where T : unmanaged
        {
            return list->items.AsSpan<T>(0, list->count);
        }

        public static Span<T> AsSpan<T>(UnsafeList* list, uint start) where T : unmanaged
        {
            if (start >= list->count)
            {
                throw new IndexOutOfRangeException();
            }

            return list->items.AsSpan<T>(start, list->count - start);
        }

        public static Span<T> AsSpan<T>(UnsafeList* list, uint start, uint length) where T : unmanaged
        {
            if (start + length > list->count)
            {
                throw new IndexOutOfRangeException();
            }

            return list->items.AsSpan<T>(start, length);
        }

        public static bool IsDisposed(UnsafeList* list)
        {
            return list->items.IsDisposed;
        }

        public static uint GetCount(UnsafeList* list)
        {
            return list->count;
        }

        public static uint GetCapacity(UnsafeList* list)
        {
            return list->items.length / list->type.size;
        }

        public static void CopyTo(UnsafeList* source, uint sourceIndex, UnsafeList* destination, uint destinationIndex)
        {
            if (sourceIndex >= source->count)
            {
                throw new IndexOutOfRangeException();
            }

            if (destinationIndex >= destination->count)
            {
                throw new IndexOutOfRangeException();
            }

            uint elementSize = source->type.size;
            Span<byte> sourceElement = source->items.AsSpan<byte>(sourceIndex * elementSize, elementSize);
            Span<byte> destinationElement = destination->items.AsSpan<byte>((destinationIndex) * elementSize, elementSize);
            sourceElement.CopyTo(destinationElement);
        }

        public static void CopyTo<T>(UnsafeList* source, uint sourceIndex, Span<T> destination, uint destinationIndex) where T : unmanaged
        {
            if (sourceIndex >= source->count)
            {
                throw new IndexOutOfRangeException();
            }

            if (destinationIndex + source->count - sourceIndex > destination.Length)
            {
                throw new ArgumentException("Destination span is too small to fit destination index.");
            }

            Span<T> sourceSpan = AsSpan<T>(source, sourceIndex);
            sourceSpan.CopyTo(destination[(int)destinationIndex..]);
        }
    }
}
