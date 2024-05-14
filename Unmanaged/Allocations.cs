#if DEBUG
#define TRACE_ALLOCATIONS
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

        public static void ThrowIfAnyAllocation()
        {
            if (addresses.Count > 0)
            {
                StringBuilder exceptionBuilder = new();
                foreach (nint address in addresses)
                {
                    exceptionBuilder.AppendLine($"Leaked memory at {address:X} allocated from:\n{allocations[address]}");
                }

                string exceptionMessage = exceptionBuilder.ToString();
                throw new Exception(exceptionMessage);
            }
        }

        public static void* Allocate(uint size)
        {
            void* pointer = NativeMemory.Alloc(size);
            nint address = (nint)pointer;
            addresses.Add(address);
#if TRACE_ALLOCATIONS
            allocations[address] = new StackTrace(1, true);
            disposals[address] = null!;
#endif
            return pointer;
        }

        public static void Free(void* pointer)
        {
            nint address = (nint)pointer;
            addresses.Remove(address);
            NativeMemory.Free(pointer);
#if TRACE_ALLOCATIONS
            disposals[address] = new StackTrace(1, true);
#endif
        }

        public static void* Reallocate(void* pointer, uint newSize)
        {
            nint oldAddress = (nint)pointer;
            addresses.Remove(oldAddress);
            void* newPointer = NativeMemory.Realloc(pointer, newSize);
            nint newAddress = (nint)newPointer;
            addresses.Add(newAddress);
            return newPointer;
        }

        public static bool IsNull(void* pointer)
        {
            nint address = (nint)pointer;
            return IsNull(address);
        }

        public static void ThrowIfNull(void* pointer)
        {
            nint address = (nint)pointer;
            ThrowIfNull(address);
        }

        public static void Register(nint address)
        {
            addresses.Add(address);
        }

        public static void Unregister(nint address)
        {
            addresses.Remove(address);
        }

        public static void ThrowIfNull(nint address)
        {
            if (!addresses.Contains(address))
            {
#if TRACE_ALLOCATIONS
                if (disposals.TryGetValue(address, out StackTrace? stackTrace))
                {
                    throw new NullReferenceException($"Null pointer from:\n{stackTrace}");
                }
#endif

                throw new NullReferenceException("Null pointer.");
            }
        }

        public static bool IsNull(nint address)
        {
            return !addresses.Contains(address);
        }
    }
}