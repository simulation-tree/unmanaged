using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Unmanaged
{
    /// <summary>
    /// Represents a continous region of unmanaged memory
    /// containing <typeparamref name="T"/> elements.
    /// </summary>
#if NET
    [CollectionBuilder(typeof(USpanBuilder), "Create")]
#endif
    public readonly unsafe ref struct USpan<T> where T : unmanaged
    {
        public readonly static uint ElementSize = (uint)sizeof(T);

        public readonly T* pointer;

        private readonly uint length;

        public ref T this[uint index]
        {
            get
            {
                ThrowIfAccessingOutOfRange(index);
                return ref Unsafe.AsRef<T>((void*)((nint)pointer + index * ElementSize));
            }
        }

        public readonly nint Address => (nint)pointer;

        /// <summary>
        /// Amount of <typeparamref name="T"/> elements in this span.
        /// </summary>
        public readonly uint Length => length;

        /// <summary>
        /// Creates a reference to memory at this address with the given element length.
        /// </summary>
        public USpan(void* pointer, uint length)
        {
            this.pointer = (T*)pointer;
            this.length = length;
        }

        /// <summary>
        /// Creates a reference to memory at this address with the given element length.
        /// </summary>
        public USpan(nint address, uint length)
        {
            pointer = (T*)address;
            this.length = length;
        }

        public USpan(Span<T> span)
        {
            fixed (T* ptr = span)
            {
                pointer = ptr;
                length = (uint)span.Length;
            }
        }

        public USpan(ReadOnlySpan<T> span)
        {
            fixed (T* ptr = span)
            {
                pointer = ptr;
                length = (uint)span.Length;
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfAccessingOutOfRange(uint index)
        {
            if (index == default && Length == default)
            {
                return;
            }

            if (index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be less than the length of the span");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDestinationTooSmall(uint length)
        {
            if (length < this.Length)
            {
                throw new ArgumentOutOfRangeException("Destination span is too small", nameof(length));
            }
        }

        public override string ToString()
        {
            if (typeof(T) == typeof(char)) //special case
            {
                return new string((char*)pointer, 0, (int)Length);
            }
            else
            {
                USpan<char> buffer = stackalloc char[32];
                uint length = ToString(buffer);
                return new string(buffer.pointer, 0, (int)length);
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

            length += this.Length.ToString(buffer.Slice(length));

            buffer[length++] = ']';
            return length;
        }

        public readonly int GetHashCodeOfContents()
        {
            unchecked
            {
                uint seed = 0x9E377;
                uint hash = seed + Length;
                for (uint i = 0; i < Length; i++)
                {
                    hash ^= (uint)this[i].GetHashCode() + seed + i;
                    hash = (hash << 13) | (hash >> 19);
                    hash *= 2654435761;
                }

                return (int)hash;
            }
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine((nint)pointer, Length);
        }

        public readonly USpan<T> Slice(uint start, uint length)
        {
            ThrowIfAccessingOutOfRange(start);
            return new USpan<T>(pointer + start, length);
        }

        public readonly USpan<T> Slice(uint start)
        {
            ThrowIfAccessingOutOfRange(start);
            return new USpan<T>(pointer + start, Length - start);
        }

        public readonly uint IndexOf(T value)
        {
            for (uint i = 0; i < Length; i++)
            {
                if (this[i].Equals(value))
                {
                    return i;
                }
            }

            throw new ArgumentException("Value not found in span", nameof(value));
        }

        public readonly uint LastIndexOf(T value)
        {
            for (uint i = Length - 1; i != uint.MaxValue; i--)
            {
                if (this[i].Equals(value))
                {
                    return i;
                }
            }

            throw new ArgumentException("Value not found in span", nameof(value));
        }

        public readonly bool TryIndexOf(T value, out uint index)
        {
            for (uint i = 0; i < Length; i++)
            {
                if (this[i].Equals(value))
                {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        public readonly bool TryLastIndexOf(T value, out uint index)
        {
            for (uint i = Length - 1; i != uint.MaxValue; i--)
            {
                if (this[i].Equals(value))
                {
                    index = i;
                    return true;
                }
            }

            index = 0;
            return false;
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

        public readonly bool Contains(T value)
        {
            for (uint i = 0; i < Length; i++)
            {
                if (pointer[i].Equals(value))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool Contains(USpan<T> span)
        {
            for (uint i = 0; i < Length; i++)
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
                array[i] = pointer[i];
            }

            return array;
        }

        public readonly bool SequenceEqual(USpan<T> other)
        {
            if (Length != other.Length)
            {
                return false;
            }

            for (uint i = 0; i < Length; i++)
            {
                if (!pointer[i].Equals(other.pointer[i]))
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
        public readonly void Clear()
        {
            Unsafe.InitBlockUnaligned(pointer, 0, Length * ElementSize);
        }

        /// <summary>
        /// Fills the span with the given value.
        /// </summary>
        public readonly void Fill(T value)
        {
            for (uint i = 0; i < Length; i++)
            {
                pointer[i] = value;
            }
        }

        public readonly void CopyTo(USpan<T> otherSpan)
        {
            //todo: efficiency: to remove this branch, spans cant be allowed to contain default values at all...
            //because stackalloc of 0 will give a blank pointer (undefined)
            if (Length == 0)
            {
                return;
            }

            ThrowIfDestinationTooSmall(otherSpan.Length);
            Unsafe.CopyBlockUnaligned(otherSpan.pointer, pointer, Length * ElementSize);
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
