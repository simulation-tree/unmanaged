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
        private static readonly HashSet<nint> addresses = new();
        private static readonly Dictionary<nint, StackTrace> allocations = new();
        private static readonly Dictionary<nint, StackTrace> disposals = new();

        /// <summary>
        /// Invoked before <see cref="AppDomain.ProcessExit"/> is, and
        /// before an exception is thrown if there are any memory leaks.
        /// </summary>
        public static event Action? Finish;

#if TRACK_ALLOCATIONS
        /// <summary>
        /// Amount of allocations made that have not been freed.
        /// This value is always 0 in release builds.
        /// </summary>
        public static uint Count => (uint)addresses.Count;
#else
        public const uint Count = 0;
#endif

        static Allocations()
        {
            Finish?.Invoke();
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                ThrowIfAny();
            };
        }

        /// <summary>
        /// Throws an <see cref="Exception"/> if there are any memory leaks.
        /// </summary>
        public static void ThrowIfAny(bool clear = true)
        {
#if TRACK_ALLOCATIONS
            if (addresses.Count > 0)
            {
                StringBuilder exceptionBuilder = new();
                exceptionBuilder.AppendLine($"Leaked {addresses.Count} allocation(s):\n");
                foreach (nint address in addresses)
                {
                    if (allocations.TryGetValue(address, out StackTrace? allocation))
                    {
                        exceptionBuilder.AppendLine($"    {address:X} from {allocation}");
                    }
                    else
                    {
                        exceptionBuilder.AppendLine($"    {address:X}");
                    }
                }

                string exceptionMessage = exceptionBuilder.ToString();
                if (clear)
                {
                    Clear();
                }

                throw new Exception(exceptionMessage);
            }
#endif
        }

        /// <summary>
        /// Frees all allocations made by this class.
        /// </summary>
        public static void Clear()
        {
            foreach (nint address in addresses)
            {
#if ALIGNED
                NativeMemory.AlignedFree((void*)address);
#else
                NativeMemory.Free((void*)address);
#endif
            }

            addresses.Clear();
            allocations.Clear();
            disposals.Clear();
        }

        public static void* Allocate(uint size)
        {
#if ALIGNED
            void* pointer = NativeMemory.AlignedAlloc(size, GetAlignment(size));
#else
            void* pointer = NativeMemory.Alloc(size);
#endif
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

        /// <summary>
        /// Allocates unmanaged memory for a single instance of the given type.
        /// <para>
        /// Requires to be disposed with <see cref="Free(ref void*)"/>
        /// </para>
        /// </summary>
        public static T* Allocate<T>() where T : unmanaged
        {
            return (T*)Allocate((uint)sizeof(T));
        }

        public static void Free(ref void* pointer)
        {
#if ALIGNED
            NativeMemory.AlignedFree(pointer);
#else
            NativeMemory.Free(pointer);
#endif
            nint address = (nint)pointer;
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
#if ALIGNED
            void* newPointer = NativeMemory.AlignedRealloc(pointer, newSize, GetAlignment(newSize));
#else
            void* newPointer = NativeMemory.Realloc(pointer, newSize);
#endif
            nint newAddress = (nint)newPointer;
#if TRACK_ALLOCATIONS
            addresses.Add(newAddress);
#endif
            return newPointer;
        }

        public static T* Reallocate<T>(T* pointer, uint newSize) where T : unmanaged
        {
            void* voidPointer = pointer;
            void* newPointer = Reallocate(voidPointer, newSize);
            return (T*)newPointer;
        }

        /// <summary>
        /// Returns <c>true</c> if the pointer is null, or isn't known
        /// to be a tracked allocation.
        /// </summary>
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

        [Conditional("DEBUG")]
        public static void ThrowIfNull(void* pointer)
        {
            if (IsNull(pointer))
            {
#if !IGNORE_STACKTRACES
                nint address = (nint)pointer;
                if (allocations.TryGetValue(address, out StackTrace? stackTrace))
                {
                    if (disposals.TryGetValue(address, out StackTrace? disposedStackTrace))
                    {
                        throw new NullReferenceException($"Invalid pointer {address:X}\nAllocated at:{stackTrace}\nDisposed at:\n{disposedStackTrace}");
                    }
                    else
                    {
                        throw new NullReferenceException($"Invalid pointer {address:X} that no longer exists, was previously allocated at:\n{stackTrace}");
                    }
                }
                else
                {
                    if (disposals.TryGetValue(address, out stackTrace))
                    {
                        throw new NullReferenceException($"Invalid pointer {address:X} that isn't known to be allocated, but has been disposed at:\n{stackTrace}");
                    }
                    else
                    {
                        throw new NullReferenceException($"Invalid pointer {address:X} that hasn't ever been allocated or disposed.");
                    }
                }
#endif
            }
        }

        public static uint GetAlignment<T>() where T : unmanaged
        {
            return GetAlignment((uint)sizeof(T));
        }

        /// <summary>
        /// Returns an alignment that is able to contain the size.
        /// </summary>
        public static uint GetAlignment(uint size)
        {
            //todo: cases for 16, 32 and 64 is a bit too aggressive for absolutely all invokes
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