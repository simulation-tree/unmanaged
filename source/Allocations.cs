#if DEBUG
#define TRACK
#endif

using System;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// Contains functions for allocating, freeing and tracking unmanaged memory.
    /// </summary>
    public static unsafe partial class Allocations
    {
        private static uint count;

        /// <summary>
        /// Amount of allocations made that have not been freed yet.
        /// </summary>
        public static uint Count => count;

        static Allocations()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                ThrowIfAny();
            };
        }

        /// <summary>
        /// Throws an <see cref="Exception"/> if there are any allocations present.
        /// <para>
        /// The <paramref name="free"/> parameter is only functional in debug builds, never in release.
        /// </para>
        /// </summary>
        public static void ThrowIfAny(bool free = false)
        {
            if (Count > 0)
            {
                System.Collections.Generic.List<char> stringBuilder = new(1024);
                Append("Allocations present: ");
                AppendLine(Count.ToString());

                Tracker.AppendAllocations(stringBuilder, free);

                void Append(string str)
                {
                    for (int i = 0; i < str.Length; i++)
                    {
                        stringBuilder.Add(str[i]);
                    }
                }

                void AppendLine(string str)
                {
                    Append(str);
                    stringBuilder.Add('\n');
                }

                throw new Exception(new(stringBuilder.ToArray()));
            }
        }

        /// <summary>
        /// Allocates unmanaged memory of the given size.
        /// </summary>
        public static void* Allocate(uint size)
        {
            void* pointer = NativeMemory.Alloc(size);
            Tracker.Track(pointer, size);
            count++;
            return pointer;
        }

        /// <summary>
        /// Allocates unmanaged memory of the given size with the given alignment.
        /// </summary>
        public static void* AllocateAligned(uint size, uint alignment)
        {
            void* pointer = NativeMemory.AlignedAlloc(size, alignment);
            Tracker.TrackAligned(pointer, size);
            count++;
            return pointer;
        }

        /// <summary>
        /// Allocates unmanaged memory for a single instance of the given type.
        /// </summary>
        public static T* Allocate<T>() where T : unmanaged
        {
            uint size = TypeInfo<T>.size;
            void* pointer = NativeMemory.Alloc(size);
            Tracker.Track(pointer, size);
            count++;
            return (T*)pointer;
        }

        /// <summary>
        /// Free the memory at the given pointer.
        /// </summary>
        public static void Free(ref void* pointer)
        {
            NativeMemory.Free(pointer);
            Tracker.Untrack(pointer);
            count--;
            pointer = null;
        }

        /// <summary>
        /// Frees the aligned memory at the given pointer.
        /// </summary>
        public static void FreeAligned(ref void* pointer)
        {
            NativeMemory.AlignedFree(pointer);
            Tracker.UntrackAligned(pointer);
            count--;
            pointer = null;
        }

        /// <summary>
        /// Frees the memory at the given pointer.
        /// </summary>
        public static void Free<T>(ref T* pointer) where T : unmanaged
        {
            NativeMemory.Free(pointer);
            Tracker.Untrack(pointer);
            count--;
            pointer = null;
        }

        /// <summary>
        /// Frees the aligned memory at the given pointer.
        /// </summary>
        public static void FreeAligned<T>(ref T* pointer) where T : unmanaged
        {
            NativeMemory.AlignedFree(pointer);
            Tracker.UntrackAligned(pointer);
            count--;
            pointer = null;
        }

        /// <summary>
        /// Reallocates the memory at the given pointer to the new size.
        /// </summary>
        public static void* Reallocate(void* pointer, uint newSize)
        {
            Tracker.Untrack(pointer);
            void* newPointer = NativeMemory.Realloc(pointer, newSize);
            Tracker.Track(newPointer, newSize);
            return newPointer;
        }

        /// <summary>
        /// Reallocates the aligned memory at the given pointer to the new size.
        /// </summary>
        public static void* ReallocateAligned(void* pointer, uint newSize, uint alignment)
        {
            Tracker.UntrackAligned(pointer);
            void* newPointer = NativeMemory.AlignedRealloc(pointer, newSize, alignment);
            Tracker.TrackAligned(newPointer, newSize);
            return newPointer;
        }

        /// <summary>
        /// Throws an <see cref="NullReferenceException"/> if the pointer is null.
        /// </summary>
        public static void ThrowIfNull(void* pointer)
        {
            nint address = (nint)pointer;
            if (pointer is null)
            {
                throw new NullReferenceException($"Unknown pointer at {address}");
            }

            Tracker.ThrowIfNull(address);
        }

        /// <summary>
        /// Retrieves an alignment for the given type.
        /// </summary>
        public static uint GetAlignment<T>() where T : unmanaged
        {
            return GetAlignment(TypeInfo<T>.size);
        }

        /// <summary>
        /// Returns an alignment that is able to contain the size.
        /// </summary>
        public static uint GetAlignment(uint stride)
        {
            if ((stride & 7) == 0)
            {
                return 8u;
            }

            if ((stride & 3) == 0)
            {
                return 4u;
            }

            return (stride & 1) == 0 ? 2u : 1u;
        }

        /// <summary>
        /// Retrieves the upper bound of the given stride and alignment.
        /// </summary>
        public static uint CeilAlignment(uint stride, uint alignment)
        {
            return alignment switch
            {
                1 => stride,
                2 => ((stride + 1) >> 1) * 2,
                4 => ((stride + 3) >> 2) * 4,
                8 => ((stride + 7) >> 3) * 8,
                _ => throw new ArgumentException($"Invalid alignment {alignment}"),
            };
        }

        /// <summary>
        /// Retrieves the next power of 2 for the given value.
        /// </summary>
        public static uint GetNextPowerOf2(uint value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return ++value;
        }

        /// <summary>
        /// Retrieves the index of the power of 2 for the given value.
        /// <para>
        /// If <paramref name="value"/> isn't a power of 2 the returning value
        /// isn't valid.
        /// </para>
        /// </summary>
        public static uint GetIndexOfPowerOf2(uint value)
        {
            uint index = 0;
            while ((value & 1) == 0)
            {
                value >>= 1;
                index++;
            }

            return index;
        }
    }
}