using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmanaged.Collections
{
    public unsafe struct UnsafeList
    {
        private RuntimeType type;
        private uint count;
        private UnmanagedBuffer items;

        public UnsafeList()
        {
            throw new InvalidOperationException("Use UnsafeList.Allocate() to create an UnsafeList.");
        }

        public static void Free(UnsafeList* list)
        {
            list->items.Dispose();
            Marshal.FreeHGlobal((nint)list);
            list->type = default;
            list->count = 0;
            list->items = default;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfSizeMismatch<T>(UnsafeList* list) where T : unmanaged
        {
            if (list->type.size != sizeof(T))
            {
                throw new InvalidOperationException("Size mismatch.");
            }
        }

        public static UnsafeList* Allocate<T>(uint initialCapacity = 0) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            nint listPointer = Marshal.AllocHGlobal(sizeof(UnsafeList));
            UnsafeList* list = (UnsafeList*)listPointer;
            list->type = type;
            list->count = 0;
            list->items = new(type.size, initialCapacity);
            return list;
        }

        public static UnsafeList* Allocate(RuntimeType type, uint initialCapacity = 0)
        {
            nint listPointer = Marshal.AllocHGlobal(sizeof(UnsafeList));
            UnsafeList* list = (UnsafeList*)listPointer;
            list->type = type;
            list->count = 0;
            list->items = new(type.size, initialCapacity);
            return list;
        }

        public static UnsafeList* Allocate<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            UnsafeList* list = Allocate<T>((uint)span.Length);
            Span<T> items = list->items.AsSpan<T>();
            span.CopyTo(items);
            list->count = (uint)span.Length;
            return list;
        }

        public static UnsafeList* Allocate<T>(Span<T> span) where T : unmanaged
        {
            UnsafeList* list = Allocate<T>((uint)span.Length);
            Span<T> items = AsSpan<T>(list);
            span.CopyTo(items);
            list->count = (uint)span.Length;
            return list;
        }

        public static ref T GetRef<T>(UnsafeList* list, uint index) where T : unmanaged
        {
            ThrowIfSizeMismatch<T>(list);
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException();
            }

            return ref list->items.GetRef<T>(index);
        }

        public static T Get<T>(UnsafeList* list, uint index) where T : unmanaged
        {
            ThrowIfSizeMismatch<T>(list);
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException($"Trying to access index {index} that is out of range, count: {list->count}");
            }

            return list->items.Get<T>(index);
        }

        public static Span<byte> Get(UnsafeList* list, uint index)
        {
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException();
            }

            return list->items.Get(index);
        }

        public static void Set<T>(UnsafeList* list, uint index, T value) where T : unmanaged
        {
            ThrowIfSizeMismatch<T>(list);
            if (index >= list->count)
            {
                throw new IndexOutOfRangeException();
            }

            list->items.Set(index, value);
        }

        public static void Add<T>(UnsafeList* list, T item) where T : unmanaged
        {
            ThrowIfSizeMismatch<T>(list);
            if (list->count == list->items.length)
            {
                uint newCapacity = list->items.length == 0 ? 1 : list->items.length * 2;
                UnmanagedBuffer newItems = new(list->type.size, newCapacity);
                list->items.CopyTo(newItems);
                list->items.Dispose();
                list->items = newItems;
            }

            list->items.Set(list->count, item);
            list->count++;
        }

        public static bool AddIfUnique<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            ThrowIfSizeMismatch<T>(list);
            Span<T> span = list->items.AsSpan<T>(list->count);
            if (span.Contains(item))
            {
                return false;
            }

            if (list->count == list->items.length)
            {
                uint newCapacity = list->items.length == 0 ? 1 : list->items.length * 2;
                UnmanagedBuffer newItems = new(list->type.size, newCapacity);
                list->items.CopyTo(newItems);
                list->items.Dispose();
                list->items = newItems;
            }

            list->items.Set(list->count, item);
            list->count++;
            return true;
        }

        public static void AddDefault(UnsafeList* list, uint count = 1)
        {
            uint newCount = list->count + count;
            if (newCount >= list->items.length)
            {
                UnmanagedBuffer newItems = new(list->type.size, newCount);
                list->items.CopyTo(newItems);
                list->items.Dispose();
                list->items = newItems;
            }

            Span<byte> bytes = list->items.AsSpan(list->count, count);
            bytes.Clear();
            list->count = newCount;
        }

        public static void AddRange<T>(UnsafeList* list, ReadOnlySpan<T> items) where T : unmanaged
        {
            ThrowIfSizeMismatch<T>(list);
            uint addLength = (uint)items.Length;
            uint newCapacity = list->items.length + addLength;
            if (newCapacity >= list->items.length)
            {
                UnmanagedBuffer newItems = new(list->type.size, newCapacity);
                list->items.CopyTo(newItems);
                list->items.Dispose();
                list->items = newItems;
            }

            Span<T> destination = list->items.AsSpan<T>(list->count, addLength);
            items.CopyTo(destination);
            list->count += addLength;
        }

        public static uint IndexOf<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            ThrowIfSizeMismatch<T>(list);
            Span<T> span = AsSpan<T>(list);
            int result = span.IndexOf(item);
            if (result == -1)
            {
                throw new ArgumentException("Item not found", nameof(item));
            }
            else return (uint)result;
        }

        public static bool TryIndexOf<T>(UnsafeList* list, T item, out uint index) where T : unmanaged, IEquatable<T>
        {
            ThrowIfSizeMismatch<T>(list);
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
            ThrowIfSizeMismatch<T>(list);
            Span<T> span = AsSpan<T>(list);
            return span.Contains(item);
        }

        public static uint Remove<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            ThrowIfSizeMismatch<T>(list);
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

            while (index < count - 1)
            {
                Span<byte> thisElement = list->items.Get(index);
                Span<byte> nextElement = list->items.Get(index + 1);
                nextElement.CopyTo(thisElement);
                index++;
            }

            list->count--;
        }

        public static int GetHashCode(UnsafeList* list)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + list->type.GetHashCode();
                hash = hash * 23 + list->count.GetHashCode();
                hash = hash * 23 + list->items.GetHashCode();
                return hash;
            }
        }

        public static int GetContentHashCode(UnsafeList* list)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + list->type.GetHashCode();
                hash = hash * 23 + list->count.GetHashCode();
                hash = hash * 23 + list->items.GetContentHashCode();
                return hash;
            }
        }

        public static void Clear(UnsafeList* list)
        {
            list->count = 0;
        }

        public static Span<T> AsSpan<T>(UnsafeList* list) where T : unmanaged
        {
            ThrowIfSizeMismatch<T>(list);
            return list->items.AsSpan<T>(list->count);
        }

        public static Span<T> AsSpan<T>(UnsafeList* list, uint start) where T : unmanaged
        {
            ThrowIfSizeMismatch<T>(list);
            if (start >= list->count)
            {
                throw new IndexOutOfRangeException();
            }

            return list->items.AsSpan<T>(start, list->count - start);
        }

        public static Span<T> AsSpan<T>(UnsafeList* list, uint start, uint length) where T : unmanaged
        {
            ThrowIfSizeMismatch<T>(list);
            if (start + length > list->count)
            {
                throw new IndexOutOfRangeException();
            }

            return list->items.AsSpan<T>(start, length);
        }

        public static UnmanagedList<T> AsList<T>(UnsafeList* list) where T : unmanaged
        {
            ThrowIfSizeMismatch<T>(list);
            return new UnmanagedList<T>(list);
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
            return list->items.length;
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

            Span<byte> sourceElement = source->items.Get(sourceIndex);
            Span<byte> destinationElement = destination->items.Get(destinationIndex);
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
