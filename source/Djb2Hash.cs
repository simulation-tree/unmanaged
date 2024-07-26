using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    public static class Djb2Hash
    {
        public static int Get(string? str)
        {
            return Get((str ?? string.Empty).AsSpan());
        }

        public static int Get<T>(ReadOnlySpan<T> span) where T : notnull
        {
            //djb2 implementation from CommunityToolkit.HighPerformance/SpanHelper.Hash.cs
            //todo: this could be xor hash instead, and shorter?????
            ref T first = ref MemoryMarshal.GetReference(span);
            nint length = (nint)(uint)span.Length;
            int hash = 5381;
            nint offset = 0;
            while (length >= 8)
            {
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 0).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 1).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 2).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 3).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 4).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 5).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 6).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 7).GetHashCode());

                length -= 8;
                offset += 8;
            }

            if (length >= 4)
            {
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 0).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 1).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 2).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 3).GetHashCode());

                length -= 4;
                offset += 4;
            }

            while (length > 0)
            {
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset).GetHashCode());

                length -= 1;
                offset += 1;
            }

            return hash;
        }

        public static int Get<T>(Span<T> span) where T : notnull
        {
            ref T first = ref MemoryMarshal.GetReference(span);
            nint length = (nint)(uint)span.Length;
            int hash = 5381;
            nint offset = 0;
            while (length >= 8)
            {
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 0).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 1).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 2).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 3).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 4).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 5).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 6).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 7).GetHashCode());

                length -= 8;
                offset += 8;
            }

            if (length >= 4)
            {
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 0).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 1).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 2).GetHashCode());
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset + 3).GetHashCode());

                length -= 4;
                offset += 4;
            }

            while (length > 0)
            {
                hash = unchecked(((hash << 5) + hash) ^ Unsafe.Add(ref first, offset).GetHashCode());

                length -= 1;
                offset += 1;
            }

            return hash;
        }
    }
}
