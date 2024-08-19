#if DEBUG
#define TRACK
#endif

using System;
using System.Runtime.InteropServices;

#if TRACK
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Concurrent;
#endif

namespace Unmanaged
{
    public static unsafe class Allocations
    {
#if TRACK
        private static readonly ConcurrentBag<nint> addresses = new();
        private static readonly Dictionary<nint, StackTrace> allocations = new();
        private static readonly Dictionary<nint, StackTrace> disposals = new();

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
#if TRACK
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                ThrowIfAny();
            };
#endif
        }

        /// <summary>
        /// Throws an <see cref="Exception"/> if there are any memory leaks.
        /// </summary>
        public static void ThrowIfAny(bool clearAll = true)
        {
#if TRACK
            if (addresses.Count > 0)
            {
                List<char> exceptionBuilder = new();
                Append("Leaked ");
                Append(addresses.Count.ToString());
                AppendLine(" allocation(s):");
                foreach (nint address in addresses)
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

                string exceptionMessage = new(exceptionBuilder.ToArray());
                if (clearAll)
                {
                    foreach (nint address in addresses)
                    {
#if ALIGNED
                        NativeMemory.AlignedFree((void*)address);
#else
                        NativeMemory.Free((void*)address);
#endif
                    }
                }

                void Append(string str)
                {
                    for (int i = 0; i < str.Length; i++)
                    {
                        exceptionBuilder.Add(str[i]);
                    }
                }

                void AppendLine(string str)
                {
                    Append(str);
                    exceptionBuilder.Add('\n');
                }

                throw new Exception(exceptionMessage);
            }
#endif
        }

        public static void* Allocate(uint size)
        {
#if ALIGNED
            void* pointer = NativeMemory.AlignedAlloc(size, GetAlignment(size));
#else
            void* pointer = NativeMemory.Alloc(size);
#endif

#if TRACK
            nint address = (nint)pointer;
            addresses.Add(address);
            allocations[address] = new StackTrace(1, true);
            disposals.Remove(address);
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
#if TRACK
            nint address = (nint)pointer;
            nint[] temp = addresses.ToArray();
            addresses.Clear();
            foreach (nint addr in temp)
            {
                if (addr != address)
                {
                    addresses.Add(addr);
                }
            }

            disposals[address] = new StackTrace(1, true);
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
#if TRACK
            nint oldAddress = (nint)pointer;
            nint[] temp = addresses.ToArray();
            addresses.Clear();
            foreach (nint addr in temp)
            {
                if (addr != oldAddress)
                {
                    addresses.Add(addr);
                }
            }
#endif

#if ALIGNED
            void* newPointer = NativeMemory.AlignedRealloc(pointer, newSize, GetAlignment(newSize));
#else
            void* newPointer = NativeMemory.Realloc(pointer, newSize);
#endif

#if TRACK
            nint newAddress = (nint)newPointer;
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

#if TRACK
            nint address = (nint)pointer;
            return Array.IndexOf(addresses.ToArray(), address) == -1;
#else
            return false;
#endif
        }

        public static void ThrowIfNull(void* pointer)
        {
            if (IsNull(pointer))
            {
                nint address = (nint)pointer;
#if TRACK
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
                throw new NullReferenceException($"Invalid pointer {address:X}.");
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