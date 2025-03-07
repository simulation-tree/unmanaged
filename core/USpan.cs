using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// Represents a continous region of unmanaged memory
    /// containing <typeparamref name="T"/> elements.
    /// Restricted to work with the <see cref="uint"/> set of numbers.
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
        public unsafe ref T this[uint index]
        {
            get
            {
                ThrowIfAccessingOutOfRange(index);
                unchecked
                {
                    return ref value[(int)index];
                }
            }
        }

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
                fixed (T* pointer = value)
                {
                    return pointer;
                }
            }
        }

        /// <summary>
        /// Amount of <typeparamref name="T"/> elements in this span.
        /// </summary>
        public unsafe readonly uint Length
        {
            get
            {
                unchecked
                {
                    return (uint)value.Length;
                }
            }
        }

        /// <summary>
        /// Checks if this span is empty.
        /// </summary>
        public readonly bool IsEmpty => value.IsEmpty;

        /// <summary>
        /// Creates a new span from the given pointer with element <paramref name="length"/>.
        /// </summary>
        public unsafe USpan(void* pointer, uint length)
        {
            unchecked
            {
                value = new(pointer, (int)length);
            }
        }

        /// <summary>
        /// Creates a new span starting at the given reference with specified element <paramref name="length"/>.
        /// </summary>
        public unsafe USpan(ref T pointer, uint length)
        {
            unchecked
            {
                fixed (T* p = &pointer)
                {
                    value = new(p, (int)length);
                }
            }
        }

        /// <summary>
        /// Creates a new span from the given native address with specified element <paramref name="length"/>.
        /// </summary>
        public unsafe USpan(nint address, uint length)
        {
            unchecked
            {
                value = new((T*)address, (int)length);
            }
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
                throw new ArgumentOutOfRangeException(nameof(length), $"Destination span of length {length} is too small to contain another span of length {Length}");
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

        /// <summary>
        /// Retrieves a hash based on the address and length of this span.
        /// </summary>
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
        /// Retrieves a hash based on the contents of the span.
        /// </summary>
        public readonly int GetContentHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (uint i = 0; i < Length; i++)
                {
                    hash += 31 * hash + this[i].GetHashCode();
                }

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
        /// Reinterprets this span to be of type <typeparamref name="X"/>.
        /// <para>
        /// Length will change to fit the new type.
        /// </para>
        /// </summary>
        public unsafe readonly USpan<X> Reinterpret<X>() where X : unmanaged
        {
            unchecked
            {
                uint newLength = Length * (uint)sizeof(T) / (uint)sizeof(X);
                return new(Address, newLength);
            }
        }

        /// <summary>
        /// Retrieves a span of the specified <paramref name="length"/> from the start.
        /// </summary>
        public unsafe readonly USpan<T> GetSpan(uint length)
        {
            unchecked
            {
                ThrowIfAccessingPastRange(length);

                fixed (T* pointer = value)
                {
                    return new(pointer, length);
                }
            }
        }

        /// <summary>
        /// Retrieves a slice of <paramref name="length"/> from <paramref name="start"/>.
        /// </summary>
        public unsafe readonly USpan<T> Slice(uint start, uint length)
        {
            unchecked
            {
                ThrowIfAccessingPastRange(start + length);

                fixed (T* pointer = value)
                {
                    return new(pointer + start, length);
                }
            }
        }

        /// <summary>
        /// Retrieves a slice of this span starting at the given index.
        /// </summary>
        public unsafe readonly USpan<T> Slice(uint start)
        {
            unchecked
            {
                ThrowIfAccessingPastRange(start);

                fixed (T* pointer = value)
                {
                    return new(pointer + start, (uint)value.Length - start);
                }
            }
        }

        /// <summary>
        /// Retrieves the slice that the given <paramref name="range"/> represents.
        /// </summary>
        public readonly USpan<T> Slice(URange range)
        {
            return Slice(range.start, range.Length);
        }

        /// <summary>
        /// Retrieves this span as a managed array.
        /// </summary>
        public readonly T[] ToArray()
        {
            unchecked
            {
                T[] array = new T[Length];
                for (uint i = 0; i < Length; i++)
                {
                    array[i] = value[(int)i];
                }

                return array;
            }
        }

        /// <inheritdoc/>
        public readonly Span<T>.Enumerator GetEnumerator()
        {
            return value.GetEnumerator();
        }

        /// <summary>
        /// Clears the span to default values.
        /// </summary>
        public readonly void Clear()
        {
            //todo: efficiency: this has a branch for whether or not the span contains references
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
        public readonly void CopyTo(USpan<T> destination)
        {
            ThrowIfDestinationTooSmall(destination.Length);

            value.CopyTo(destination.value);
        }

        /// <summary>
        /// Copies the memory of this span into the <paramref name="destination"/>
        /// with the specified <paramref name="byteLength"/>.
        /// </summary>
        public unsafe readonly void CopyTo(void* destination, uint byteLength)
        {
            ThrowIfAccessingPastRange(byteLength / (uint)sizeof(T));

            unchecked
            {
                Buffer.MemoryCopy(Pointer, destination, byteLength, byteLength);
            }
        }

        /// <summary>
        /// Copies the memory of <paramref name="source"/> into this span.
        /// </summary>
        public readonly void CopyFrom(USpan<T> source)
        {
            source.ThrowIfDestinationTooSmall(Length);

            source.value.CopyTo(value);
        }

        /// <summary>
        /// Copies the memory from <paramref name="source"/> into this span
        /// with the specified <paramref name="byteLength"/>.
        /// </summary>
        public unsafe readonly void CopyFrom(void* source, uint byteLength)
        {
            ThrowIfAccessingPastRange(byteLength / (uint)sizeof(T));

            unchecked
            {
                Buffer.MemoryCopy(source, Pointer, byteLength, byteLength);
            }
        }

        /// <inheritdoc/>
        public static implicit operator USpan<T>(Span<T> span)
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
        public static implicit operator Span<T>(USpan<T> span)
        {
            return span.value;
        }

        /// <inheritdoc/>
        public static implicit operator ReadOnlySpan<T>(USpan<T> span)
        {
            return span.value;
        }

        /// <inheritdoc/>
        public static unsafe implicit operator T*(USpan<T> span)
        {
            return span.Pointer;
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