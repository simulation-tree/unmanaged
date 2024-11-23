using System;
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
        //todo: efficiency: take advantage of vector256 and vector512

        private readonly ref T pointer;
        private readonly uint length;

        /// <summary>
        /// Ref access to the element at the given index.
        /// </summary>
        public ref T this[uint index]
        {
            get
            {
                ThrowIfAccessingOutOfRange(index);
#if NET
                return ref Unsafe.Add(ref pointer, index);
#else
                return ref Unsafe.Add(ref pointer, (int)index);
#endif
            }
        }

        /// <summary>
        /// Native address of the first element in this span.
        /// </summary>
        public readonly nint Address
        {
            get
            {
                unsafe
                {
                    return (nint)Unsafe.AsPointer(ref pointer);
                }
            }
        }

        /// <summary>
        /// Amount of <typeparamref name="T"/> elements in this span.
        /// </summary>
        public readonly uint Length => length;

        /// <summary>
        /// Creates a new span from the given pointer with specified element length.
        /// </summary>
        public unsafe USpan(void* pointer, uint length)
        {
            this.pointer = ref Unsafe.AsRef<T>(pointer);
            this.length = length;
        }

        /// <summary>
        /// Creates a new span starting at the given reference with specified element length.
        /// </summary>
        public USpan(ref T pointer, uint length)
        {
            this.pointer = ref pointer;
            this.length = length;
        }

        /// <summary>
        /// Creates a new span from the given native address with specified element length.
        /// </summary>
        public unsafe USpan(nint address, uint length)
        {
            this.pointer = ref Unsafe.AsRef<T>((void*)address);
            this.length = length;
        }

        /// <summary>
        /// Creates a new span from a <see cref="Span{T}"/> value.
        /// </summary>
        public USpan(Span<T> span)
        {
            length = (uint)span.Length;
            if (length > 0)
            {
                pointer = ref span[0];
            }
        }

        /// <summary>
        /// Creates a new span from a <see cref="ReadOnlySpan{T}"/> value.
        /// </summary>
        public USpan(ReadOnlySpan<T> span)
        {
            length = (uint)span.Length;
            if (length > 0)
            {
                pointer = ref MemoryMarshal.GetReference(span);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfAccessingOutOfRange(uint index)
        {
            if (index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} must be less than length {length} of the span");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfAccessingPastRange(uint index)
        {
            if (index > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} must be less than or equal to length {length} of the span");
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
        private static void ThrowIfTypeSizeMismatches<V>() where V : unmanaged
        {
            if (TypeInfo<T>.size != TypeInfo<V>.size)
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
        /// Retrieves this span as a <see cref="Span{T}"/> value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe readonly Span<T> AsSystemSpan()
        {
            return new Span<T>((void*)Address, (int)length);
        }

        /// <summary>
        /// Retrieves this span as a <see cref="Span{T}"/> value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe readonly Span<V> AsSystemSpan<V>() where V : unmanaged
        {
            ThrowIfTypeSizeMismatches<V>();
            return new Span<V>((void*)Address, (int)length);
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
        /// Retrieves this span as a span of another type of
        /// the same size.
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
            ThrowIfAccessingPastRange(start + length);

#if NET
            return new USpan<T>(ref Unsafe.Add(ref pointer, start), length);
#else
            return new USpan<T>(ref Unsafe.Add(ref pointer, (int)start), length);
#endif
        }

        /// <summary>
        /// Retrieves a slice of this span starting at the given index.
        /// </summary>
        public readonly USpan<T> Slice(uint start)
        {
            ThrowIfAccessingPastRange(start);

#if NET
            return new USpan<T>(ref Unsafe.Add(ref pointer, start), Length - start);
#else
            return new USpan<T>(ref Unsafe.Add(ref pointer, (int)start), Length - start);
#endif
        }

        /// <summary>
        /// Retrieves the index of the first occurrence of the given value.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public readonly uint IndexOf<V>(V value) where V : unmanaged, IEquatable<V>
        {
            ThrowIfTypeSizeMismatches<V>();
            unchecked
            {
                for (uint i = 0; i < length; i++)
                {
#if NET
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, i));
#else
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, (int)i));
#endif
                    if (e.Equals(value))
                    {
                        return i;
                    }
                }

                return uint.MaxValue;
            }
        }

        /// <summary>
        /// Retrieves the index of the last occurrence of the given value.
        /// <para>
        /// Will be <see cref="uint.MaxValue"/> if not found.
        /// </para>
        /// </summary>
        public readonly uint LastIndexOf<V>(V value) where V : unmanaged, IEquatable<V>
        {
            ThrowIfTypeSizeMismatches<V>();
            unchecked
            {
                for (uint i = length - 1; i != uint.MaxValue; i--)
                {
#if NET
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, i));
#else
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, (int)i));
#endif
                    if (e.Equals(value))
                    {
                        return i;
                    }
                }

                return uint.MaxValue;
            }
        }

        /// <summary>
        /// Attempts to retrieve the index of the first occurrence of the given value.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryIndexOfSlow(T value, out uint index)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (uint i = 0; i < Length; i++)
            {
#if NET
                ref T e = ref Unsafe.Add(ref pointer, i);
#else
                ref T e = ref Unsafe.Add(ref pointer, (int)i);
#endif
                if (comparer.Equals(e, value))
                {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the index of the first occurrence of the given value.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryIndexOf<V>(V value, out uint index) where V : unmanaged, IEquatable<V>
        {
            ThrowIfTypeSizeMismatches<V>();
            unchecked
            {
                for (uint i = 0; i < length; i++)
                {
#if NET
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, i));
#else
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, (int)i));
#endif
                    if (e.Equals(value))
                    {
                        index = i;
                        return true;
                    }
                }

                index = 0;
                return false;
            }
        }

        /// <summary>
        /// Attempts to retrieve the index of the last occurrence of the given value.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryLastIndexOf<V>(V value, out uint index) where V : unmanaged, IEquatable<V>
        {
            ThrowIfTypeSizeMismatches<V>();
            unchecked
            {
                for (uint i = length - 1; i != uint.MaxValue; i--)
                {
#if NET
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, i));
#else
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, (int)i));
#endif
                    if (e.Equals(value))
                    {
                        index = i;
                        return true;
                    }
                }

                index = 0;
                return false;
            }
        }

        /// <summary>
        /// Retrieves the index of the first occurrence of the given value.
        /// <para>May throw <see cref="ArgumentException"/> if not found.</para>
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public readonly uint IndexOf(USpan<T> span)
        {
            for (uint i = 0; i < Length; i++)
            {
                USpan<T> left = Slice(i, span.Length);
                if (left.SequenceEqual(span))
                {
                    return i;
                }
            }

            throw new ArgumentException($"Span `{span.ToString()}` not found", nameof(span));
        }

        /// <summary>
        /// Attempts to retrieve the index of the first occurrence of the given value.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryIndexOf(USpan<T> span, out uint index)
        {
            for (uint i = 0; i < Length; i++)
            {
                if (Slice(i, span.Length).SequenceEqual(span))
                {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        /// <summary>
        /// Checks if the span contains the given value.
        /// </summary>
        public readonly bool ContainsSlow(T value)
        {
            return TryIndexOfSlow(value, out _);
        }

        /// <summary>
        /// Checks if the span contains the given value.
        /// </summary>
        public readonly bool Contains<V>(V value) where V : unmanaged, IEquatable<V>
        {
            ThrowIfTypeSizeMismatches<V>();
            for (uint i = 0; i < Length; i++)
            {
#if NET
                ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, i));
#else
                ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, (int)i));
#endif
                if (e.Equals(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the span contains the given span.
        /// </summary>
        public readonly bool Contains(USpan<T> span)
        {
            if (span.Length > Length)
            {
                return false;
            }

            uint maxLength = Length - span.Length;
            for (uint i = 0; i <= maxLength; i++)
            {
                if (Slice(i, span.Length).SequenceEqual(span))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves this span as a managed array.
        /// </summary>
        public readonly T[] ToArray()
        {
            T[] array = new T[Length];
            for (uint i = 0; i < Length; i++)
            {
#if NET
                array[i] = Unsafe.Add(ref pointer, i);
#else
                array[i] = Unsafe.Add(ref pointer, (int)i);
#endif
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
#if NET
                if (!comparer.Equals(Unsafe.Add(ref pointer, i), Unsafe.Add(ref other.pointer, i)))
                {
                    return false;
                }
#else
                if (!comparer.Equals(Unsafe.Add(ref pointer, (int)i), Unsafe.Add(ref other.pointer, (int)i)))
                {
                    return false;
                }
#endif
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
        public unsafe readonly void Clear()
        {
            Unsafe.InitBlockUnaligned((void*)Address, 0, Length * TypeInfo<T>.size);
        }

        /// <summary>
        /// Fills the span with the given value.
        /// </summary>
        public readonly void Fill(T value)
        {
            AsSystemSpan().Fill(value);
        }

        /// <summary>
        /// Copies this entire span to the given span.
        /// </summary>
        /// <returns>Amount of values copied.</returns>
        public unsafe readonly uint CopyTo(USpan<T> otherSpan)
        {
            ThrowIfDestinationTooSmall(otherSpan.Length);
            Unsafe.CopyBlockUnaligned((void*)otherSpan.Address, (void*)Address, Length * TypeInfo<T>.size);
            return Length;
        }

        /// <inheritdoc/>
        public static unsafe implicit operator USpan<T>(Span<T> span)
        {
            if (span.Length > 0)
            {
                void* pointer = Unsafe.AsPointer(ref span[0]);
                return new(pointer, (uint)span.Length);
            }
            else
            {
                return default;
            }
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

        /// <summary>
        /// Enumerator for <see cref="USpan{T}"/>.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly USpan<T> span;
            private int index;

            /// <summary>
            /// Current element in the span.
            /// </summary>
            public readonly ref T Current => ref span[(uint)index];

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
