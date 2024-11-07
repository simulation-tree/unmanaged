using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmanaged
{
    public static unsafe class Allocations
    {
        private static uint count;

        /// <summary>
        /// Amount of allocations made that have not been freed.
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

        public static void* Allocate(uint size)
        {
            void* pointer = NativeMemory.Alloc(size);
            Tracker.Track(pointer);
            count++;
            return pointer;
        }

        public static void* AllocateAligned(uint size, uint alignment)
        {
            void* pointer = NativeMemory.AlignedAlloc(size, alignment);
            Tracker.TrackAligned(pointer);
            count++;
            return pointer;
        }

        /// <summary>
        /// Allocates unmanaged memory for a single instance of the given type.
        /// <para>
        /// Requires to be disposed with <see cref="Free(ref void*)"/>
        /// </para>
        /// </summary>
        public static T* Allocate<T>() where T : unmanaged
        {
            void* pointer = NativeMemory.Alloc(TypeInfo<T>.size);
            Tracker.Track(pointer);
            count++;
            return (T*)pointer;
        }

        public static void Free(ref void* pointer)
        {
            NativeMemory.Free(pointer);
            Tracker.Untrack(pointer);
            count--;
            pointer = null;
        }

        public static void FreeAligned(ref void* pointer)
        {
            NativeMemory.AlignedFree(pointer);
            Tracker.UntrackAligned(pointer);
            count--;
            pointer = null;
        }

        public static void Free<T>(ref T* pointer) where T : unmanaged
        {
            NativeMemory.Free(pointer);
            Tracker.Untrack(pointer);
            count--;
            pointer = null;
        }

        public static void FreeAligned<T>(ref T* pointer) where T : unmanaged
        {
            NativeMemory.AlignedFree(pointer);
            Tracker.UntrackAligned(pointer);
            count--;
            pointer = null;
        }

        public static void* Reallocate(void* pointer, uint newSize)
        {
            Tracker.Untrack(pointer);
            void* newPointer = NativeMemory.Realloc(pointer, newSize);
            Tracker.Track(newPointer);
            return newPointer;
        }

        public static void* ReallocateAligned(void* pointer, uint newSize, uint alignment)
        {
            Tracker.UntrackAligned(pointer);
            void* newPointer = NativeMemory.AlignedRealloc(pointer, newSize, alignment);
            Tracker.TrackAligned(newPointer);
            return newPointer;
        }

        public static void ThrowIfNull(void* pointer)
        {
            if (pointer is null)
            {
                nint address = (nint)pointer;
                Tracker.ThrowIfNull(address);
                throw new NullReferenceException($"Unknown pointer at {address}");
            }
        }

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

        private static class Tracker
        {
            private static readonly ConcurrentStack<nint> addresses = new();
            private static readonly ConcurrentStack<nint> alignedAddresses = new();
            private static readonly Dictionary<nint, StackTrace> allocations = new();
            private static readonly Dictionary<nint, StackTrace> disposals = new();

            [Conditional("DEBUG")]
            public static void AppendAllocations(List<char> exceptionBuilder)
            {
                nint[] leakedAddresses = addresses.ToArray();
                foreach (nint address in leakedAddresses)
                {
                    Append("    ");
                    if (allocations.TryGetValue(address, out StackTrace? allocation))
                    {
                        Append(address.ToString());
                        Append(" from ");
                        AppendLine(allocation.ToString());
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
                    if (allocations.TryGetValue(address, out StackTrace? allocation))
                    {
                        Append(address.ToString());
                        Append(" from ");
                        AppendLine(allocation.ToString());
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

            [Conditional("DEBUG")]
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

            [Conditional("DEBUG")]
            public static void Track(void* pointer)
            {
                nint address = (nint)pointer;
                addresses.Push(address);
                allocations[address] = new StackTrace(2, true);
                disposals.Remove(address);
            }

            [Conditional("DEBUG")]
            public static void TrackAligned(void* pointer)
            {
                nint address = (nint)pointer;
                alignedAddresses.Push(address);
                allocations[address] = new StackTrace(2, true);
                disposals.Remove(address);
            }

            [Conditional("DEBUG")]
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
                    addresses.PushRange(currentAddresses);
                }

                disposals[address] = new StackTrace(2, true);
            }

            [Conditional("DEBUG")]
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

            [Conditional("DEBUG")]
            public static void ThrowIfNull(nint address)
            {
                if (allocations.TryGetValue(address, out StackTrace? stackTrace))
                {
                    if (disposals.TryGetValue(address, out StackTrace? disposedStackTrace))
                    {
                        throw new NullReferenceException($"Invalid pointer at `{address}` allocated then disposed at:\n{stackTrace}\n{disposedStackTrace}");
                    }
                    else
                    {
                        throw new NullReferenceException($"Unrecognized pointer at `{address}` that isn't known to be disposed, but supposedly allocated at:\n{stackTrace}");
                    }
                }
                else
                {
                    if (disposals.TryGetValue(address, out stackTrace))
                    {
                        throw new NullReferenceException($"Unrecognized pointer at `{address}` that isn't known to be allocated, but has been disposed at:\n{stackTrace}");
                    }
                    else
                    {
                        throw new NullReferenceException($"Unknown pointer at `{address}` that hasn't ever been allocated or disposed");
                    }
                }
            }
        }
    }
}