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

        public ref T this[uint index]
        {
            get
            {
                ThrowIfAccessingOutOfRange(index);

                return ref Unsafe.Add(ref pointer, index);
            }
        }

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
        /// Creates a reference to memory at this address with the given element length.
        /// </summary>
        public unsafe USpan(void* pointer, uint length)
        {
            this.pointer = ref Unsafe.AsRef<T>(pointer);
            this.length = length;
        }

        public USpan(ref T pointer, uint length)
        {
            this.pointer = ref pointer;
            this.length = length;
        }

        /// <summary>
        /// Creates a reference to memory at this address with the given element length.
        /// </summary>
        public unsafe USpan(nint address, uint length)
        {
            this.pointer = ref Unsafe.AsRef<T>((void*)address);
            this.length = length;
        }

        public USpan(Span<T> span)
        {
            length = (uint)span.Length;
            if (length > 0)
            {
                pointer = ref span[0];
            }
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe readonly Span<T> AsSystemSpan()
        {
            return new Span<T>((void*)Address, (int)length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe readonly Span<V> AsSystemSpan<V>() where V : unmanaged
        {
            ThrowIfTypeSizeMismatches<V>();
            return new Span<V>((void*)Address, (int)length);
        }

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

        public readonly USpan<T> Slice(uint start, uint length)
        {
            ThrowIfAccessingPastRange(start + length);

            return new USpan<T>(ref Unsafe.Add(ref pointer, start), length);
        }

        public readonly USpan<T> Slice(uint start)
        {
            ThrowIfAccessingPastRange(start);

            return new USpan<T>(ref Unsafe.Add(ref pointer, start), Length - start);
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
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, i));
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
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, i));
                    if (e.Equals(value))
                    {
                        return i;
                    }
                }

                return uint.MaxValue;
            }
        }

        public readonly bool TryIndexOfSlow(T value, out uint index)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (uint i = 0; i < Length; i++)
            {
                ref T e = ref Unsafe.Add(ref pointer, i);
                if (comparer.Equals(e, value))
                {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        public readonly bool TryIndexOf<V>(V value, out uint index) where V : unmanaged, IEquatable<V>
        {
            ThrowIfTypeSizeMismatches<V>();
            unchecked
            {
                for (uint i = 0; i < length; i++)
                {
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, i));
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

        public readonly bool TryLastIndexOf<V>(V value, out uint index) where V : unmanaged, IEquatable<V>
        {
            ThrowIfTypeSizeMismatches<V>();
            unchecked
            {
                for (uint i = length - 1; i != uint.MaxValue; i--)
                {
                    ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, i));
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

        public readonly bool ContainsSlow(T value)
        {
            return TryIndexOfSlow(value, out _);
        }

        public readonly bool Contains<V>(V value) where V : unmanaged, IEquatable<V>
        {
            ThrowIfTypeSizeMismatches<V>();
            for (uint i = 0; i < Length; i++)
            {
                ref V e = ref Unsafe.As<T, V>(ref Unsafe.Add(ref pointer, i));
                if (e.Equals(value))
                {
                    return true;
                }
            }

            return false;
        }

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

        public readonly T[] ToArray()
        {
            T[] array = new T[Length];
            for (uint i = 0; i < Length; i++)
            {
                array[i] = Unsafe.Add(ref pointer, i);
            }

            return array;
        }

        public readonly bool SequenceEqual(USpan<T> other)
        {
            if (Length != other.Length)
            {
                return false;
            }

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (uint i = 0; i < Length; i++)
            {
                if (!comparer.Equals(Unsafe.Add(ref pointer, i), Unsafe.Add(ref other.pointer, i)))
                {
                    return false;
                }
            }

            return true;
        }

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

        public unsafe readonly uint CopyTo(USpan<T> otherSpan)
        {
            ThrowIfDestinationTooSmall(otherSpan.Length);
            Unsafe.CopyBlockUnaligned((void*)otherSpan.Address, (void*)Address, Length * TypeInfo<T>.size);
            return Length;
        }

        public static implicit operator USpan<T>(Span<T> span)
        {
            return new(span);
        }

        public static implicit operator USpan<T>(ReadOnlySpan<T> span)
        {
            return new(span);
        }

        public static implicit operator USpan<T>(T[] array)
        {
            return new(array);
        }

        public ref struct Enumerator
        {
            private readonly USpan<T> span;
            private int index;

            public readonly ref T Current => ref span[(uint)index];

            internal Enumerator(USpan<T> span)
            {
                this.span = span;
                index = -1;
            }

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

    public static class USpanBuilder
    {
        public static USpan<T> Create<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            return values;
        }
    }
}
