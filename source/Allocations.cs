#if DEBUG
#define TRACK
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    /// <summary>
    /// Contains functions for allocating, freeing and tracking unmanaged memory.
    /// </summary>
    public static unsafe class Allocations
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
                Tracker.FreeAll();
            };
        }

        /// <summary>
        /// Throws an <see cref="Exception"/> if there are any memory leaks.
        /// </summary>
        public static void ThrowIfAny()
        {
            if (Count > 0)
            {
                List<char> exceptionBuilder = new();
                Append("Leaked ");
                Append(Count.ToString());
                AppendLine(" allocation(s)");

                Tracker.AppendAllocations(exceptionBuilder);

                void Append(string str)
                {
                    for (uint i = 0; i < str.Length; i++)
                    {
                        exceptionBuilder.Add(str[(int)i]);
                    }
                }

                void AppendLine(string str)
                {
                    Append(str);
                    exceptionBuilder.Add('\n');
                }

                string exceptionMessage = new(exceptionBuilder.ToArray());
                throw new Exception(exceptionMessage);
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

        internal static class Tracker
        {
            private static readonly ConcurrentStack<nint> addresses = new();
            private static readonly ConcurrentStack<nint> alignedAddresses = new();
            private static readonly Dictionary<nint, (StackTrace stack, uint size)> allocations = new();
            private static readonly Dictionary<nint, StackTrace> disposals = new();

            [Conditional("TRACK")]
            public static void AppendAllocations(List<char> exceptionBuilder)
            {
                nint[] leakedAddresses = addresses.ToArray();
                foreach (nint address in leakedAddresses)
                {
                    Append("    ");
                    if (allocations.TryGetValue(address, out (StackTrace stack, uint size) info))
                    {
                        Append(address.ToString());
                        Append(" (");
                        Append(info.size.ToString());
                        Append(")");
                        Append(" from ");
                        AppendLine(info.stack.ToString());
                    }
                    else
                    {
                        AppendLine(address.ToString());
                    }
                }

                nint[] leakedAlignedAddresses = alignedAddresses.ToArray();
                foreach (nint address in leakedAlignedAddresses)
                {
                    Append("    ");
                    if (allocations.TryGetValue(address, out (StackTrace stack, uint size) info))
                    {
                        Append(address.ToString());
                        Append(" (");
                        Append(info.size.ToString());
                        Append(")");
                        Append(" from ");
                        AppendLine(info.stack.ToString());
                    }
                    else
                    {
                        AppendLine(address.ToString());
                    }
                }

                void Append(string str)
                {
                    for (uint i = 0; i < str.Length; i++)
                    {
                        exceptionBuilder.Add(str[(int)i]);
                    }
                }

                void AppendLine(string str)
                {
                    Append(str);
                    exceptionBuilder.Add('\n');
                }
            }

            [Conditional("TRACK")]
            public static void FreeAll()
            {
                nint[] leakedAddresses = addresses.ToArray();
                foreach (nint address in leakedAddresses)
                {
                    NativeMemory.Free((void*)address);
                }

                nint[] leakedAlignedAddresses = alignedAddresses.ToArray();
                foreach (nint address in leakedAlignedAddresses)
                {
                    NativeMemory.AlignedFree((void*)address);
                }
            }

            [Conditional("TRACK")]
            public static void Track(void* pointer, uint size)
            {
                nint address = (nint)pointer;
                addresses.Push(address);
                allocations[address] = (new StackTrace(2, true), size);
                disposals.Remove(address);
            }

            [Conditional("TRACK")]
            public static void TrackAligned(void* pointer, uint size)
            {
                nint address = (nint)pointer;
                alignedAddresses.Push(address);
                allocations[address] = (new StackTrace(2, true), size);
                disposals.Remove(address);
            }

            [Conditional("TRACK")]
            public static void Untrack(void* pointer)
            {
                nint address = (nint)pointer;
                nint[] currentAddresses = addresses.ToArray();
                int index = Array.IndexOf(currentAddresses, address);
                if (index != -1)
                {
                    currentAddresses[index] = currentAddresses[^1];
                    Array.Resize(ref currentAddresses, currentAddresses.Length - 1);
                    addresses.Clear();
                    if (currentAddresses.Length > 0)
                    {
                        addresses.PushRange(currentAddresses);
                    }
                }

                disposals[address] = new StackTrace(2, true);
            }

            [Conditional("TRACK")]
            public static void UntrackAligned(void* pointer)
            {
                nint address = (nint)pointer;
                nint[] currentAddresses = alignedAddresses.ToArray();
                int index = Array.IndexOf(currentAddresses, address);
                if (index != -1)
                {
                    currentAddresses[index] = currentAddresses[^1];
                    Array.Resize(ref currentAddresses, currentAddresses.Length - 1);
                    alignedAddresses.Clear();
                    alignedAddresses.PushRange(currentAddresses);
                }

                disposals[address] = new StackTrace(2, true);
            }

            [Conditional("TRACK")]
            public static void ThrowIfNull(nint address)
            {
                if (allocations.TryGetValue(address, out (StackTrace stack, uint size) allocInfo))
                {
                    if (disposals.TryGetValue(address, out StackTrace? disposedStackTrace))
                    {
                        throw new NullReferenceException($"Memory at address `{address}` was allocated then disposed at:\n{allocInfo.stack}\n{disposedStackTrace}");
                    }
                    else
                    {
                        //allocation address is good, but no disposal yet, nothing wrong here
                    }
                }
                else
                {
                    if (!disposals.ContainsKey(address))
                    {
                        throw new InvalidOperationException($"Memory at address `{address}` isn't known to be allocated or disposed");
                    }
                    else
                    {
                        //allocation not present, but it has been disposed, this case shouldnt be possible
                    }
                }
            }

            public static bool TryGetSize(nint address, out uint size)
            {
                if (allocations.TryGetValue(address, out (StackTrace stack, uint size) info))
                {
                    size = info.size;
                    return true;
                }
                else
                {
                    size = 0;
                    return false;
                }
            }
        }
    }
}