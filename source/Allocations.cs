#if DEBUG
#define TRACK_ALLOCATIONS
#else
#define IGNORE_STACKTRACES
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Unmanaged
{
    public static unsafe class Allocations
    {
        private static readonly HashSet<nint> addresses = [];
        private static readonly Dictionary<nint, StackTrace> allocations = [];
        private static readonly Dictionary<nint, StackTrace> disposals = [];

        public static uint Count => (uint)addresses.Count;

        static Allocations()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                ThrowIfAnyAllocation();
            };
        }

        /// <summary>
        /// Throws an <see cref="Exception"/> if there are any memory leaks.
        /// </summary>
        public static void ThrowIfAnyAllocation()
        {
#if TRACK_ALLOCATIONS
            if (addresses.Count > 0)
            {
                StringBuilder exceptionBuilder = new();
                foreach (nint address in addresses)
                {
                    if (allocations.TryGetValue(address, out StackTrace? allocation))
                    {
                        exceptionBuilder.AppendLine($"Leaked memory at {address:X} allocated from:\n{allocation}");
                    }
                    else
                    {
                        exceptionBuilder.AppendLine($"Leaked memory at {address:X}.");
                    }
                }

                string exceptionMessage = exceptionBuilder.ToString();
                throw new Exception(exceptionMessage);
            }
#endif
        }

        public static void* Allocate(uint size)
        {
            //void* pointer = NativeMemory.Alloc(size);
            void* pointer = NativeMemory.AlignedAlloc(size, GetAlignment(size));
            nint address = (nint)pointer;
#if TRACK_ALLOCATIONS
            addresses.Add(address);
#if !IGNORE_STACKTRACES
            allocations[address] = new StackTrace(1, true);
            disposals[address] = null!;
#endif
#endif
            return pointer;
        }

        public static T* Allocate<T>() where T : unmanaged
        {
            return (T*)Allocate((uint)sizeof(T));
        }

        public static void Free(ref void* pointer)
        {
            nint address = (nint)pointer;
            NativeMemory.AlignedFree(pointer);
#if TRACK_ALLOCATIONS
            addresses.Remove(address);
#if !IGNORE_STACKTRACES
            disposals[address] = new StackTrace(1, true);
#endif
#endif
            pointer = null;
        }

        public static void Free<T>(ref T* pointer) where T : unmanaged
        {
            void* voidPointer = pointer;
            Free(ref voidPointer);
            pointer = null;
        }

        public static void* Reallocate(void* pointer, uint newSize)
        {
            nint oldAddress = (nint)pointer;
#if TRACK_ALLOCATIONS
            addresses.Remove(oldAddress);
#endif
            void* newPointer = NativeMemory.AlignedRealloc(pointer, newSize, GetAlignment(newSize));
            nint newAddress = (nint)newPointer;
#if TRACK_ALLOCATIONS
            addresses.Add(newAddress);
#endif
            return newPointer;
        }

        public static bool IsNull(void* pointer)
        {
            if (pointer is null)
            {
                return true;
            }

#if TRACK_ALLOCATIONS
            nint address = (nint)pointer;
            return !addresses.Contains(address);
#else
            return false;
#endif
        }

        public static void ThrowIfNull(void* pointer)
        {
            if (IsNull(pointer))
            {
#if !IGNORE_STACKTRACES
                nint address = (nint)pointer;
                if (allocations.TryGetValue(address, out StackTrace? stackTrace))
                {
                    throw new NullReferenceException($"Null pointer that was previously allocated at:\n{stackTrace}");
                }

                if (disposals.TryGetValue(address, out stackTrace))
                {
                    throw new NullReferenceException($"Null pointer that was previously disposed at:\n{stackTrace}");
                }
#endif
                throw new NullReferenceException("Null pointer.");
            }
        }

        public static uint GetAlignment<T>() where T : unmanaged
        {
            return GetAlignment((uint)sizeof(T));
        }

        public static uint GetAlignment(uint size)
        {
            if (size % 8 == 0)
            {
                return 8;
            }
            else if (size % 4 == 0)
            {
                return 4;
            }
            else if (size % 2 == 0)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }
    }
}