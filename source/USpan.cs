using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// Represents a continous region of unmanaged memory
    /// containing <typeparamref name="T"/> elements.
    /// </summary>
#if NET
    [CollectionBuilder(typeof(USpanBuilder), "Create")]
#endif
    public readonly ref struct USpan<T> where T : unmanaged
    {
        private readonly Span<T> value;

        /// <summary>
        /// Ref access to the element at the given index.
        /// </summary>
        public unsafe ref T this[uint index] => ref value[(int)index];

        /// <summary>
        /// Native address of the first element in this span.
        /// </summary>
        public unsafe readonly nint Address => (nint)Pointer;

        /// <summary>
        /// Pointer to the first element in this span.
        /// </summary>
        public unsafe readonly T* Pointer
        {
            get
            {
                fixed (T* pointer = &MemoryMarshal.GetReference(value))
                {
                    return pointer;
                }
            }
        }

        /// <summary>
        /// Amount of <typeparamref name="T"/> elements in this span.
        /// </summary>
        public readonly uint Length => (uint)value.Length;

        /// <summary>
        /// Creates a new span from the given pointer with specified element length.
        /// </summary>
        public unsafe USpan(void* pointer, uint length)
        {
            value = new(pointer, (int)length);
        }

        /// <summary>
        /// Creates a new span starting at the given reference with specified element length.
        /// </summary>
        public unsafe USpan(ref T pointer, uint length)
        {
            fixed (T* p = &pointer)
            {
                value = new(p, (int)length);
            }
        }

        /// <summary>
        /// Creates a new span from the given native address with specified element length.
        /// </summary>
        public unsafe USpan(nint address, uint length)
        {
            value = new((T*)address, (int)length);
        }

        /// <summary>
        /// Initializes a span from an existing <paramref name="value"/>.
        /// </summary>
        public USpan(Span<T> value)
        {
            this.value = value;
        }

        /// <summary>
        /// Initializes a span from an existing <paramref name="value"/>.
        /// </summary>
        public unsafe USpan(ReadOnlySpan<T> value)
        {
            fixed (T* pointer = &MemoryMarshal.GetReference(value))
            {
                this.value = new(pointer, value.Length);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfAccessingOutOfRange(uint index)
        {
            if (index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} must be less than length {Length} of the span");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfAccessingPastRange(uint index)
        {
            if (index > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} must be less than or equal to length {Length} of the span");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDestinationTooSmall(uint length)
        {
            if (length < Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Destination span is too small to fit within {length}");
            }
        }

        [Conditional("DEBUG")]
        private unsafe static void ThrowIfTypeSizeMismatches<V>() where V : unmanaged
        {
            if (sizeof(T) != sizeof(V))
            {
                throw new ArgumentException("Size of type mismatch");
            }
        }

        /// <summary>
        /// Retrieves a string representation of this span.
        /// </summary>
        public unsafe override string ToString()
        {
            if (typeof(T) == typeof(char)) //special case
            {
                char* pointer = (char*)Address;
                return new string(pointer, 0, (int)Length);
            }
            else
            {
                USpan<char> buffer = stackalloc char[32];
                uint length = ToString(buffer);
                return new string((char*)buffer.Address, 0, (int)length);
            }
        }

        /// <summary>
        /// Retrieves a string representation of this span.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            uint length = 0;
            buffer[length++] = 'U';
            buffer[length++] = 'S';
            buffer[length++] = 'p';
            buffer[length++] = 'a';
            buffer[length++] = 'n';
            buffer[length++] = '<';

            string typeName = typeof(T).Name;
            foreach (char c in typeName)
            {
                buffer[length++] = c;
            }

            buffer[length++] = '>';
            buffer[length++] = '[';

            length += Length.ToString(buffer.Slice(length));

            buffer[length++] = ']';
            return length;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash += 31 * hash + Length.GetHashCode();
                hash += 31 * hash + Address.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Retrieves this span as a span of <typeparamref name="X"/> elements,
        /// assuming its of same size.
        /// </summary>
        public readonly USpan<X> As<X>() where X : unmanaged
        {
            ThrowIfTypeSizeMismatches<X>();

            return new(Address, Length);
        }

        /// <summary>
        /// Retrieves a slice of this span starting at the given index with a specified length.
        /// </summary>
        public readonly USpan<T> Slice(uint start, uint length)
        {
            return new(value.Slice((int)start, (int)length));
        }

        /// <summary>
        /// Retrieves a slice of this span starting at the given index.
        /// </summary>
        public readonly USpan<T> Slice(uint start)
        {
            return new(value.Slice((int)start));
        }

        /// <summary>
        /// Retrieves this span as a managed array.
        /// </summary>
        public readonly T[] ToArray()
        {
            T[] array = new T[Length];
            for (uint i = 0; i < Length; i++)
            {
                array[i] = value[(int)i];
            }

            return array;
        }

        /// <summary>
        /// Checks if the span is equal to the given span.
        /// </summary>
        public readonly bool SequenceEqual(USpan<T> other)
        {
            if (Length != other.Length)
            {
                return false;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (uint i = 0; i < Length; i++)
            {
                if (!comparer.Equals(value[(int)i], other.value[(int)i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <summary>
        /// Clears the span to default values.
        /// </summary>
        public readonly void Clear()
        {
            value.Clear();
        }

        /// <summary>
        /// Fills the span with the given value.
        /// </summary>
        public readonly void Fill(T value)
        {
            this.value.Fill(value);
        }

        /// <summary>
        /// Copies the memory of this span to the given <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of values copied.</returns>
        public unsafe readonly uint CopyTo(USpan<T> destination)
        {
            ThrowIfDestinationTooSmall(destination.Length);

            Unsafe.CopyBlockUnaligned(destination.Pointer, Pointer, Length * (uint)sizeof(T));
            return Length;
        }

        /// <summary>
        /// Copies the memory of this span into the <paramref name="destination"/>
        /// with the specified <paramref name="byteLength"/>.
        /// </summary>
        public unsafe readonly void CopyTo(void* destination, uint byteLength)
        {
            ThrowIfAccessingPastRange(byteLength / (uint)sizeof(T));

            Unsafe.CopyBlockUnaligned(destination, Pointer, Length * (uint)sizeof(T));
        }

        /// <summary>
        /// Copies the memory of <paramref name="source"/> into this span.
        /// </summary>
        /// <returns>Amount of values copied.</returns>
        public unsafe readonly uint CopyFrom(USpan<T> source)
        {
            source.ThrowIfDestinationTooSmall(Length);

            Unsafe.CopyBlockUnaligned(Pointer, source.Pointer, Length * (uint)sizeof(T));
            return source.Length;
        }

        /// <summary>
        /// Copies the memory from <paramref name="source"/> into this span
        /// with the specified <paramref name="byteLength"/>.
        /// </summary>
        public unsafe readonly void CopyFrom(void* source, uint byteLength)
        {
            ThrowIfAccessingPastRange(byteLength / (uint)sizeof(T));

            Unsafe.CopyBlockUnaligned(Pointer, source, Length * (uint)sizeof(T));
        }

        /// <inheritdoc/>
        public static unsafe implicit operator USpan<T>(Span<T> span)
        {
            return new(span);
        }

        /// <inheritdoc/>
        public static implicit operator USpan<T>(ReadOnlySpan<T> span)
        {
            return new(span);
        }

        /// <inheritdoc/>
        public static implicit operator USpan<T>(T[] array)
        {
            return new(array);
        }

        /// <inheritdoc/>
        public static unsafe implicit operator Span<T>(USpan<T> span)
        {
            return span.value;
        }

        /// <inheritdoc/>
        public static unsafe implicit operator ReadOnlySpan<T>(USpan<T> span)
        {
            return span.value;
        }

        /// <summary>
        /// Enumerator for <see cref="USpan{T}"/>.
        /// </summary>
        public ref struct Enumerator : IEnumerator<T>
        {
            private readonly USpan<T> span;
            private int index;

            /// <summary>
            /// Current element in the span.
            /// </summary>
            public readonly ref T Current => ref span[(uint)index];
            readonly T IEnumerator<T>.Current => Current;
            readonly object IEnumerator.Current => Current;

            internal Enumerator(USpan<T> span)
            {
                this.span = span;
                index = -1;
            }

            /// <summary>
            /// Iterates to the next element in the span.
            /// </summary>
            public bool MoveNext()
            {
                int index = this.index + 1;
                if (index < span.Length)
                {
                    this.index = index;
                    return true;
                }

                return false;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                index = -1;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
            }
        }
    }

    /// <summary>
    /// Allows for the creation of <see cref="USpan{T}"/> instances using the collection expression.
    /// </summary>
    public static class USpanBuilder
    {
        /// <inheritdoc/>
        public static USpan<T> Create<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            return values;
        }
    }
}
