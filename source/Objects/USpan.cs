using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Unmanaged
{
    public readonly unsafe ref struct USpan<T> where T : unmanaged
    {
        private readonly static uint Size = (uint)sizeof(T);

        public readonly void* pointer;
        public readonly uint length;

        public ref T this[uint index]
        {
            get
            {
                ThrowIfAccessingOutOfRange(index);
                return ref Unsafe.AsRef<T>((void*)((nint)pointer + index * Size));
            }
        }

        public readonly nint Address => (nint)pointer;

        public USpan(void* pointer, uint length)
        {
            this.pointer = pointer;
            this.length = length;
        }

        public USpan(nint address, uint length)
        {
            pointer = (void*)address;
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

        [Conditional("DEBUG")]
        private readonly void ThrowIfAccessingOutOfRange(uint index)
        {
            if (index >= length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be less than the length of the span");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDestinationTooSmall(uint length)
        {
            if (length < this.length)
            {
                throw new ArgumentOutOfRangeException("Destination span is too small", nameof(length));
            }
        }

        public override string ToString()
        {
            USpan<char> buffer = stackalloc char[64];
            uint length = ToString(buffer);
            return new string((char*)buffer.pointer, 0, (int)length);
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
            
            Span<char> signedBuffer = stackalloc char[16];
            this.length.TryFormat(signedBuffer, out int signedLength);
            for (int i = 0; i < signedLength; i++)
            {
                buffer[length++] = signedBuffer[i];
            }

            buffer[length++] = ']';
            return length;
        }

        public readonly int GetHashCodeOfContents()
        {
            unchecked
            {
                uint seed = 0x9E377;
                uint hash = seed + length;
                for (uint i = 0; i < length; i++)
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
            return HashCode.Combine((nint)pointer, length);
        }

        public readonly USpan<T> Slice(uint start, uint length)
        {
            ThrowIfAccessingOutOfRange(start);
            ThrowIfAccessingOutOfRange(start + length);
            return new USpan<T>((void*)((nint)pointer + start * Size), length);
        }

        public readonly USpan<T> Slice(uint start)
        {
            ThrowIfAccessingOutOfRange(start);
            return new USpan<T>((void*)((nint)pointer + start * Size), length - start);
        }

        public readonly uint IndexOf(T value)
        {
            for (uint i = 0; i < length; i++)
            {
                if (this[i].Equals(value))
                {
                    return i;
                }
            }

            throw new ArgumentException("Value not found in span", nameof(value));
        }

        public readonly uint IndexOfLast(T value)
        {
            for (uint i = length - 1; i != uint.MaxValue; i--)
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
            for (uint i = 0; i < length; i++)
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

        public readonly bool TryIndexOfLast(T value, out uint index)
        {
            for (uint i = length - 1; i != uint.MaxValue; i--)
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

        public readonly T[] ToArray()
        {
            T[] array = new T[length];
            for (uint i = 0; i < length; i++)
            {
                array[i] = this[i];
            }

            return array;
        }

        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        public readonly void Clear()
        {
            Unsafe.InitBlockUnaligned(pointer, 0, length * Size);
        }

        public readonly void Fill(T value)
        {
            for (uint i = 0; i < length; i++)
            {
                this[i] = value;
            }
        }

        public readonly void CopyTo(USpan<T> otherSpan)
        {
            ThrowIfDestinationTooSmall(otherSpan.length);
            Unsafe.CopyBlockUnaligned(otherSpan.pointer, pointer, length * Size);
        }

        public static implicit operator USpan<T>(Span<T> span)
        {
            return new(span);
        }

        public ref struct Enumerator
        {
            private readonly USpan<T> span;
            private int index;

            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref span[(uint)index];
            }

            internal Enumerator(USpan<T> span)
            {
                this.span = span;
                index = -1;
            }

            public bool MoveNext()
            {
                int index = this.index + 1;
                if (index < span.length)
                {
                    this.index = index;
                    return true;
                }

                return false;
            }
        }
    }
}
