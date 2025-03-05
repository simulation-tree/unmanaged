#if DEBUG
#define TRACK
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Unmanaged
{
    internal unsafe static class Tracker
    {
        private static readonly ConcurrentStack<nint> addresses = new();
        private static readonly ConcurrentStack<nint> alignedAddresses = new();
        private static readonly Dictionary<nint, (StackTrace stack, uint size)> allocations = new();
        private static readonly Dictionary<nint, StackTrace> disposals = new();

        public static void AppendAllocations(List<char> exceptionMessage, bool free = false)
        {
            nint[] leakedAddresses = addresses.ToArray();
            foreach (nint address in leakedAddresses)
            {
                Append("    ", exceptionMessage);
                if (allocations.TryGetValue(address, out (StackTrace stack, uint size) info))
                {
                    Append(address.ToString(), exceptionMessage);
                    Append(" (", exceptionMessage);
                    Append(info.size.ToString(), exceptionMessage);
                    Append(")", exceptionMessage);
                    Append(" from ", exceptionMessage);
                    AppendLine(info.stack.ToString(), exceptionMessage);
                }
                else
                {
                    AppendLine(address.ToString(), exceptionMessage);
                }

                if (free)
                {
                    void* pointer = (void*)address;
                    Allocations.Free(ref pointer);
                }
            }

            nint[] leakedAlignedAddresses = alignedAddresses.ToArray();
            foreach (nint address in leakedAlignedAddresses)
            {
                Append("    ", exceptionMessage);
                if (allocations.TryGetValue(address, out (StackTrace stack, uint size) info))
                {
                    Append(address.ToString(), exceptionMessage);
                    Append(" (", exceptionMessage);
                    Append(info.size.ToString(), exceptionMessage);
                    Append(")", exceptionMessage);
                    Append(" from ", exceptionMessage);
                    AppendLine(info.stack.ToString(), exceptionMessage);
                }
                else
                {
                    AppendLine(address.ToString(), exceptionMessage);
                }

                if (free)
                {
                    void* pointer = (void*)address;
                    Allocations.FreeAligned(ref pointer);
                }
            }

            void Append(string str, List<char> exceptionMessage)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    exceptionMessage.Add(str[i]);
                }
            }

            void AppendLine(string str, List<char> exceptionMessage)
            {
                Append(str, exceptionMessage);
                exceptionMessage.Add('\n');
            }
        }

        public static void Track(void* pointer, uint size)
        {
            nint address = (nint)pointer;
            addresses.Push(address);
            allocations[address] = (new StackTrace(2, true), size);
            disposals.Remove(address);
        }

        public static void TrackAligned(void* pointer, uint size)
        {
            nint address = (nint)pointer;
            alignedAddresses.Push(address);
            allocations[address] = (new StackTrace(2, true), size);
            disposals.Remove(address);
        }

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

        public readonly struct AllocatedMemory
        {
            public readonly nint address;
            public readonly uint length;

            public AllocatedMemory(nint address, uint length)
            {
                this.address = address;
                this.length = length;
            }
        }
    }
}