using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Unmanaged
{
    public readonly unsafe ref struct USpan<T> where T : unmanaged
    {
        public readonly void* pointer;
        public readonly uint length;

        public ref T this[uint index]
        {
            get
            {
                ThrowIfAccessingOutOfRange(index, length);
                return ref Unsafe.AsRef<T>((void*)((nint)pointer + index * sizeof(T)));
            }
        }

        public USpan<T> this[Range range]
        {
            get
            {
                uint start = (uint)range.Start.Value;
                if (range.Start.IsFromEnd)
                {
                    start += this.length + 1;
                }

                uint end = (uint)range.End.Value;
                if (range.End.IsFromEnd)
                {
                    end += this.length + 1;
                }

                uint length = end - start;
                return Slice(start, length);
            }
        }

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
            ThrowIfLessThanZero(span.Length);
            fixed (T* ptr = span)
            {
                pointer = ptr;
                length = (uint)span.Length;
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfAccessingOutOfRange(uint index, uint length)
        {
            if (index >= length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be less than the length of the span.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfLessThanZero(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than or equal to zero.");
            }
        }

        public readonly USpan<T> Slice(uint start, uint length)
        {
            ThrowIfAccessingOutOfRange(start, this.length);
            ThrowIfAccessingOutOfRange(start + length, this.length);
            return new USpan<T>((void*)((nint)pointer + start * sizeof(T)), length);
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

        public static implicit operator USpan<T>(Span<T> span)
        {
            return new(span);
        }
    }
}
