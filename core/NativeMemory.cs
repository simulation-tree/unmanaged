#if !NET
using System.Buffers;

namespace System.Runtime.CompilerServices
{
    public class SkipLocalsInit : Attribute
    {

    }
}

namespace System.Runtime.InteropServices
{
    public unsafe static class NativeMemory
    {
        public static void* Alloc(uint size)
        {
            return Marshal.AllocHGlobal((int)size).ToPointer();
        }

        public static void* AllocZeroed(uint size)
        {
            IntPtr address = Marshal.AllocHGlobal((int)size);
            Span<byte> span = new(address.ToPointer(), (int)size);
            span.Clear();
            return address.ToPointer();
        }

        public static void Free(void* ptr)
        {
            Marshal.FreeHGlobal(new IntPtr(ptr));
        }

        public static void Clear(void* ptr, uint size)
        {
            Span<byte> span = new((byte*)ptr, (int)size);
            span.Clear();
        }

        public static void Fill(void* ptr, uint length, byte value)
        {
            Span<byte> span = new((byte*)ptr, (int)length);
            span.Fill(value);
        }

        public static void* Realloc(void* ptr, uint newSize)
        {
            return Marshal.ReAllocHGlobal(new IntPtr(ptr), new IntPtr(newSize)).ToPointer();
        }

        public static void* AlignedRealloc(void* ptr, uint newSize, uint alignment)
        {
            return Marshal.ReAllocHGlobal(new IntPtr(ptr), new IntPtr(newSize)).ToPointer();
        }
    }
}

namespace System
{
    public static partial class MemoryExtensions
    {
        public static unsafe bool Contains<T>(this Span<T> span, T value) where T : IEquatable<T>
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].Equals(value))
                {
                    return true;
                }
            }

            return false;
        }

        public static unsafe bool Contains<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].Equals(value))
                {
                    return true;
                }
            }

            return false;
        }

        public static unsafe void Sort<T>(this Span<T> span) where T : IComparable<T>
        {
            T[] buffer = ArrayPool<T>.Shared.Rent(span.Length);
            span.CopyTo(buffer);
            Array.Sort(buffer);
            buffer.AsSpan(0, span.Length).CopyTo(span);
            ArrayPool<T>.Shared.Return(buffer);
        }
    }
}
#endif